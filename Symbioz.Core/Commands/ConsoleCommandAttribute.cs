using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.Commands
{
    /// <summary>
    /// Attribut à placer sur les méthodes statiques pour les déclarer comme commandes console.
    /// Le nom fourni au constructeur devient le mot-clé à taper dans la console du serveur.
    ///
    /// Exemple d'utilisation :
    ///   [ConsoleCommand("reload")]
    ///   public static void ReloadCommand(string input) { ... }
    ///
    /// ConsoleCommands.Initialize() scanne l'assembly au démarrage et enregistre automatiquement
    /// toutes les méthodes portant cet attribut.
    /// </summary>
    public class ConsoleCommandAttribute : Attribute
    {
        /// <summary>Nom de la commande tel qu'il doit être tapé dans la console.</summary>
        public string Name { get; set; }

        /// <summary>
        /// Initialise l'attribut avec le nom de la commande.
        /// </summary>
        /// <param name="name">Mot-clé de la commande (insensible à la casse lors de l'appel).</param>
        public ConsoleCommandAttribute(string name)
        {
            this.Name = name;
        }
    }
}
