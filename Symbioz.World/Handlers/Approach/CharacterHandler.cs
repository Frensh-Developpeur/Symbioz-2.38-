using SSync.Messages;
using SSync.Transition;
using Symbioz.Core;
using Symbioz.ORM;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Selfmade.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Network;
using Symbioz.World.Providers;
using Symbioz.World.Records;
using Symbioz.World.Records.Breeds;
using Symbioz.World.Records.Characters;
using Symbioz.World.Records.Guilds;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.Approach
{
    /// <summary>
    /// Handler des messages liés aux personnages : liste, création, sélection, suppression.
    /// Ces actions se déroulent avant que le joueur soit "en jeu" (écran de sélection du personnage).
    /// Certaines opérations (création, suppression) nécessitent une validation par le serveur Auth.
    /// </summary>
    class CharacterHandler
    {
        static Logger logger = new Logger();

        // Caractères interdits dans les noms de personnages
        public static char[] UnauthorizedNameContent = new char[] { '(', ')', '[', '{', '}', ']', '\'', ':', '<', '>', '?', '!' };

        /// <summary>
        /// Reçu quand le client ouvre l'écran de sélection : envoie la liste des personnages du compte.
        /// </summary>
        [MessageHandler]
        public static void HandleCharacterList(CharactersListRequestMessage message, WorldClient client)
        {
            client.SendCharactersList();

        }

        /// <summary>
        /// Reçu quand le joueur demande la suppression d'un personnage.
        /// La suppression est d'abord validée auprès du serveur Auth (pour cohérence entre les serveurs),
        /// puis traitée localement dans ProcessDeletion si la réponse est positive.
        /// </summary>
        [MessageHandler]
        public static void HandleCharacterDeletionRequest(CharacterDeletionRequestMessage message, WorldClient client)
        {
            if (!WorldServer.Instance.IsStatus(ServerStatusEnum.ONLINE))
            {
                client.Send(new CharacterDeletionErrorMessage((sbyte)CharacterDeletionErrorEnum.DEL_ERR_NO_REASON));
                return;
            }
            if (!client.InGame)
            {
                // Envoie une demande de suppression au serveur Auth et attend la réponse (pattern request/callback)
                MessagePool.SendRequest<OnCharacterDeletionResultMessage>(TransitionServerManager.Instance.AuthServer, new OnCharacterDeletionMessage
                {
                    CharacterId = (long)message.characterId,
                },
                delegate (OnCharacterDeletionResultMessage msg)
                {
                    // Une fois la réponse reçue, on traite la suppression côté monde
                    ProcessDeletion(msg.Succes, client, (long)message.characterId);
                });

            }
        }

        /// <summary>
        /// Effectue la suppression effective d'un personnage après validation par le serveur Auth.
        /// Retire le personnage de la guilde si nécessaire, supprime les données en base, puis renvoie la liste.
        /// </summary>
        public static void ProcessDeletion(bool succes, WorldClient client, long characterId)
        {

            if (!succes)
            {
                client.Send(new CharacterDeletionErrorMessage((sbyte)CharacterDeletionErrorEnum.DEL_ERR_NO_REASON));
                return;
            }

            var record = client.GetAccountCharacter(characterId);
            if (record == null)
            {
                client.Send(new CharacterDeletionErrorMessage((sbyte)CharacterDeletionErrorEnum.DEL_ERR_NO_REASON));
                return;
            }
            // Retire le personnage de la liste en mémoire du compte
            client.Characters.Remove(record);

            // Si le personnage appartenait à une guilde, nettoie son entrée dans la guilde
            if (record.GuildId != 0)
            {
                GuildRecord.RemoveWhereId(record);
            }

            // Supprime toutes les données du personnage en base de données (items, sorts, stats, etc.)
            DatabaseManager.GetInstance().RemoveWhereIdMethod((long)characterId);

            // Renvoie la liste mise à jour au client
            client.SendCharactersList();
        }

        /// <summary>
        /// Génère et envoie un nom de personnage aléatoire au client (bouton "dé" sur l'écran de création).
        /// </summary>
        [MessageHandler]
        public static void HandleCharacterNameSuggestionRequest(CharacterNameSuggestionRequestMessage message, WorldClient client)
        {
            if (!client.InGame)
                client.Send(new CharacterNameSuggestionSuccessMessage(StringUtils.RandomName()));
        }

        /// <summary>
        /// Reçu quand le joueur valide la création d'un nouveau personnage.
        /// Effectue toutes les vérifications (serveur en ligne, slots disponibles, nom valide)
        /// puis soumet la demande au serveur Auth. Si accepté, crée le personnage et le sélectionne.
        /// </summary>
        [MessageHandler]
        public static void HandleCharacterCreation(CharacterCreationRequestMessage message, WorldClient client)
        {
            if (!WorldServer.Instance.IsStatus(ServerStatusEnum.ONLINE))
            {
                client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_NO_REASON));
                return;
            }
            if (!client.InGame)
            {
                // Vérifie que le compte n'a pas atteint le nombre maximum de personnages autorisés
                if (client.Characters.Count() == client.Account.CharacterSlots)
                {
                    client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_TOO_MANY_CHARACTERS));
                    return;
                }
                // Vérifie que le nom choisi n'est pas déjà utilisé par un autre personnage
                if (CharacterRecord.NameExist(message.name))
                {

                    client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_NAME_ALREADY_EXISTS));
                    return;
                }

                // Les joueurs normaux ne peuvent pas utiliser de caractères spéciaux dans leur pseudo ;
                // les animateurs et supérieurs sont exemptés de cette restriction
                if (client.Account.Role < ServerRoleEnum.Animator)
                {
                    foreach (var value in message.name)
                    {
                        if (UnauthorizedNameContent.Contains(value))
                        {
                            client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_INVALID_NAME));
                            return;
                        }
                    }
                }
                // Interdit les noms composés (avec espaces)
                if (message.name.Split(null).Count() > 1)
                {
                    client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_INVALID_NAME));
                    return;
                }

                // Génère le prochain identifiant unique disponible pour le nouveau personnage
                long nextId = CharacterRecord.Characters.DynamicPop(x => x.Id);

                // Demande la validation de la création au serveur Auth, avec le callback CreateCharacter
                MessagePool.SendRequest<OnCharacterCreationResultMessage>(TransitionServerManager.Instance.AuthServer, new OnCharacterCreationMessage
                {
                    AccountId = client.Account.Id,
                    CharacterId = nextId,

                }, delegate (OnCharacterCreationResultMessage result)
                {
                    CreateCharacter(message, client, result.Succes, nextId);
                });
            }

        }

        /// <summary>
        /// Crée effectivement le personnage en base de données et en mémoire après validation Auth.
        /// Génère l'apparence (look) à partir de la race, du sexe et des couleurs choisies,
        /// puis appelle ProcessSelection pour mettre le personnage en jeu immédiatement.
        /// </summary>
        static void CreateCharacter(CharacterCreationRequestMessage message, WorldClient client, bool succes, long id)
        {
            if (!succes)
            {
                client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_NO_REASON));
                return;
            }
            // Génère l'apparence (look) correspondant à la race, au sexe, au cosmétique et aux couleurs
            ContextActorLook look = BreedRecord.GetBreedLook(message.breed, message.sex, message.cosmeticId, message.colors);
            // Crée le record en base et initialise le personnage avec ses données de départ
            CharacterRecord record = CharacterRecord.New(id, message.name, client.Account.Id, look, message.breed, message.cosmeticId, message.sex);

            record.AddInstantElement();
            // Instancie le personnage en mémoire (true = nouveau personnage, déclenche les messages de bienvenue)
            client.Character = new Character(client, record, true);
            logger.White("Character " + record.Name + " created");
            ProcessSelection(client);
        }

        /// <summary>
        /// Finalise la connexion d'un personnage sélectionné : envoie toutes les données initiales au client
        /// (sorts, inventaire, raccourcis, métiers, alignement, guilde, restrictions, canaux activés...).
        /// C'est la séquence de chargement complète qui place le joueur dans le monde.
        /// </summary>
        static void ProcessSelection(WorldClient client)
        {
            // Envoie la liste des notifications actives du compte (ex. 2147483647 = tous les bits actifs)
            client.Send(new NotificationListMessage(new int[] { 2147483647 }));
            client.Send(new CharacterSelectedSuccessMessage(client.Character.Record.GetCharacterBaseInformations(),
               false));
            // Capacités du personnage (masque de bits des actions autorisées)
            client.Send(new CharacterCapabilitiesMessage(4095));
            client.Send(new SequenceNumberRequestMessage());
           
            client.Character.RefreshEmotes();
            client.Character.RefreshSpells();
            client.Character.Inventory.Refresh();
            client.Character.RefreshShortcuts();
            client.Character.RefreshJobs();
            client.Character.RefreshAlignment();
            client.Character.SetRestrictions();
            client.Character.RefreshArenaInfos();
            client.Character.OnConnected();
            client.Character.RefreshGuild();
            // Active les canaux de chat disponibles (0-10, 12, 13 = canaux standards)
            client.Send(new EnabledChannelsMessage(new sbyte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13 },
               new sbyte[0]));
            client.Character.SafeConnection();

            // Signale au client que le chargement est terminé (déclenche l'affichage du monde)
            client.Send(new CharacterLoadingCompleteMessage());

        }

        /// <summary>
        /// Reçu quand le joueur sélectionne un personnage existant sur l'écran de choix.
        /// Instancie le personnage en mémoire puis envoie toutes les données via ProcessSelection.
        /// </summary>
        [MessageHandler]
        public static void HandleCharacterSelection(CharacterSelectionMessage message, WorldClient client)
        {
            if (!client.InGame)
            {
                // false = personnage existant (pas nouveau), pas de messages de bienvenue
                client.Character = new Character(client, client.GetAccountCharacter((long)message.id), false);
                ProcessSelection(client);
            }
        }

        /// <summary>
        /// Variante de la sélection normale avec remodelage du personnage.
        /// Permet de modifier le nom, la race, le sexe, le cosmétique ou les couleurs d'un personnage
        /// existant si le flag RemodelingMask le permet (acheté en boutique ou gagné en jeu).
        /// </summary>
        [MessageHandler]
        public static void HandleCharacterSelectionWithRemodel(CharacterSelectionWithRemodelMessage message, WorldClient client)
        {
            if (!client.InGame)
            {
                CharacterRecord record = client.GetAccountCharacter((long)message.id);

                // Remodelage du nom si le flag correspondant est actif
                if (record.RemodelingMaskEnum.HasFlag(CharacterRemodelingEnum.CHARACTER_REMODELING_NAME))
                {
                    if (!CharacterRemodelingProvider.Instance.RemodelName(record, message.remodel.name))
                    {
                        // Le nouveau nom est déjà pris
                        client.Send(new CharacterCreationResultMessage((sbyte)CharacterCreationResultEnum.ERR_NAME_ALREADY_EXISTS));
                        return;
                    }
                }

                // Remodelage de la race et du cosmétique si le flag est actif
                if (record.RemodelingMaskEnum.HasFlag(CharacterRemodelingEnum.CHARACTER_REMODELING_BREED))
                {
                    CharacterRemodelingProvider.Instance.RemodelBreed(record, message.remodel.breed, message.remodel.cosmeticId);
                }

                // Remodelage du genre (non implémenté pour l'instant)
                if (record.RemodelingMaskEnum.HasFlag(CharacterRemodelingEnum.CHARACTER_REMODELING_GENDER))
                {

                }

                // Remodelage du cosmétique seul (visage/tête)
                if (record.RemodelingMaskEnum.HasFlag(CharacterRemodelingEnum.CHARACTER_REMODELING_COSMETIC))
                {
                    CharacterRemodelingProvider.Instance.RemodelCosmetic(record, message.remodel.cosmeticId);
                }

                // Remodelage des couleurs du personnage
                if (record.RemodelingMaskEnum.HasFlag(CharacterRemodelingEnum.CHARACTER_REMODELING_COLORS))
                {
                    CharacterRemodelingProvider.Instance.RemodelColors(record, message.remodel.colors);

                }

                // Remet le masque à "non applicable" pour désactiver le mode remodelage
                record.RemodelingMaskEnum = CharacterRemodelingEnum.CHARACTER_REMODELING_NOT_APPLICABLE;
                client.Character = new Character(client, record, false);
                ProcessSelection(client);
            }
        }
    }
}
