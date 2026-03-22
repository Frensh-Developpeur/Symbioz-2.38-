using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay.Commands
{
    /// <summary>
    /// Attribut qui marque une méthode statique comme handler d'une commande de chat.
    ///
    /// Usage :
    ///   [ChatCommand("item", ServerRoleEnum.Moderator)]
    ///   public static void ItemCommand(string value, WorldClient client) { ... }
    ///
    /// CommandsHandler.LoadChatCommands() parcourt l'assembly par réflexion et enregistre
    /// toutes les méthodes annotées avec cet attribut dans le dictionnaire Commands.
    ///
    /// Si Role n'est pas précisé, la commande est accessible à tous (Player).
    /// </summary>
    public class ChatCommand : Attribute
    {
        // Rôle minimum requis pour exécuter cette commande
        public ServerRoleEnum Role { get; set; }

        // Nom de la commande (sans le préfixe ".")
        public string Name { get; set; }

        public ChatCommand(string name, ServerRoleEnum role)
        {
            this.Name = name;
            this.Role = role;
        }

        // Surcharge : commande accessible à tous les joueurs
        public ChatCommand(string name)
        {
            this.Name = name;
            this.Role = ServerRoleEnum.Player;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
