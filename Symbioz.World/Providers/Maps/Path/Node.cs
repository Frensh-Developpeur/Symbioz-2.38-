using System;
using System.Collections.Generic;
using System.Linq;
using _cell = Symbioz.World.Providers.Maps.Path.CoordCells.CellData;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Maps.Path
{
    /// <summary>
    /// Nœud de la grille isométrique utilisé par l'algorithme A*.
    /// Chaque nœud correspond à une cellule de la map et stocke :
    ///   - G : coût réel depuis le départ (nombre de pas × 10)
    ///   - H : heuristique (distance de Manhattan vers l'arrivée)
    ///   - F = G + H : score total utilisé pour trier la liste ouverte
    ///   - Parent : nœud précédent dans le chemin trouvé
    /// </summary>
    internal class Node : INode
    {
        // FIELDS
        private _cell m_cell;   // Données de la cellule (Id, X, Y, voisins)
        private Node m_parent;  // Nœud précédent dans le chemin (null pour le départ)
        private int m_g;        // Coût réel depuis le départ (chaque pas coûte 10)
        private int m_h;        // Heuristique : distance de Manhattan jusqu'à l'arrivée
        private bool m_walkable;// Indique si la cellule est praticable

        // CONSTRUCTOR
        public Node(_cell cell)
        {
            this.m_cell = cell;
        }

        // PROPERTIES
        public short CellID { get { return this.m_cell.Id; } }
        // Voisins en ligne droite (haut, bas, gauche, droite) — les seuls utilisés par le pathfinder
        public List<_cell> Neighbors { get { return this.m_cell.Line; } }

        public sbyte X { get { return this.m_cell.X; } }
        public sbyte Y { get { return this.m_cell.Y; } }
        // Le setter recalcule automatiquement G lors du changement de parent
        public Node Parent { get { return this.m_parent; } set { this.SetParent(value); } }

        public int G { get { return this.m_g; } }
        public int H { get { return this.m_h; } }
        // F = G + H : score total A*, la liste ouverte est triée par F croissant
        public int F { get { return this.m_g + this.H; } }
        public bool Walkable { get { return this.m_walkable; } set { this.m_walkable = value; } }

        // METHODS
        // Affecte le parent et recalcule le coût G (parent.G + 1 déplacement = +10)
        private void SetParent(Node parent)
        {
            this.m_parent = parent;
            if (parent != null)
                this.m_g = this.m_parent.G + 10;
        }

        // Calcule l'heuristique H via la distance de Manhattan (|ΔX| + |ΔY|).
        // C'est une estimation admissible : elle ne surestime jamais le coût réel.
        public void SetHeuristic(Node endPoint)
        {
            this.m_h = Math.Abs(this.X - endPoint.X) + Math.Abs(this.Y - endPoint.Y);
        }

        // Retourne le coût G qu'aurait ce nœud si son parent actuel était confirmé.
        // Utilisé pour comparer si un nouveau chemin vers ce nœud serait meilleur.
        public int CostWillBe()
        {
            return (this.m_parent != null ? this.m_parent.G + 10 : 0);
        }
    }

    /// <summary>
    /// Liste générique de nœuds A* avec accès par CellID et suppression du premier élément.
    /// Utilisée comme liste fermée (nœuds déjà traités) et comme base de SortedNodeList.
    /// </summary>
    internal class NodeList<T> : List<T> where T : INode
    {
        // Retire et retourne le premier élément de la liste (index 0)
        public T RemoveFirst()
        {
            T first = this[0];
            this.RemoveAt(0);
            return first;
        }

        // Vérifie si un nœud de même CellID est déjà dans la liste
        public new bool Contains(T node)
        {
            return this[node] != null;
        }

        // Indexeur par nœud : retourne le nœud de même CellID, ou default si absent
        public T this[T node]
        {
            get
            {
                foreach (T n in this)
                {
                    if (n.CellID == node.CellID) return n;
                }
                return default(T);
            }
        }
    }

    /// <summary>
    /// Liste ouverte A* maintenue triée par score F croissant.
    /// L'insertion dichotomique garantit que le meilleur nœud est toujours en tête (index 0).
    /// </summary>
    internal class SortedNodeList<T> : NodeList<T> where T : INode
    {
        // Insère le nœud à la bonne position par recherche binaire (O(log n)),
        // afin de conserver la liste triée par F croissant.
        public void AddDichotomic(T node)
        {
            int left = 0;
            int right = this.Count - 1;
            int center = 0;

            while (left <= right)
            {
                center = (left + right) / 2;
                if (node.F < this[center].F)
                    right = center - 1;
                else if (node.F > this[center].F)
                    left = center + 1;
                else
                {
                    left = center; // Score égal : insère à la position courante
                    break;
                }
            }
            this.Insert(left, node);
        }
    }

    /// <summary>
    /// Interface minimale requise par NodeList et SortedNodeList.
    /// Tout nœud doit exposer son score F et son identifiant de cellule.
    /// </summary>
    internal interface INode
    {
        int F { get; }
        short CellID { get; }
    }
}
