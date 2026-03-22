using SSync.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbioz.Protocol.Messages;
using System.Threading.Tasks;
using Symbioz.World.Network;
using Symbioz.World.Models.Entities;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Dialogs;

namespace Symbioz.World.Handlers.RolePlay.Npcs
{
    /// <summary>
    /// Gère les interactions entre un joueur et un PNJ :
    ///   - HandleNpcGenericActionRequest : clic sur un PNJ (parler, acheter, se soigner...)
    ///   - HandleNpcDialogReplyMessage : sélection d'une réponse dans un dialogue de PNJ
    /// </summary>
    class NpcsHandler
    {
        // Reçu quand le joueur clique sur un PNJ. Vérifie que le PNJ est sur la bonne map,
        // puis délègue à Npc.InteractWith() pour exécuter l'action correspondante.
        [MessageHandler]
        public static void HandleNpcGenericActionRequest(NpcGenericActionRequestMessage message, WorldClient client)
        {
            if (message.npcMapId == client.Character.Map.Id)
            {
                Npc npc = client.Character.Map.Instance.GetEntity<Npc>((long)message.npcId);

                if (npc != null)
                {
                    npc.InteractWith(client.Character, (NpcActionTypeEnum)message.npcActionId);
                }
            }
            else
            {
                client.Character.ReplyError("Entity is not on map...");
            }
        }

        // Reçu quand le joueur choisit une réponse dans un dialogue de PNJ.
        // Vérifie que le personnage est bien en train de parler à un PNJ avant de traiter la réponse.
        [MessageHandler]
        public static void HandleNpcDialogReplyMessage(NpcDialogReplyMessage message, WorldClient client)
        {
            if (client.Character.Dialog is NpcTalkDialog)
            {
                client.Character.GetDialog<NpcTalkDialog>().Reply(message.replyId);
            }
        }
    }
}
