using SSync.Messages;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Handlers.RolePlay.Commands;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Symbioz.World.Handlers.RolePlay.Chat
{

    /// <summary>
    /// Gère les messages de chat envoyés par les clients et les distribue aux bons canaux.
    ///
    /// Architecture :
    ///   - ChatHandlers : dictionnaire {canal → méthode} chargé par réflexion depuis ChatChannels
    ///     via l'attribut [ChatChannel(ChatActivableChannelsEnum.X)]
    ///   - Handle() : dispatch central qui détecte d'abord les commandes (préfixe /) puis achemine
    ///     le message au bon handler de canal
    ///
    /// Canaux gérés : GLOBAL, TRADE, RECRUITMENT, GENERAL, GUILD, TEAM, GROUP + chat privé
    /// </summary>
    class ChatHandler
    {
        // Type de délégué pour les handlers de canal (un par canal de chat)
        public delegate void ChatHandlerDelegate(WorldClient client, string message);

        // Dictionnaire des handlers de canal, rempli par réflexion dans Initialize()
        public static readonly Dictionary<ChatActivableChannelsEnum, ChatHandlerDelegate> ChatHandlers = new Dictionary<ChatActivableChannelsEnum, ChatHandlerDelegate>();

        // Charge les handlers de canal depuis la classe ChatChannels via l'attribut [ChatChannel]
        [StartupInvoke("Chat Channels", StartupInvokePriority.Eighth)]
        public static void Initialize()
        {
            foreach (var method in typeof(ChatChannels).GetMethods())
            {
                var attributes = method.GetCustomAttributes(typeof(ChatChannelAttribute), false);
                if (attributes.Count() > 0)
                {
                    var attribute = attributes[0] as ChatChannelAttribute;
                    ChatHandlers.Add(attribute.Channel, (ChatHandlerDelegate)Delegate.CreateDelegate(typeof(ChatHandlerDelegate), method));
                }
            }
        }

        // Dispatch central : si le message commence par le préfixe de commande, le délègue à CommandsHandler.
        // Sinon, cherche le handler du canal et l'appelle.
        static void Handle(WorldClient client, string message, ChatActivableChannelsEnum channel)
        {
            if (message.StartsWith(CommandsHandler.CommandsPrefix))
            {
                CommandsHandler.Handle(message, client);
                return;
            }

            var handler = ChatHandlers.FirstOrDefault(x => x.Key == channel);
            if (handler.Value != null)
                handler.Value(client, message);
            else
                client.Character.Reply("Ce chat n'est pas géré");
        }

        // Affiche une émoticône du personnage à tous les joueurs de la map
        [MessageHandler]
        public static void HandleChatSmileyRequest(ChatSmileyRequestMessage message, WorldClient client)
        {
            client.Character.DisplaySmiley(message.smileyId);
        }

        // Message de chat avec objet lié (ex. lien vers un item dans le chat global)
        [MessageHandler]
        public static void HandleChatClientMultiWithObject(ChatClientMultiWithObjectMessage message, WorldClient client)
        {
            client.Character.SendMap(ChatChannels.GetChatServerWithObjectMessage(ChatActivableChannelsEnum.CHANNEL_GLOBAL, message.objects, message.content, client));
        }

        // Message de chat multi (canal général, commerce, recrutement...) - dispatch via Handle()
        [MessageHandler]
        public static void HandleChatMultiClient(ChatClientMultiMessage message, WorldClient client)
        {
            Handle(client, message.content, (ChatActivableChannelsEnum)message.channel);
        }

        // Message privé : vérifie que le joueur ne s'envoie pas un message à lui-même,
        // puis transmet au destinataire s'il est en ligne
        [MessageHandler]
        public static void ChatClientPrivate(ChatClientPrivateMessage message, WorldClient client)
        {
            if (message.receiver == client.Character.Name)
            {
                client.Character.OnChatError(ChatErrorEnum.CHAT_ERROR_INTERIOR_MONOLOGUE);
                return;
            }

            WorldClient target = WorldServer.Instance.GetOnlineClient(message.receiver);

            if (target != null)
            {
                if(target.Character.Status.statusId == (sbyte)PlayerStatusEnum.PLAYER_STATUS_IDLE)
                {
                    target.Send(ChatChannels.GetChatServerMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client));
                    client.Send(ChatChannels.GetChatServerCopyMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client, target));
                    client.Character.Reply($"Le joueur est absent.");
                }
                else if(target.Character.Status.statusId == (sbyte)PlayerStatusEnum.PLAYER_STATUS_SOLO)
                {
                    client.Character.Reply($"Le joueur ne peut pas recevoir de message pour le moment.");
                }
                else if(target.Character.Status.statusId == (sbyte)PlayerStatusEnum.PLAYER_STATUS_PRIVATE)
                {
                    // faire un if pour Friend (voir plus tard)
                    // Pour l'instant, il ne filtre rien.
                    target.Send(ChatChannels.GetChatServerMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client));
                    client.Send(ChatChannels.GetChatServerCopyMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client, target));
                }
                else if (target.Character.Status.statusId == (sbyte)PlayerStatusEnum.PLAYER_STATUS_AFK)
                {
                    
                    target.Send(ChatChannels.GetChatServerMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client));
                    client.Send(ChatChannels.GetChatServerCopyMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client, target));
                    
                    /// peut recevoir un message privé et on en renvoie un
                    if(target.Character.Status is PlayerStatusExtended extMessage)
                    {
                        client.Character.Reply($"Le joueur est absent - Message automatique : {extMessage.message} ");
                    }
                    else
                    {
                        client.Character.Reply($"Le joueur est absent.");
                    }
                }
                else
                {
                    target.Send(ChatChannels.GetChatServerMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client));
                    client.Send(ChatChannels.GetChatServerCopyMessage(ChatActivableChannelsEnum.PSEUDO_CHANNEL_PRIVATE, message.content, client, target));
                }

            }
            else
            {
                client.Character.OnChatError(ChatErrorEnum.CHAT_ERROR_RECEIVER_NOT_FOUND);
            }
            
        }

        // Envoie un message d'annonce coloré à tous les joueurs connectés (utilisé par les GMs)
        public static void SendAnnounceMessage(string value, Color color)
        {
            WorldServer.Instance.Send(new TextInformationMessage(0, 0, new string[] { string.Format("<font color=\"#{0}\">{1}</font>", color.ToArgb().ToString("X"), value) }));
        }

        // Envoie une notification de serveur à tous les joueurs (notification système)
        public static void SendNotificationToAllMessage(string message)
        {
            WorldServer.Instance.Send(new NotificationByServerMessage(24, new string[] { message }, true));
        }
    }
}
