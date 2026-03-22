using SSync.Messages;
using Symbioz.Protocol.Messages;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers
{
    /// <summary>
    /// Handler des messages de base du protocole réseau.
    /// Ces messages sont indépendants du contexte de jeu (ils fonctionnent même en dehors du jeu).
    /// </summary>
    class BasicHandler
    {
        /// <summary>
        /// Répond à un ping réseau envoyé par le client.
        /// Le client envoie régulièrement un BasicPingMessage pour vérifier que la connexion est toujours active.
        /// Le serveur répond avec un BasicPongMessage contenant le flag "quiet" (true = pas d'affichage côté client).
        /// Si le serveur ne répond pas, le client considère la connexion comme perdue et se déconnecte.
        /// </summary>
        [MessageHandler]
        public static void HandleBasicPing(BasicPingMessage message,WorldClient client)
        {
            client.Send(new BasicPongMessage(message.quiet));
        }
    }
}
