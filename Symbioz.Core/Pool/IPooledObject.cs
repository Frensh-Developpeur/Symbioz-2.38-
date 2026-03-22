using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.Pool
{
    /// <summary>
    /// Interface à implémenter sur les objets qui peuvent être recyclés dans un ObjectPool.
    /// Quand un objet est rendu au pool via Recycle(), la méthode Cleanup() est appelée
    /// automatiquement pour remettre l'objet dans un état propre avant sa prochaine réutilisation.
    ///
    /// Exemple : réinitialiser les champs d'un paquet réseau avant de le remettre dans le pool.
    /// </summary>
    public interface IPooledObject
    {
        /// <summary>
        /// Remet l'objet dans son état initial en préparation du recyclage.
        /// À implémenter pour effacer les données de la session précédente.
        /// </summary>
        void Cleanup();
    }
}
