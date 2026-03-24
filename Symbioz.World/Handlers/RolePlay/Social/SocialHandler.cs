using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Network;
using Symbioz.World.Records.Social;
using System.Collections.Generic;
using Symbioz.World.Records.Guilds;

namespace Symbioz.World.Handlers.RolePlay.Social
{
    public class SocialHandler
    {
        /// <summary>
        /// Reçu quand le client demande la liste d'amis du compte.
        /// Amis en ligne : FriendOnlineInformations (TypeId=92) avec GuildInformations (inclut GuildEmblem).
        /// Amis hors ligne : FriendInformations (TypeId=78).
        /// </summary>
        [MessageHandler]
        public static void HandleFriendGetList(FriendsGetListMessage message, WorldClient client)
        {
            List<SocialRecord> friendRecordAll = SocialRecord.GetSocialByAccountId(client.Account.Id);
            List<FriendInformations> friendAll = new List<FriendInformations>();
            foreach (var target in friendRecordAll)
            {
                var friend = WorldServer.Instance.GetOnlineClient((int)target.FriendAccountId);
                if (friend != null && friend.InGame)
                {
                    var guildInfo = friend.Character.HasGuild
                        ? friend.Character.Guild.Record.GetGuildInformations()
                        : new GuildInformations(0, "", 0, new GuildEmblem(0, 0, 0, 0));

                    friendAll.Add(new FriendOnlineInformations(
                        friend.Account.Id,
                        friend.Account.Nickname,
                        (sbyte)PlayerStateEnum.GAME_TYPE_ROLEPLAY,
                        0,
                        0,
                        (ulong)friend.Character.Id,
                        friend.Character.Name,
                        (byte)friend.Character.Level,
                        0,
                        friend.Character.Record.BreedId,
                        friend.Character.Record.Sex,
                        guildInfo,
                        0,
                        new PlayerStatus(friend.Character.Status.statusId)
                    ));
                }
                else
                    friendAll.Add(new FriendInformations((int)target.FriendAccountId, target.FriendName, (sbyte)PlayerStateEnum.NOT_CONNECTED, 0, 0));
            }
            client.Send(new FriendsListMessage(friendAll.ToArray()));
        }

        /// <summary>
        /// Reçu quand le client active/désactive les notifications de connexion d'ami.
        /// </summary>
        [MessageHandler]
        public static void HandleFriendSetWarnOnConnection(FriendSetWarnOnConnectionMessage message, WorldClient client)
        {
            client.Send(new FriendWarnOnConnectionStateMessage(message.enable));
        }

        /// <summary>
        /// Reçu quand le client demande les informations sur le conjoint.
        /// Pas de système de mariage implémenté : on répond avec un statut "sans conjoint".
        /// </summary>
        [MessageHandler]
        public static void HandleSpouseGetInformations(SpouseGetInformationsMessage message, WorldClient client)
        {
            client.Send(new SpouseStatusMessage(false));
        }

        /// <summary>
        /// Reçu quand le client veut ajouter un ami par nom de personnage.
        /// </summary>
        [MessageHandler]
        public static void FriendAddRequest(FriendAddRequestMessage message, WorldClient client)
        {
            var target = WorldServer.Instance.GetOnlineClient(message.name);
            if (target == null)
                client.Send(new FriendAddFailureMessage((sbyte)ListAddFailureEnum.LIST_ADD_FAILURE_NOT_FOUND));
            else if (target.Account.Id == client.Account.Id)
                client.Send(new FriendAddFailureMessage((sbyte)ListAddFailureEnum.LIST_ADD_FAILURE_EGOCENTRIC));
            else if (SocialRecord.CheckSocial(client.Account.Id, target.Account.Id))
                client.Send(new FriendAddFailureMessage((sbyte)ListAddFailureEnum.LIST_ADD_FAILURE_IS_DOUBLE));
            else
            {
                new SocialRecord(0, client.Account.Id, target.Account.Id, target.Account.Nickname).AddInstantElement();
                client.Send(new FriendAddedMessage(new FriendInformations(target.Account.Id, target.Account.Nickname, target.Character.Status.statusId, 0, 0)));
            }
        }

        /// <summary>
        /// Reçu quand le client demande la liste des joueurs ignorés du compte.
        /// Répond avec une liste vide (fonctionnalité non implémentée).
        /// </summary>
        [MessageHandler]
        public static void IgnoredGetList(IgnoredGetListMessage message, WorldClient client)
        {
            client.Send(new IgnoredListMessage(new IgnoredInformations[0]));
        }
    }
}
