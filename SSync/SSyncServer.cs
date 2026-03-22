using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SSync
{
    /// <summary>
    /// Serveur TCP asynchrone de la bibliothèque SSync.
    /// Écoute les connexions entrantes sur un EndPoint (IP:Port) et déclenche
    /// des événements pour chaque nouvelle connexion ou en cas d'erreur de démarrage.
    /// Utilise le pattern async/await des sockets Windows (SocketAsyncEventArgs)
    /// pour une performance maximale sans bloquer de threads.
    /// </summary>
    public class SSyncServer
    {
        // Délégués (types de fonctions) pour les événements du serveur
        public delegate void OnSocketAcceptedDel(Socket socket);
        public delegate void OnServerFailedToStartDel(Exception ex);

        /// <summary>
        /// Déclenché quand un client se connecte sur l'EndPoint du serveur
        /// </summary>
        public event OnSocketAcceptedDel OnClientConnected;
        /// <summary>
        /// Déclenché quand le serveur n'a pas pu démarrer (port déjà utilisé, etc.)
        /// </summary>
        public event OnServerFailedToStartDel OnServerFailedToStart;
        /// <summary>
        /// Déclenché quand le serveur a démarré avec succès
        /// </summary>
        public event Action OnServerStarted;

        // Le socket d'écoute : attend les nouvelles connexions TCP
        private Socket m_Listen_Socket { get; set; }

        // L'adresse IP et le port sur lesquels le serveur écoute
        public IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// Crée un serveur sur l'adresse IP et le port indiqués (ex: "127.0.0.1", 5555)
        /// </summary>
        public SSyncServer(string ip,int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            m_Listen_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        // Appelée quand le serveur est bien démarré : déclenche l'événement OnServerStarted
        void OnListenSucces()
        {
            if (OnServerStarted != null)
                OnServerStarted();
        }

        /// <summary>
        /// Démarre le serveur :
        /// 1. Lie le socket à l'adresse IP:Port (Bind)
        /// 2. Met le socket en mode écoute (Listen) avec une file d'attente de 100 connexions
        /// 3. Commence à accepter les connexions de manière asynchrone
        /// </summary>
        public void Start()
        {
            try
            {
                m_Listen_Socket.Bind(EndPoint);
            }
            catch (Exception ex)
            {
                OnListenFailed(ex);
                return;
            }
            m_Listen_Socket.Listen(100); // 100 = taille de la file d'attente de connexions
            StartAccept(null);
            OnListenSucces();
            var a = "".Split(", ".ToCharArray());
        }

        // Appelée si le démarrage échoue : déclenche OnServerFailedToStart avec l'exception
        void OnListenFailed(Exception ex)
        {
            if (OnServerFailedToStart != null)
                OnServerFailedToStart(ex);
        }

        /// <summary>
        /// Lance (ou relance) une acceptation asynchrone de connexion.
        /// La première fois : crée un SocketAsyncEventArgs et branche AcceptEventCompleted.
        /// Ensuite : réinitialise AcceptSocket à null pour réutiliser l'objet.
        /// Si AcceptAsync retourne false, la connexion est déjà disponible → on traite directement.
        /// </summary>
        protected void StartAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += AcceptEventCompleted;
            }
            else
            {
                args.AcceptSocket = null; // Réinitialise pour la prochaine connexion
            }

            bool willRaiseEvent = m_Listen_Socket.AcceptAsync(args);
            if (!willRaiseEvent)
            {
                // La connexion était déjà prête : on la traite immédiatement (sans passer par l'événement)
                ProcessAccept(args);
            }
        }

        // Appelée quand l'acceptation asynchrone se termine (événement Completed)
        private void AcceptEventCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// Arrête le serveur et ferme le socket d'écoute
        /// </summary>
        public void Stop()
        {
            m_Listen_Socket.Shutdown(SocketShutdown.Both);
        }

        /// <summary>
        /// Traite une nouvelle connexion :
        /// 1. Notifie les abonnés via OnClientConnected (passe le socket du nouveau client)
        /// 2. Relance immédiatement l'acceptation pour le prochain client → boucle infinie
        /// </summary>
        void ProcessAccept(SocketAsyncEventArgs args)
        {
            if (OnClientConnected != null)
                OnClientConnected(args.AcceptSocket);
            StartAccept(args); // Boucle : recommence à attendre le prochain client
        }
    }
}
