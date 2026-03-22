using SSync.Messages;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay.Social
{
    /// <summary>
    /// Gère les messages sociaux du client : liste d'amis et liste d'ignorés.
    /// Ces deux fonctionnalités ne sont pas encore implémentées sur ce serveur :
    /// le serveur répond avec des listes vides pour satisfaire le protocole client
    /// et éviter les erreurs d'initialisation côté interface.
    /// </summary>
    public class SocialHandler
    {
        /// <summary>
        /// Reçu quand le client demande la liste d'amis du compte.
        /// Répond avec une liste vide (fonctionnalité non implémentée).
        /// </summary>
        [MessageHandler]
        public static void HandleFriendGetList(FriendsGetListMessage message, WorldClient client)
        {
            // Liste d'amis vide : le système d'amis n'est pas implémenté
            client.Send(new FriendsListMessage(new FriendInformations[0]));
        }

        /// <summary>
        /// Reçu quand le client demande la liste des joueurs ignorés du compte.
        /// Répond avec une liste vide (fonctionnalité non implémentée).
        /// </summary>
        [MessageHandler]
        public static void IgnoredGetList(IgnoredGetListMessage message,WorldClient client)
        {
            // Liste d'ignorés vide : le système d'ignorés n'est pas implémenté
            client.Send(new IgnoredListMessage(new IgnoredInformations[0]));

        }
    }
}
