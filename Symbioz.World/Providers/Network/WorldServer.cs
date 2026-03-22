using SSync;
using SSync.Messages;
using Symbioz.Core;
using Symbioz.Core.DesignPattern;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Network
{
    /// <summary>
    /// Serveur TCP principal du monde de jeu.
    /// Il accepte les connexions des joueurs (WorldClient), les stocke dans une liste,
    /// et permet d'envoyer des messages à tous les clients connectés.
    /// Utilise le pattern Singleton : une seule instance existe dans tout le programme.
    /// </summary>
    public class WorldServer : Singleton<WorldServer>
    {
        // Verrou pour éviter les accès simultanés à la liste des clients depuis plusieurs threads
        static object m_locker = new object();

        static Logger logger = new Logger();

        // Statut actuel du serveur (ONLINE, OFFLINE, FULL, etc.)
        private ServerStatusEnum ServerStatus = ServerStatusEnum.ONLINE;

        // Liste interne de tous les clients connectés (même sans personnage chargé)
        private List<WorldClient> m_clients = new List<WorldClient>();

        /// <summary>
        /// Nombre maximum de clients connectés simultanément depuis le démarrage.
        /// Utile pour surveiller le pic de fréquentation.
        /// </summary>
        public int MaxClientsCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Nombre de clients actuellement connectés au serveur.
        /// </summary>
        public int ClientsCount
        {
            get
            {
                return m_clients.Count;
            }
        }

        /// <summary>
        /// Vérifie si le serveur est dans le statut donné.
        /// </summary>
        public bool IsStatus(ServerStatusEnum status)
        {
            return ServerStatus == status;
        }

        /// <summary>
        /// Le serveur TCP SSync sous-jacent qui gère les connexions socket bas niveau.
        /// </summary>
        public SSyncServer Server
        {
            get;
            set;
        }

        /// <summary>
        /// Constructeur : crée le serveur TCP en utilisant l'hôte et le port de la configuration,
        /// et enregistre les callbacks d'événements réseau.
        /// </summary>
        public WorldServer()
        {
            this.Server = new SSyncServer(WorldConfiguration.Instance.Host, WorldConfiguration.Instance.Port);
            this.Server.OnServerStarted += Server_OnServerStarted;
            this.Server.OnServerFailedToStart += Server_OnServerFailedToStart;
            // Quand un socket client arrive, on crée un WorldClient pour le gérer
            this.Server.OnClientConnected += Server_OnSocketAccepted;
        }

        /// <summary>
        /// Appelé quand un nouveau socket client se connecte.
        /// Vérifie que le client a bien un endpoint réseau avant de créer son WorldClient.
        /// </summary>
        void Server_OnSocketAccepted(Socket socket)
        {
            if (socket.RemoteEndPoint != null)
            {
                logger.White("New client connected");
                // Crée un WorldClient qui va gérer toute la session de ce joueur
                new WorldClient(socket);
            }
            else
            {
                // Un socket sans endpoint est suspect (tentative de spoofing ?)
                logger.Error("A world socket try to connect without endpoint??? is it spoofing?");
            }
        }

        /// <summary>
        /// Appelé si le serveur TCP ne parvient pas à démarrer (port déjà utilisé, etc.).
        /// </summary>
        void Server_OnServerFailedToStart(Exception ex)
        {

        }

        /// <summary>
        /// Appelé quand le serveur TCP a démarré avec succès. Affiche l'adresse d'écoute.
        /// </summary>
        void Server_OnServerStarted()
        {
            logger.Gray("Server Started (" + Server.EndPoint.ToString() + ")");
        }

        /// <summary>
        /// Démarre l'écoute TCP sur le port configuré.
        /// </summary>
        public void Start()
        {
            Server.Start();
        }

        /// <summary>
        /// Envoie un message à tous les clients "en jeu" (ceux qui ont un personnage chargé).
        /// Utilise un verrou pour éviter les problèmes de concurrence entre threads.
        /// </summary>
        public void Send(Message message)
        {
            lock (m_locker)
                GetOnlineClients().SendAll(message);
        }

        /// <summary>
        /// Ajoute un client à la liste et met à jour le record de connexions simultanées maximum.
        /// </summary>
        public void AddClient(WorldClient client)
        {
            m_clients.Add(client);

            // Met à jour le record de connexions simultanées si dépassé
            if (ClientsCount > MaxClientsCount)
            {
                MaxClientsCount = ClientsCount;
            }
        }

        /// <summary>
        /// Retire un client de la liste (appelé lors de la déconnexion).
        /// </summary>
        public void RemoveClient(WorldClient client)
        {
            lock (m_locker)
            {
                m_clients.Remove(client);
                logger.White("Client disconnected");
            }
        }

        /// <summary>
        /// Retourne la liste complète des clients connectés (avec ou sans personnage).
        /// </summary>
        public List<WorldClient> GetClients()
        {
            lock (m_locker)
            {
                return m_clients;
            }
        }

        /// <summary>
        /// Déconnecte le client dont le compte a l'identifiant donné.
        /// Retourne true si le client a été trouvé et déconnecté, false sinon.
        /// </summary>
        public bool Disconnect(int accountId)
        {
            lock (m_locker)
            {
                // Cherche le client par ID de compte
                var client = m_clients.Find(x => x.Account != null && x.Account.Id == accountId);
                if (client != null)
                {
                    client.Disconnect();
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Retourne uniquement les clients qui ont un personnage en jeu (Character != null).
        /// Ce sont les joueurs "actifs" dans le monde.
        /// </summary>
        public List<WorldClient> GetOnlineClients()
        {
            lock (m_locker)
            {
                return m_clients.FindAll(x => x.Character != null);
            }
        }

        /// <summary>
        /// Retourne le client en ligne dont le compte correspond à l'ID donné.
        /// </summary>
        public WorldClient GetOnlineClient(int accountId)
        {
            lock (m_locker)
            {
                return GetOnlineClients().Find(x => x.Account != null && x.Account.Id == accountId);
            }
        }

        /// <summary>
        /// Retourne le client en ligne dont le personnage porte le nom donné.
        /// </summary>
        public WorldClient GetOnlineClient(string name)
        {
            lock (m_locker)
            {
                return GetOnlineClients().Find(x => x.Character.Name == name);
            }
        }

        /// <summary>
        /// Retourne le client en ligne dont le personnage a l'ID long donné.
        /// </summary>
        public WorldClient GetOnlineClient(long id)
        {
            lock (m_locker)
            {
                return GetOnlineClients().Find(x => x.Character.Id == id);
            }
        }

        /// <summary>
        /// Vérifie si un personnage (par son ID) est actuellement connecté en jeu.
        /// </summary>
        public bool IsOnline(long id)
        {
            return GetOnlineClient(id) != null;
        }

        /// <summary>
        /// Envoie un message à tous les clients dont le personnage se trouve dans une sous-zone donnée.
        /// Utilisé par exemple pour les messages de zone ou les événements locaux.
        /// </summary>
        public void SendOnSubarea(Message message, ushort subAreaId)
        {
            foreach (var client in GetOnlineClients().FindAll(x => x.Character.SubareaId == subAreaId))
            {
                client.Send(message);
            }
        }

        /// <summary>
        /// Exécute une action sur chaque client "en ligne" (qui a un personnage).
        /// Pratique pour itérer sur tous les joueurs actifs sans dupliquer le code de filtre.
        /// </summary>
        public void OnClients(Action<WorldClient> action)
        {
            foreach (var client in GetOnlineClients())
            {
                action(client);
            }
        }

        /// <summary>
        /// Change le statut du serveur et notifie le serveur d'authentification.
        /// Certains statuts (OFFLINE, STOPING) déclenchent la déconnexion de tous les joueurs.
        /// </summary>
        public void SetServerStatus(ServerStatusEnum status)
        {
            ServerStatus = status;
            // Informe l'AuthServer du nouveau statut pour qu'il mette à jour la liste des serveurs
            TransitionServerManager.Instance.AuthServer.SendUnique(new SetServerStatusMessage(ServerStatus));

            switch (status)
            {
                case ServerStatusEnum.STATUS_UNKNOWN:
                    break;
                case ServerStatusEnum.OFFLINE:
                    // Le serveur passe hors ligne : on déconnecte tout le monde
                    DisconnectAll();
                    break;
                case ServerStatusEnum.STARTING:
                    break;
                case ServerStatusEnum.ONLINE:
                    break;
                case ServerStatusEnum.NOJOIN:
                    break;
                case ServerStatusEnum.SAVING:
                    break;
                case ServerStatusEnum.STOPING:
                    // Le serveur s'arrête : on déconnecte tout le monde
                    DisconnectAll();
                    break;
                case ServerStatusEnum.FULL:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Déconnecte tous les clients connectés (utilisé lors de l'arrêt du serveur).
        /// </summary>
        public void DisconnectAll()
        {
            for (int i = 0; i < m_clients.Count; i++)
            {
                m_clients[i].Disconnect();
            }
        }
    }
}
