using SSync.Messages;
using Symbioz.Core;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Items;
using Symbioz.World.Network;
using Symbioz.World.Records.Characters;
using Symbioz.World.Records.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.Approach
{
    /// <summary>
    /// Handler des "Startup Actions" : cadeaux et objets de démarrage associés à un compte.
    /// Une StartupAction est une récompense en attente d'attribution (ex. pack de démarrage, cadeau de bienvenue).
    /// Le joueur choisit sur quel personnage il veut recevoir les objets, puis le serveur les ajoute à l'inventaire.
    /// </summary>
    class StartupActionsHandler
    {
        // Si false : les objets sont générés avec des statistiques aléatoires (réaliste).
        // Si true : les objets sont générés avec les statistiques maximales (parfaits).
        public const bool ITEM_GENERATION_PERFECT = false;

        // Verrou pour éviter les doublons si deux attributions arrivent simultanément
        static object locker = new object();

        static Logger logger = new Logger();

        /// <summary>
        /// Reçu quand le client demande la liste des actions de démarrage disponibles sur ce compte.
        /// Convertit chaque StartupActionRecord en StartupActionAddObject et les envoie en une seule liste.
        /// </summary>
        [MessageHandler]
        public static void HandleStartupActionsExecute(StartupActionsExecuteMessage message, WorldClient client)
        {
            client.Send(new StartupActionsListMessage(client.StartupActions.ConvertAll<StartupActionAddObject>(x => x.GetStartupActionAddObject()).ToArray()));
        }

        /// <summary>
        /// Reçu quand le joueur choisit un personnage pour recevoir les objets d'une action.
        /// Le verrou (lock) est essentiel pour éviter qu'une double soumission attribue les items deux fois.
        /// </summary>
        [MessageHandler]
        public static void HandleStartupActionsObjetAttribution(StartupActionsObjetAttributionMessage message, WorldClient client)
        {
            lock (locker)
            {
                if (client.StartupActions != null)
                {
                    // Cherche l'action demandée dans la liste du client
                    StartupActionRecord action = client.StartupActions.FirstOrDefault(x => x.Id == message.actionId);

                    if (action != null)
                    {
                        // Attribution réelle des objets au personnage choisi
                        AttributeAction(client, action, (long)message.characterId);
                        // Retire l'action de la liste pour éviter de la redistribuer
                        client.StartupActions.Remove(action);
                    }
                    else
                        // L'action n'existe pas ou a déjà été utilisée
                        client.Send(new StartupActionFinishedMessage(false, false, action.Id));
                }
                else
                {
                    logger.Error("StartupActions of client is null");
                    client.Disconnect();
                }
            }
        }

        /// <summary>
        /// Ajoute effectivement les objets de l'action dans l'inventaire du personnage ciblé.
        /// Parcourt la liste des GId (identifiants génériques d'items) et de leurs quantités,
        /// crée les CharacterItems correspondants et les ajoute silencieusement (sans rafraîchissement immédiat).
        /// Le bloc finally supprime l'action de la base même en cas d'erreur.
        /// </summary>
        private static void AttributeAction(WorldClient client, StartupActionRecord action, long characterId)
        {
            if (WorldServer.Instance.IsStatus(ServerStatusEnum.ONLINE))
            {
                try
                {
                    // Boucle sur chaque item de l'action (un pack peut contenir plusieurs items différents)
                    for (int i = 0; i < action.GIds.Count; i++)
                    {
                        ushort gid = action.GIds[i];           // Identifiant générique de l'item (template)
                        uint quantity = action.Quantities[i];  // Quantité à donner
                        ItemRecord item = ItemRecord.GetItem(gid);

                        var character = client.GetAccountCharacter(characterId);
                        // Génère une instance de l'item avec les stats (parfait ou aléatoire selon ITEM_GENERATION_PERFECT)
                        var characterItem = item.GetCharacterItem(characterId, quantity, ITEM_GENERATION_PERFECT);

                        // Ajoute l'item silencieusement (pas de notification visuelle immédiate au joueur)
                        CharacterItemRecord.AddQuietCharacterItem(character, characterItem);
                    }

                    client.Send(new StartupActionFinishedMessage(true, false, action.Id));
                }
                catch (Exception ex)
                {
                    logger.Error("Unable to attribute action to " + client.Account.Username + " :" + ex);
                    client.Send(new StartupActionFinishedMessage(false, false, action.Id));
                }
                finally
                {
                    // Supprime l'action de la base dans tous les cas (succès ou erreur) pour éviter les doublons
                    action.RemoveInstantElement(); // How, its dangerous!
                }
            }
            else
            {
                // Le serveur est hors ligne (maintenance), déconnecte le client proprement
                client.Disconnect();
            }
        }
    }
}
