using SSync;
using SSync.Messages;
using SSync.Transition;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Messages;
using Symbioz.Protocol.Selfmade.Types;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities;
using Symbioz.World.Records;
using Symbioz.World.Records.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Core;
using System.Net;
using Symbioz.World.Records.Breeds;
using Symbioz.Protocol.Enums;

namespace Symbioz.World.Network
{
    /// <summary>
    /// Représente un joueur connecté au serveur de jeu.
    /// Hérite de SSyncClient qui gère la couche réseau bas niveau (socket, sérialisation des messages).
    /// Un WorldClient est créé dès qu'un socket se connecte, et contient :
    /// - les données du compte (Account, AccountInformations)
    /// - la liste des personnages du compte (Characters)
    /// - le personnage actuellement en jeu (Character), null si pas encore choisi
    /// </summary>
    public class WorldClient : SSyncClient
    {
        /// <summary>
        /// Données brutes du compte reçues depuis l'AuthServer via la transition
        /// (identifiant, pseudo, droits, ticket...).
        /// </summary>
        public AccountData Account
        {
            get;
            set;
        }

        /// <summary>
        /// Informations supplémentaires du compte stockées en base de données
        /// (préférences, tutoriels, etc.).
        /// </summary>
        public AccountInformationsRecord AccountInformations
        {
            get;
            set;
        }

        /// <summary>
        /// Indique si le joueur est actuellement "en jeu" (un personnage est chargé).
        /// Un client peut être connecté mais pas encore en jeu (écran de sélection de personnage).
        /// </summary>
        public bool InGame
        {
            get
            {
                return Character != null;
            }
        }

        /// <summary>
        /// Le personnage actuellement joué par ce client.
        /// Null si le joueur est encore à l'écran de sélection.
        /// </summary>
        public Character Character
        {
            get;
            set;
        }

        /// <summary>
        /// Liste de tous les personnages appartenant à ce compte.
        /// Chargée en base de données à l'authentification.
        /// </summary>
        public List<CharacterRecord> Characters
        {
            get;
            set;
        }

        /// <summary>
        /// Indique si le compte possède des actions de démarrage en attente
        /// (ex : récompenses à distribuer, tutoriels non complétés).
        /// </summary>
        public bool HasStartupActions
        {
            get
            {
                return StartupActions.Count > 0;
            }
        }

        /// <summary>
        /// Liste des actions de démarrage associées à ce compte
        /// (cadeaux, notifications, actions différées liées au compte).
        /// </summary>
        public List<StartupActionRecord> StartupActions
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructeur : appelé dès qu'un socket se connecte au serveur.
        /// Envoie immédiatement le message HelloGame pour initier le handshake Dofus,
        /// et s'enregistre auprès du WorldServer.
        /// </summary>
        public WorldClient(Socket socket)
            : base(socket)
        {
            // Si un message ne peut pas être traité, on envoie un message d'erreur au joueur
            base.OnMessageHandleFailed += WorldClient_OnMessageHandleFailed;
            // Premier message envoyé au client pour démarrer la procédure d'authentification
            base.Send(new HelloGameMessage());
            // S'enregistre dans la liste globale du serveur
            WorldServer.Instance.AddClient(this);
        }

        /// <summary>
        /// Appelé quand un message reçu du client ne peut pas être traité (handler introuvable ou exception).
        /// Informe le joueur de l'erreur si son personnage est chargé.
        /// </summary>
        void WorldClient_OnMessageHandleFailed(Message message)
        {
            if (Character != null && message != null)
                Character.ReplyError("Impossible d'executer l'action (" + message.ToString() + ").");
        }

        /// <summary>
        /// Appelé automatiquement quand la connexion socket est fermée.
        /// Retire le client du WorldServer et déclenche la sauvegarde/nettoyage du personnage.
        /// </summary>
        public override void OnClosed()
        {
            try
            {
                // Retirer le client de la liste globale
                WorldServer.Instance.RemoveClient(this);
                // Si un personnage était en jeu, déclenche sa routine de déconnexion (sauvegarde, quitter la map, etc.)
                if (Character != null)
                    Character.OnDisconnected();
                base.OnClosed();
            }
            catch (Exception ex)
            {
                Logger.Write<WorldClient>("Cannot disconnect client..." + ex.ToString(), ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Appelé après réception et validation des données de compte depuis l'AuthServer.
        /// Charge les informations du compte depuis la BDD et envoie au client les messages
        /// d'initialisation nécessaires pour afficher l'écran de sélection de personnage.
        /// </summary>
        public void OnAccountReceived()
        {
            // Charge les infos supplémentaires du compte depuis la base de données
            AccountInformations = AccountInformationsRecord.Load(Account.Id);
            // Charge tous les personnages appartenant à ce compte
            Characters = CharacterRecord.GetCharactersByAccountId(Account.Id);
            // Charge les actions de démarrage en attente pour ce compte
            StartupActions = StartupActionRecord.GetStartupActions(Account.Id);

            // Valide le ticket d'authentification côté client
            Send(new AuthenticationTicketAcceptedMessage());
            // Envoie les capacités du compte (races disponibles, abonnement, id...)
            Send(new AccountCapabilitiesMessage(true, true, Account.Id, BreedRecord.AvailableBreedsFlags,
                  BreedRecord.AvailableBreedsFlags, 1));
            // Indique que le compte est de confiance (pas de restriction anti-bot)
            Send(new TrustStatusMessage(true, true));
            // Paramètres du serveur : langue "fr", communauté 1
            Send(new ServerSettingsMessage("fr", 1, 0, 0));
            // Fonctionnalités optionnelles activées (montiliers, havresacs, etc.)
            Send(new ServerOptionalFeaturesMessage(new sbyte[] { 1, 2, 3, 4 }));
        }

        /// <summary>
        /// Retourne le CharacterRecord (données BDD) du personnage identifié par son ID,
        /// parmi les personnages de ce compte. Retourne null si non trouvé.
        /// </summary>
        public CharacterRecord GetAccountCharacter(long id)
        {
            return Characters.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        /// Envoie des données brutes au client sous forme de RawDataMessage.
        /// Utilisé pour les données de protocole qui ne correspondent pas à un message typé.
        /// </summary>
        public void SendRaw(byte[] rawData)
        {
            this.Send(new RawDataMessage(rawData));
        }

        /// <summary>
        /// Envoie au client la liste de ses personnages.
        /// Si certains personnages nécessitent un remodelage (changement d'apparence),
        /// envoie un message spécial incluant ces informations.
        /// </summary>
        public void SendCharactersList()
        {
            // Convertit chaque CharacterRecord en CharacterBaseInformations (type protocole réseau)
            CharacterBaseInformations[] characters = Characters.ConvertAll(x => (CharacterBaseInformations)x.GetCharacterBaseInformations()).ToArray();

            // Filtre les personnages qui ont un remodelage en attente (changement de race/sexe/apparence)
            CharacterToRemodelInformations[] characterToRemodel =
                Characters.FindAll(x => x.RemodelingMaskEnum != CharacterRemodelingEnum.CHARACTER_REMODELING_NOT_APPLICABLE).
                ConvertAll(x => x.GetCharacterToRemodelInformations()).ToArray();

            if (characterToRemodel.Count() > 0)
            {
                // Message étendu incluant les infos de remodelage
                Send(new CharactersListWithRemodelingMessage(characters, HasStartupActions, characterToRemodel));
            }
            else
            {
                // Message standard
                Send(new CharactersListMessage(characters, HasStartupActions));
            }
        }

        /// <summary>
        /// Envoie une requête de bannissement du compte à l'AuthServer,
        /// puis déconnecte le client si la requête est acceptée.
        /// Retourne true si le ban a été effectué, false sinon.
        /// </summary>
        public bool Ban()
        {
            bool result = false;

            if (Account != null)
            {
                // Envoie une demande de ban à l'AuthServer et attend la confirmation
                MessagePool.SendRequest<BanConfirmMessage>(TransitionServerManager.Instance.AuthServer, new BanRequestMessage
                {
                    AccountId = Account.Id,
                }, delegate (BanConfirmMessage msg)
                {
                    // Le ban est confirmé : on déconnecte le joueur
                    result = true;
                    this.Disconnect();
                },
                delegate ()
                {
                    // Timeout ou refus sans action particulière
                });
            }
            return result;

        }

    }
}
