using SSync.IO;
using SSync.Messages;
using SSync.Transition;
using Symbioz.Core;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Messages;
using Symbioz.World.Network;
using Symbioz.World.Records;
using Symbioz.World.Records.Breeds;
using Symbioz.World.Records.Characters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.Approach
{
    /// <summary>
    /// Handler d'approche : gère la connexion initiale du client au serveur monde.
    /// C'est la première étape après la connexion TCP :
    ///   1. Le client envoie un ticket d'authentification reçu du serveur Auth
    ///   2. Le serveur Monde transmet ce ticket au serveur Auth via MessagePool
    ///   3. Le serveur Auth répond avec les données du compte (AccountMessage)
    ///   4. Le serveur Monde valide la connexion et prépare le compte
    /// </summary>
    class ApproachHandler
    {
        // Verrou pour protéger la gestion des tickets contre les connexions simultanées
        public static object m_locker = new object();

        [MessageHandler]
        public static void HandleClientKeyRequestMessage(ClientKeyMessage message, WorldClient client)
        {
            // Non implémenté : clé de chiffrement (inutilisée dans cet émulateur)
        }
        // Répond que le re-login par token n'est pas supporté
        [MessageHandler]
        public static void HandleReloginTokenRequestMessage(ReloginTokenRequestMessage message, WorldClient client)
        {
            client.Send(new ReloginTokenStatusMessage(false, new sbyte[0]));
        }
        // Reçoit le ticket d'authentification du client, l'envoie au serveur Auth pour validation
        [MessageHandler]
        public static void HandleAuthentificationTicketMessage(AuthenticationTicketMessage message, WorldClient client)
        {
            lock (m_locker)
            {
                // Le ticket est encodé : premier octet = longueur, puis les bytes ASCII du ticket
                var reader = new BigEndianReader(Encoding.ASCII.GetBytes(message.ticket));
                var count = reader.ReadByte();
                var ticket = reader.ReadUTFBytes(count);

                // Demande au serveur Auth si ce ticket est valide et récupère les données du compte
                MessagePool.SendRequest<AccountMessage>(TransitionServerManager.Instance.AuthServer, new AccountRequestMessage
                {
                    Ticket = ticket
                }, delegate (AccountMessage msg)
                {
                    OnAccountReceived(client, msg);
                },
                delegate ()
                {
                    OnAccountReceptionError(client);
                });
            }

        }
        // Erreur de communication avec le serveur Auth : déconnecte le client
        public static void OnAccountReceptionError(WorldClient client)
        {
            client.Disconnect();
        }
        // Compte reçu du serveur Auth : valide la session et initialise le client
        public static void OnAccountReceived(WorldClient client, AccountMessage message)
        {
            // Déconnecte tout autre client déjà connecté avec ce compte (connexion dupliquée)
            WorldServer.Instance.Disconnect(message.Account.Id);

            if (WorldServer.Instance.IsStatus(ServerStatusEnum.ONLINE))
            {
                client.Account = message.Account;
                client.OnAccountReceived();
            }
            else
            {
                client.Disconnect(); // Serveur hors ligne → refuse la connexion
            }

        }


    }
}
