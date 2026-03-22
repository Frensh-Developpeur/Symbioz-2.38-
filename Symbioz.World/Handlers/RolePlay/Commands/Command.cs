using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay.Commands
{
    /// <summary>
    /// Représente une commande de chat enregistrée dans CommandsHandler.
    /// Stocke le nom de la commande (ex. "item") et le rôle minimum requis pour l'exécuter.
    /// Utilisé comme clé dans le dictionnaire Commands de CommandsHandler.
    /// </summary>
    public class Command
    {
        public Command(string value, ServerRoleEnum role)
        {
            this.Value = value;
            this.MinimumRoleRequired = role;
        }

        // Nom de la commande tel que saisi après le préfixe "." (ex. "item", "go", "kick")
        public string Value { get; set; }

        // Niveau de rôle minimum pour pouvoir exécuter cette commande (Player, Moderator, Administrator, Fondator...)
        public ServerRoleEnum MinimumRoleRequired { get; set; }
    }
}
