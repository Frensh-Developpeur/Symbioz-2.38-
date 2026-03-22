using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Arc
{
    /// <summary>
    /// Implémentation de test d'un client TCP héritant d'AbstractClient.
    /// Cette classe sert d'exemple ou de prototype : les méthodes importantes
    /// (OnConnected, OnDataArrival, OnFailToConnect) lèvent NotImplementedException
    /// et doivent être surchargées dans un vrai client.
    /// </summary>
    public class TestClient : AbstractClient
    {
        /// <summary>
        /// Constructeur qui accepte un socket déjà connecté (côté serveur).
        /// Alloue le buffer de réception et démarre immédiatement l'écoute asynchrone.
        /// </summary>
        public TestClient(Socket sock)
            : base(sock)
        {
            m_buffer = new byte[BufferLenght];
            // Démarre la réception asynchrone dès la création du client
            Socket.BeginReceive(m_buffer, 0, BufferLenght, SocketFlags.None, OnReceived, null);
        }

        /// <summary>
        /// Constructeur par défaut (utilisé quand on crée un client avant de le connecter).
        /// </summary>
        public TestClient()
        {

        }

        /// <summary>
        /// Appelé quand la connexion est fermée. À surcharger pour libérer des ressources.
        /// </summary>
        public override void OnClosed()
        {

        }

        /// <summary>
        /// Appelé quand la connexion est établie avec succès.
        /// Non implémenté dans la version de test.
        /// </summary>
        public override void OnConnected()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Appelé quand des données arrivent sur le socket.
        /// Non implémenté dans la version de test.
        /// </summary>
        public override void OnDataArrival(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Appelé quand la tentative de connexion échoue.
        /// Non implémenté dans la version de test.
        /// </summary>
        public override void OnFailToConnect(Exception ex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Envoie des données brutes de manière asynchrone via le socket.
        /// </summary>
        public override void Send(byte[] buffer)
        {
            Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSended, null);
        }

        /// <summary>
        /// Callback appelé quand l'envoi asynchrone est terminé. Corps vide dans cette version de test.
        /// </summary>
        public async void OnSended(IAsyncResult result)
        {

        }

        /// <summary>
        /// Lance une connexion asynchrone vers l'EndPoint configuré dans AbstractClient.
        /// </summary>
        public override void Connect(string host, int port)
        {
            Socket.BeginConnect(EndPoint, new AsyncCallback(OnConnectedAsync), Socket);
        }

        // Buffer de réception des données réseau
        byte[] m_buffer;

        // Taille du buffer de réception (8 Ko)
        public int BufferLenght = 8192;

        /// <summary>
        /// Callback appelé quand la connexion asynchrone aboutit. Corps vide dans cette version de test.
        /// </summary>
        public void OnConnectedAsync(IAsyncResult result)
        {

        }

        /// <summary>
        /// Callback appelé lors de chaque réception de données.
        /// Finalise la réception, transmet le buffer à OnDataArrival,
        /// puis relance immédiatement une nouvelle écoute asynchrone.
        /// </summary>
        public void OnReceived(IAsyncResult result)
        {
            Socket.EndReceive(result);
            //if (m_buffer[0] == 0)
            //{
            //    Disconnect();
            //    return;
            //}
            // Transmet les données reçues au handler de données
            OnDataArrival(m_buffer);
            // Relance l'écoute pour recevoir les prochains paquets
            Socket.BeginReceive(m_buffer, 0, BufferLenght, SocketFlags.None, OnReceived, null);
        }

        /// <summary>
        /// Déconnecte proprement le socket : coupe les deux sens puis ferme la connexion.
        /// </summary>
        public override void Disconnect()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
            OnClosed();
        }
    }
}
