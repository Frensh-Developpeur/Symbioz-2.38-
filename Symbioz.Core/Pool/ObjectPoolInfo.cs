using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.Pool
{
    /// <summary>
    /// Structure légère contenant des statistiques sur un pool d'objets.
    /// Permet de surveiller l'utilisation du pool : combien d'objets sont en mémoire forte
    /// (garantis non collectés par le GC) versus en référence faible (collectables).
    ///
    /// HardReferences : objets dont la durée de vie est garantie jusqu'au recyclage explicite.
    /// WeakReferences : objets maintenus en WeakReference, récupérables par le GC si besoin.
    /// </summary>
    public struct ObjectPoolInfo
    {
        /// <summary>Nombre d'objets avec une référence forte dans le pool (non collectables par le GC).</summary>
        public int HardReferences;

        /// <summary>Nombre d'objets avec une référence faible (peuvent être collectés par le GC si nécessaire).</summary>
        public int WeakReferences;

        /// <summary>
        /// Crée une instance avec les compteurs fournis.
        /// </summary>
        /// <param name="weak">Nombre de références faibles.</param>
        /// <param name="hard">Nombre de références fortes.</param>
        public ObjectPoolInfo(int weak, int hard)
        {
            this.HardReferences = hard;
            this.WeakReferences = weak;
        }
    }
}
