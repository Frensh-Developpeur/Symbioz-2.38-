using SSync.Arc;
using SSync.IO;
using SSync.Messages;
using SSync.Sockets;
using Symbioz.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SSync
{
    /// <summary>
    /// Client réseau SSync : gère la connexion à un serveur distant et l'envoi/réception de messages.
    ///
    /// SSyncClient hérite de Client (gestion bas niveau du socket) et surcharge OnDataArrival()
    /// pour deserialiser les messages reçus via SSyncCore.BuildMessage() et les dispatcher
    /// aux handlers enregistrés via SSyncCore.HandleMessage().
    ///
    /// Si aucun handler ne peut traiter un message reçu, l'événement OnMessageHandleFailed est déclenché
    /// pour permettre au code appelant de gérer ce cas (ex. messages Transition inter-serveurs).
    /// </summary>
    public class SSyncClient : Client
    {
        static Logger logger = new Logger();

        // Déclenché quand aucun handler ne peut traiter un message reçu
        public event Action<Message> OnMessageHandleFailed;

        public SSyncClient()
            : base()
        {
        }

        // Réception de données : désérialise le buffer en Message et dispatch vers le bon handler
        public override void OnDataArrival(byte[] buffer)
        {
            Message message = SSyncCore.BuildMessage(buffer);
            if (!SSyncCore.HandleMessage(message, this))
            {
                if (OnMessageHandleFailed != null)
                    OnMessageHandleFailed(message);
            }
        }

        public SSyncClient(Socket sock)
            : base(sock)
        {
        }

        // Sérialise et envoie un message au serveur distant.
        // Utilise CustomDataWriter (Big Endian) pour la sérialisation conforme au protocole Dofus.
        public void Send(Message message)
        {
            if (Socket != null && Socket.Connected)
            {
                CustomDataWriter writer = new CustomDataWriter();
                message.Pack(writer);
                var packet = writer.Data;
                this.Send(packet);
                if (SSyncCore.ShowProtocolMessage)
                    logger.DarkGray(string.Format("Send {0}", message.ToString()));
            }
        }
    }
}
