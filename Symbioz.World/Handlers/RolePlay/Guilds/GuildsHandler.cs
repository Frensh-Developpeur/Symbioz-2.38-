using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Network;
using Symbioz.World.Providers.Guilds;
using System;
using System.Collections.Generic;
using System.Linq;
using Symbioz.World.Models.Dialogs.DialogBox;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Guilds;

namespace Symbioz.World.Handlers.RolePlay.Guilds
{
    /// <summary>
    /// Gère les messages réseau liés aux guildes :
    ///   - Modification du message du jour (MOTD)
    ///   - Création de guilde
    ///   - Invitation et réponse à une invitation
    ///   - Exclusion d'un membre
    ///   - Modification des paramètres d'un membre (droits, rang, XP)
    ///   - Demande d'informations (général, membres...)
    /// </summary>
    class GuildsHandler
    {
        // Reçu quand le meneur de guilde modifie le message du jour.
        // Les lignes commentées désactivaient un assainissement du contenu (guillemets).
        [MessageHandler]
        public static void HandleGuildMotdSetRequest(GuildMotdSetRequestMessage message, WorldClient client)
        {
            if (client.Character.HasGuild)
            {
              //  message.content = message.content.Replace('"', ' ');
             //   message.content = message.content.Replace('\'', ' ');
                client.Character.Guild.SetMotd(client.Character.GuildMember, message.content);
            }
        }
        // Reçu quand le joueur valide la création d'une guilde (nom + emblème).
        // Délègue à GuildProvider qui vérifie les règles (nom unique, pas déjà en guilde...).
        [MessageHandler]
        public static void HandleGuildCreationRequest(GuildCreationValidMessage message, WorldClient client)
        {
            GuildCreationResultEnum result = GuildProvider.Instance.CreateGuild(client.Character, message.guildName, message.guildEmblem);
            client.Character.OnGuildCreated(result);
        }

        // Reçu quand un membre autorisé invite un autre joueur dans la guilde.
        // Vérifie plusieurs cas d'erreur avant d'ouvrir la boite de dialogue d'invitation.
        [MessageHandler]
        public static void HandleGuildInvitationMessage(GuildInvitationMessage message, WorldClient client)
        {
            if (client.Character.GuildMember.HasRight(GuildRightsBitEnum.GUILD_RIGHT_INVITE_NEW_MEMBERS))
            {
                var target = WorldServer.Instance.GetOnlineClient((long)message.targetId);

                if (target == null)
                    client.Character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 208); // Joueur introuvable
                else if (target.Character.HasGuild)
                    client.Character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 206); // Déjà en guilde
                else if (target.Character.Status.statusId != (sbyte)PlayerStatusEnum.PLAYER_STATUS_AVAILABLE)
                {
                    client.Character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 209);
                }
                else if (target.Character.Busy)
                    client.Character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 209); // Occupé
                else if (!client.Character.Guild.CanAddMember())
                    client.Character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 55, GuildProvider.MAX_MEMBERS_COUNT); // Guilde pleine
                else
                {
                    target.Character.OpenRequestBox(new GuildInvitation(client.Character, target.Character));
                }
            }
        }

        // Reçu quand un officier demande d'exclure un membre.
        // Si kicked = false, le membre se retire lui-même (quitter la guilde).
        [MessageHandler]
        public static void HandleGuildKickRequestMessage(GuildKickRequestMessage message, WorldClient client)
        {
            if (!client.Character.HasGuild)
                return;

            var target = client.Character.Guild.GetMember((long)message.kickedId);

            if (client.Character.Guild == target.Guild)
            {
                bool kicked = target.Id != client.Character.Id; // false = le joueur se retire lui-même
                target.Guild.Leave(target, kicked);
            }
        }

        // Reçu quand la cible répond à une invitation de guilde.
        [MessageHandler]
        public static void HandleGuildInvitationAnswerMessage(GuildInvitationAnswerMessage message, WorldClient client)
        {
            if (!client.Character.HasGuild && client.Character.RequestBox is GuildInvitation)
            {
                if (message.accept)
                    client.Character.RequestBox.Accept();
                else
                    client.Character.RequestBox.Deny();
            }
        }

        // Reçu quand un officier modifie le rang, les droits ou le pourcentage d'XP d'un membre.
        [MessageHandler]
        public static void HandleGuildChangeMemberParameters(GuildChangeMemberParametersMessage message, WorldClient client)
        {
            if (client.Character.HasGuild)
            {
                GuildMemberInstance member = client.Character.Guild.GetMember((long)message.memberId);

                if (member != null && member.Guild == client.Character.Guild)
                {
                    member.ChangeParameters(client.Character.GuildMember, message.rights, message.rank, message.experienceGivenPercent);

                }
            }
        }

        // Reçu quand le client demande des informations sur la guilde.
        // Seuls INFO_GENERAL et INFO_MEMBERS sont implémentés ; les autres (maisons, paddocks...) sont ignorés.
        [MessageHandler]
        public static void HandleGuildGetInformations(GuildGetInformationsMessage message, WorldClient client)
        {
            switch ((GuildInformationsTypeEnum)message.infoType)
            {
                case GuildInformationsTypeEnum.INFO_GENERAL:
                    SendGuildInformationsGeneral(client);
                    break;
                case GuildInformationsTypeEnum.INFO_MEMBERS:
                    SendGuildInformationsMembers(client);
                    break;
                case GuildInformationsTypeEnum.INFO_BOOSTS:
                    break;
                case GuildInformationsTypeEnum.INFO_PADDOCKS:
                    break;
                case GuildInformationsTypeEnum.INFO_HOUSES:
                    break;
                case GuildInformationsTypeEnum.INFO_TAX_COLLECTOR_GUILD_ONLY:
                    break;
                case GuildInformationsTypeEnum.INFO_TAX_COLLECTOR_ALLIANCE:
                    break;
                case GuildInformationsTypeEnum.INFO_TAX_COLLECTOR_LEAVE:
                    break;
            }
        }

        // Envoie la liste des membres de la guilde au client.
        public static void SendGuildInformationsMembers(WorldClient client)
        {
            client.Send(client.Character.Guild.GetGuildInformationsMembersMessage());
        }

        // Envoie les informations générales de la guilde (nom, emblème, niveau, XP) au client.
        public static void SendGuildInformationsGeneral(WorldClient client)
        {
            client.Send(client.Character.Guild.GetGuildInformationsGeneralMessage());
        }


    }
}
