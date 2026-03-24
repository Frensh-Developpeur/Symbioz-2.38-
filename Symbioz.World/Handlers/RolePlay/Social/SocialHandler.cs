using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Network;
using Symbioz.World.Records.Social;
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
            List <SocialRecord> friendRecordAll = SocialRecord.GetSocialByAccountId(client.Account.Id);
            List <FriendInformations> friendAll = new List<FriendInformations>();
            foreach (var target in friendRecordAll)
            {
                var friend = WorldServer.Instance.GetOnlineClient((int)target.FriendAccountId);
                if(friend != null && friend.InGame)
                {
                    friendAll.Add(new FriendInformations(friend.Account.Id, friend.Account.Nickname,
                        friend.Character.Status?.statusId ?? (sbyte)PlayerStatusEnum.PLAYER_STATUS_AVAILABLE, 0, 0));
                }
                else
                {
                    friendAll.Add(new FriendInformations((int)target.FriendAccountId, target.FriendName, (sbyte)PlayerStatusEnum.PLAYER_STATUS_OFFLINE, 0, 0));
                }

            }
            client.Send(new FriendsListMessage(friendAll.ToArray()));
        }





        /// <summary>

        /// </summary>
        [MessageHandler]
        public static void FriendAddRequest(FriendAddRequestMessage message,WorldClient client)
        {
            var target = WorldServer.Instance.GetOnlineClient(message.name);
            if(target == null)
            {
               client.Send(new FriendAddFailureMessage((sbyte)ListAddFailureEnum.LIST_ADD_FAILURE_NOT_FOUND));
            }
            else if(target.Account.Id == client.Account.Id)
            {
                client.Send(new FriendAddFailureMessage((sbyte)ListAddFailureEnum.LIST_ADD_FAILURE_EGOCENTRIC));
            }
            else if(SocialRecord.CheckSocial(client.Account.Id, target.Account.Id))
            {
                client.Send(new FriendAddFailureMessage((sbyte)ListAddFailureEnum.LIST_ADD_FAILURE_IS_DOUBLE));
            }
            else
            {
                
                 new SocialRecord(0, client.Account.Id, target.Account.Id, target.Account.Nickname).AddInstantElement();
                 client.Send(new FriendAddedMessage(new FriendInformations(target.Account.Id, target.Account.Nickname, target.Character.Status.statusId, 0,0)));
            }

        }





        [MessageHandler]
        public static void HandleSpouseGetInformations(SpouseGetInformationsMessage message, WorldClient client)
        {
            client.Send(new SpouseStatusMessage(false));
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
