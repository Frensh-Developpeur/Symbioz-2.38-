using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.Pool
{
    /// <summary>
    /// Nœud d'une liste chaînée simple (single-linked list).
    /// Utilisé comme brique de base par LockFreeQueue pour construire une file d'attente
    /// sans verrou (lock-free) grâce aux opérations atomiques de Interlocked.
    ///
    /// Chaque nœud pointe vers le nœud suivant et contient la valeur stockée.
    /// </summary>
    /// <typeparam name="T">Type de la valeur stockée dans le nœud.</typeparam>
    public class SingleLinkNode<T>
    {
        /// <summary>Référence vers le nœud suivant dans la chaîne (null si dernier nœud).</summary>
        public SingleLinkNode<T> Next;

        /// <summary>Valeur stockée dans ce nœud.</summary>
        public T Item;
    }
}
