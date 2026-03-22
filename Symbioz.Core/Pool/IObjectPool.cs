using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.Pool
{
    /// <summary>
    /// Interface commune à tous les pools d'objets du serveur Symbioz.
    /// Permet de manipuler n'importe quel pool sans connaître le type exact des objets stockés.
    /// Utilisée par ObjectPoolMgr pour gérer les pools de manière générique.
    /// </summary>
    public interface IObjectPool
    {
        /// <summary>Nombre d'objets actuellement disponibles (non empruntés) dans le pool.</summary>
        int AvailableCount
        {
            get;
        }

        /// <summary>Nombre d'objets actuellement empruntés (en cours d'utilisation) hors du pool.</summary>
        int ObtainedCount
        {
            get;
        }

        /// <summary>
        /// Rend un objet au pool après utilisation.
        /// Si l'objet implémente IPooledObject, Cleanup() est appelé avant le recyclage.
        /// </summary>
        void Recycle(object obj);

        /// <summary>
        /// Emprunte un objet du pool (version non générique).
        /// Crée un nouvel objet si le pool est vide.
        /// </summary>
        object ObtainObj();
    }
}
