using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.DesignPattern.StartupEngine
{
    /// <summary>
    /// Attribut C# utilisé pour marquer les méthodes à appeler automatiquement au démarrage.
    ///
    /// Usage :
    ///   [StartupInvoke("Nom affiché", StartupInvokePriority.Second)]
    ///   public static void MaMethode() { ... }
    ///
    /// Le StartupManager parcourt tous les types de l'assembly, trouve ces méthodes
    /// et les exécute dans l'ordre de priorité.
    /// </summary>
    public class StartupInvoke : Attribute
    {
        // Niveau de priorité : détermine l'ordre d'exécution (Primitive=0 en premier, Last=11 en dernier)
        public StartupInvokePriority Type { get; set; }

        // Si true, la méthode est exécutée mais son nom n'est pas affiché dans la console
        public bool Hided { get; set; }

        // Nom affiché dans la console lors du chargement : "(Second) Loading SQL Connection ..."
        public string Name { get; set; }

        /// <summary>
        /// Constructeur "visible" : la méthode sera affichée dans la console avec son nom
        /// </summary>
        public StartupInvoke(string name, StartupInvokePriority type)
        {
            this.Type = type;
            this.Name = name;
            this.Hided = false; // Visible dans la console
        }

        /// <summary>
        /// Constructeur "caché" : la méthode est exécutée silencieusement (pas de log)
        /// </summary>
        public StartupInvoke(StartupInvokePriority type)
        {
            this.Hided = true; // Pas de log dans la console
            this.Type = type;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
