using SSync.Messages;
using SSync.Transition;
using Symbioz.Core;
using Symbioz.Core.DesignPattern;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.ORM;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade;
using Symbioz.Protocol.Selfmade.Messages;
using Symbioz.World.Providers.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Symbioz.World.Network
{
    /// <summary>
    /// Gère la connexion entre le serveur de jeu (WorldServer) et le serveur d'authentification (AuthServer).
    /// Cette connexion "de transition" permet :
    /// - d'enregistrer le serveur de jeu auprès de l'AuthServer
    /// - de recevoir les tickets des joueurs qui se connectent
    /// - de mettre à jour le statut du serveur (ONLINE, OFFLINE, FULL, etc.)
    /// Utilise le pattern Singleton : une seule instance dans tout le programme.
    /// </summary>
    public class TransitionServerManager : Singleton<TransitionServerManager>
    {
        static Logger logger = new Logger();

        /// <summary>
        /// Le client de transition représentant la connexion vers l'AuthServer.
        /// C'est par lui que transitent les informations de compte des joueurs.
        /// </summary>
        public TransitionClient AuthServer;

        /// <summary>
        /// Indique si la connexion avec l'AuthServer est active.
        /// </summary>
        public bool IsConnected = false;

        /// <summary>
        /// Initialise et démarre la tentative de connexion au serveur d'authentification.
        /// Crée un AuthTransitionClient et tente la connexion TCP.
        /// </summary>
        public void TryToJoinAuthServer()
        {
            AuthServer = new AuthTransitionClient();
            TryConnect();
        }

        /// <summary>
        /// Envoie un message à l'AuthServer pour passer le serveur en statut ONLINE,
        /// ce qui autorise les joueurs à s'y connecter depuis la liste des serveurs.
        /// Appelé une fois que tous les composants du serveur sont initialisés.
        /// </summary>
        public void AllowConnections()
        {
            AuthServer.SendUnique(new SetServerStatusMessage(ServerStatusEnum.ONLINE));
        }

        /// <summary>
        /// Tente la connexion TCP vers l'AuthServer en utilisant l'hôte et le port
        /// définis dans la configuration (TransitionHost, TransitionPort).
        /// </summary>
        private void TryConnect()
        {
            AuthServer.Connect(WorldConfiguration.Instance.TransitionHost, WorldConfiguration.Instance.TransitionPort);
        }

        /// <summary>
        /// Appelé si la connexion à l'AuthServer échoue.
        /// Attend 3 secondes puis retente la connexion (boucle de reconnexion automatique).
        /// </summary>
        public void OnFailedToConnectAuth()
        {
            logger.Gray("Unable to connect to AuthServer.. Trying to reconnect in 3s");
            Thread.Sleep(3000); // Pause de 3 secondes avant de réessayer
            TryToJoinAuthServer();
        }

        /// <summary>
        /// Appelé si la connexion à l'AuthServer est perdue en cours de fonctionnement.
        /// Sauvegarde les données, déconnecte tous les joueurs et arrête le serveur proprement.
        /// </summary>
        public void OnConnectionToAuthLost()
        {
            logger.Error("Connection to AuthServer was lost.. Server is shutting down.");
            // Sauvegarde en urgence toutes les données (personnages, guildes, etc.)
            SaveTask.Save();
            // Déconnecte tous les joueurs connectés
            WorldServer.Instance.DisconnectAll();
            Thread.Sleep(3000); // Laisse le temps aux déconnexions de se terminer
            Environment.Exit(0); // Arrêt du processus
        }

        /// <summary>
        /// Appelé quand la connexion à l'AuthServer est établie avec succès.
        /// Enregistre ce serveur de jeu auprès de l'AuthServer en envoyant son identifiant,
        /// son nom, son type et son adresse IP/port d'écoute.
        /// </summary>
        public void OnConnectedToAuth()
        {
            logger.White("Connected to AuthServer");
            this.IsConnected = true;

            // Si UseCustomHost est activé, utilise l'IP publique personnalisée plutôt que l'IP locale
            string host = WorldConfiguration.Instance.UseCustomHost ? WorldConfiguration.Instance.CustomHost : WorldConfiguration.Instance.Host;

            // Enregistre le serveur de jeu auprès de l'AuthServer avec toutes ses informations
            AuthServer.SendUnique(new WorldRegistrationRequestMessage((ushort)WorldConfiguration.Instance.ServerId,
            WorldConfiguration.Instance.ServerName, WorldConfiguration.Instance.ServerType,
            host, WorldConfiguration.Instance.Port));
        }




    }
}
