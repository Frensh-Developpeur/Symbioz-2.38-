using System;                                   
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Arc
{
    /// <summary>
    /// Classe de base abstraite pour tous les clients réseau (côté serveur ET côté client).
    ///
    /// Définit le contrat minimal qu'un client doit implémenter :
    ///   - Événements de connexion/déconnexion (OnConnected, OnClosed)
    ///   - Réception de données brutes (OnDataArrival)
    ///   - Envoi de données (Send)
    ///   - Connexion/Déconnexion (Connect, Disconnect)
    ///
    /// Héritée par :
    ///   - Client.cs (SSync) → implémentation concrète bas niveau avec SocketAsyncEventArgs
    ///   - SSyncClient.cs → ajoute la sérialisation des messages Dofus
    ///   - WorldClient.cs (Symbioz.World) → représente un joueur connecté au serveur
    /// </summary>
    public abstract class AbstractClient
    {
        /// <summary>
        /// Constructeur pour un client sortant (qui va se connecter à un serveur)
        /// Crée un nouveau socket TCP IPv4
        /// </summary>
        public AbstractClient()
        {
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Constructeur pour un client entrant (accepté par le serveur)
        /// Réutilise le socket déjà créé par SSyncServer lors de l'acceptation
        /// </summary>
        public AbstractClient(Socket sock)
        {
            this.Socket = sock;
        }

        // Adresse IP + port du client distant (ex: 192.168.1.10:12345)
        public IPEndPoint EndPoint
        {
            get
            {
                return Socket.RemoteEndPoint as IPEndPoint;
            }
        }

        // Adresse IP du client sous forme de chaîne (ex: "192.168.1.10")
        public string Ip
        {
            get
            {
                return EndPoint.Address.ToString();
            }
        }

        // Appelée quand la connexion TCP est fermée (proprement ou par coupure)
        public abstract void OnClosed();

        // Appelée quand la connexion TCP est établie avec succès
        public abstract void OnConnected();

        // Le socket TCP sous-jacent (protégé : accessible par les classes héritées)
        protected Socket Socket
        {
            get;
            set;
        }

        // Retourne true si le socket est actuellement connecté
        public bool IsConnected
        {
            get
            {
                return Socket.Connected;
            }
        }

        // Appelée quand des données brutes arrivent du réseau (buffer = bytes reçus)
        public abstract void OnDataArrival(byte[] buffer);

        // Appelée si la tentative de connexion à un serveur distant échoue
        public abstract void OnFailToConnect(Exception ex);

        // Envoie des bytes bruts sur le réseau
        public abstract void Send(byte[] buffer);

        // Se connecte à un serveur distant (host = IP ou nom, port = numéro de port)
        public abstract void Connect(string host, int port);

        // Ferme la connexion et libère les ressources
        public abstract void Disconnect();
    }
}
