using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records;
using Symbioz.World.Records.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Maps.Path
{
    /// <summary>
    /// Algorithme de recherche de chemin A* adapté à la grille isométrique de Dofus.
    /// Calcule le chemin le plus court entre deux cellules d'une map,
    /// en tenant compte des cellules non praticables (murs, obstacles).
    /// Utilisé pour le déplacement des personnages hors combat et des monstres en IA.
    /// LINE_COST est le coût d'un déplacement en ligne (utilisé pour l'heuristique A*).
    /// </summary>
    internal class Pathfinder
    {
        // Coût d'un déplacement en ligne droite (utilisé pour le calcul de distance heuristique)
        public const byte LINE_COST = 10;

        // Toutes les cellules de la map transformées en nœuds pour l'algorithme
        private List<Node> m_cells;
        // Nœud de départ et nœud d'arrivée pour le pathfinding
        private Node m_start;
        private Node m_end;

        private SortedNodeList<Node> m_openList;

        private NodeList<Node> m_closeList;

        public Pathfinder(MapRecord map, short start, short end)
        {
            this.m_cells = GetNodes(map);

            this.m_start = this.m_cells.FirstOrDefault(x => x.CellID == start);
            this.m_end = this.m_cells.FirstOrDefault(x => x.CellID == end);

            this.m_openList = new SortedNodeList<Node>();
            this.m_closeList = new NodeList<Node>();
        }
        public Pathfinder(MapRecord map, short start)
        {
            this.m_cells = GetNodes(map);

            this.m_start = this.m_cells.FirstOrDefault(x => x.CellID == start);

            this.m_openList = new SortedNodeList<Node>();
            this.m_closeList = new NodeList<Node>();
        }

        // Calcule le chemin de m_start vers m_end (définis dans le constructeur).
        // Retourne la liste ordonnée des cellules à traverser (sans la cellule de départ),
        // ou null si aucun chemin n'existe.
        public List<short> FindPath()
        {
            // Force la destination à être praticable (elle peut être bloquée par un obstacle)
            this.m_end.Walkable = true;
            this.m_openList.Add(this.m_start);

            while (this.m_openList.Count > 0)
            {
                // Prend le nœud avec le meilleur score F (coût + heuristique)
                var bestCell = this.m_openList.RemoveFirst();
                if (bestCell.CellID == this.m_end.CellID)
                {
                    // Chemin trouvé : remonte la chaîne de parents pour reconstruire le chemin
                    var sol = new List<short>();
                    while (bestCell.Parent != null && bestCell != this.m_start)
                    {
                        sol.Add(bestCell.CellID);
                        bestCell = bestCell.Parent;
                    }
                    sol.Reverse(); // Le chemin était construit à l'envers (de fin → début)

                    this.m_end.Walkable = false;
                    return sol;
                }
                this.m_closeList.Add(bestCell);
                this.AddToOpen(bestCell, this.GetNeighbors(bestCell));
            }
            // Liste ouverte vide : aucun chemin possible
            this.m_end.Walkable = false;
            return null;
        }

        // Surcharge permettant de définir la destination au moment de l'appel,
        // utile quand le Pathfinder est construit sans destination (constructeur à 2 paramètres).
        public List<short> FindPath(short target)
        {
            this.m_end = this.m_cells.FirstOrDefault(x => x.CellID == target);
            this.m_end.Walkable = true;

            this.m_openList.Add(this.m_start);

            while (this.m_openList.Count > 0)
            {
                var bestCell = this.m_openList.RemoveFirst();
                if (bestCell.CellID == this.m_end.CellID)
                {
                    var sol = new List<short>();
                    while (bestCell.Parent != null && bestCell != this.m_start)
                    {
                        sol.Add(bestCell.CellID);
                        bestCell = bestCell.Parent;
                    }
                    sol.Reverse();

                    this.m_end.Walkable = false;
                    return sol;
                }
                this.m_closeList.Add(bestCell);
                this.AddToOpen(bestCell, this.GetNeighbors(bestCell));
            }
            this.m_end.Walkable = false;
            return null;
        }

        // Retourne les voisins praticables d'un nœud (les cellules adjacentes non bloquées).
        // Affecte le parent si la cellule n'en a pas encore, pour permettre la reconstruction du chemin.
        private List<Node> GetNeighbors(Node node)
        {
            var nodes = new List<Node>();

            node.Neighbors.ForEach(x =>
            {
                var cell = this.m_cells.FirstOrDefault(y => y.CellID == x.Id);
                if (cell != null && cell.Walkable)
                {
                    if (cell.Parent == null)
                        cell.Parent = node;

                    nodes.Add(cell);
                }
            });

            return nodes;
        }

        // Retourne le nœud avec la plus petite heuristique H (distance estimée à l'arrivée).
        // Note : OrderBy ne modifie pas la liste en place, mais First() retourne quand même le premier élément.
        private Node GetBestNode()
        {
            this.m_openList.OrderBy(x => x.H);
            return this.m_openList.First();
        }

        // Marque comme non praticables les cellules occupées par les combattants,
        // afin que le pathfinder les évite (sauf pour la cellule de départ du monstre lui-même).
        public void PutEntities(List<Fighter> fighters)
        {
            foreach (var fighter in fighters)
            {
                var node = this.m_cells.FirstOrDefault(x => x.CellID == fighter.CellId);
                if (node != this.m_start)
                    node.Walkable = false;
            }
        }

        // Convertit toutes les cellules d'une map en nœuds A*.
        // La grille Dofus contient toujours 560 cellules (14 colonnes × 40 demi-lignes).
        public static List<Node> GetNodes(MapRecord map)
        {
            var nodes = new List<Node>();
            for (short cell = 0; cell < 560; cell++)
            {
                var node = new Node(CoordCells.GetCell(cell));
                // Une cellule est praticable si la map l'autorise (pas un mur, obstacle, etc.)
                node.Walkable = map.Walkable((ushort)cell);
                nodes.Add(node);
            }
            return nodes;
        }

        // Ajoute les voisins dans la liste ouverte selon les règles A* :
        // - Si le nœud n'est pas encore traité, on l'insère de façon dichotomique (liste triée par F).
        // - Si le nœud est déjà dans la liste ouverte avec un coût plus élevé, on met à jour son parent.
        private void AddToOpen(Node current, IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                if (!this.m_openList.Contains(node))
                {
                    if (!this.m_closeList.Contains(node))
                        this.m_openList.AddDichotomic(node); // Insertion triée pour maintenir l'ordre
                }
                else
                {
                    // Si on trouve un chemin moins coûteux vers ce nœud, on le met à jour
                    if (node.CostWillBe() < this.m_openList[node].G)
                        node.Parent = current;
                }
            }
        }
    }
}
