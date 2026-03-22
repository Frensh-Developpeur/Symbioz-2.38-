using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.DesignPattern.StartupEngine
{
    /// <summary>
    /// Ordre d'exécution des méthodes [StartupInvoke] au démarrage du serveur.
    /// Le StartupManager exécute les méthodes dans l'ordre croissant de ces valeurs.
    ///
    /// Exemple d'utilisation dans Symbioz.World :
    ///   Primitive (0) → Chargement de la configuration XML (world.xml)
    ///   First    (1)  → SafeRun, SSync (protocole réseau), ConsoleCommands, connexion Auth
    ///   Second   (2)  → Connexion à la base de données MySQL, chargement des tables
    ///   ...
    ///   Tenth   (10)  → Démarrage du serveur TCP (accepte les connexions joueurs)
    ///   Last    (11)  → Tâches finales (sauvegardes automatiques, etc.)
    /// </summary>
    public enum StartupInvokePriority
    {
        Primitive = 0,  // Avant tout : configuration
        First = 1,      // Très tôt : protocole réseau, commandes, connexion Auth
        Second = 2,     // Base de données
        Third = 3,
        Fourth = 4,
        Fifth = 5,
        Modules = 6,    // Chargement des modules/plugins
        Seventh = 7,
        Eighth = 8,
        Ninth = 9,
        Tenth = 10,     // Démarrage des serveurs TCP
        Last= 11,       // Tâches finales
    }
}
