using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Maps.Path
{
    /// <summary>
    /// Table de correspondance statique entre les identifiants de cellules Dofus (0-559)
    /// et leurs coordonnées isométriques (X, Y).
    ///
    /// La grille Dofus est une isométrie de 14 colonnes × 20 rangées = 560 cellules.
    /// Le constructeur statique génère les coordonnées en deux passes (colonnes 0-13, puis 14-27)
    /// et précalcule les voisins en ligne et en diagonale pour chaque cellule.
    ///
    /// Utilisé par Pathfinder (voisins) et PathParser (distance, coordonnées).
    /// </summary>
    public static class CoordCells
    {
        // Table complète des 560 cellules, triée par Id, chargée au démarrage de l'application
        public static List<CellData> Cells = new List<CellData>();

        // Retourne les données d'une cellule par son identifiant (0-559)
        public static CellData GetCell(short id)
        {
            return Cells.FirstOrDefault(cell => cell.Id == id);
        }

        // Retourne la cellule aux coordonnées isométriques (x, y) données
        public static CellData GetCell(int x, int y)
        {
            return Cells.FirstOrDefault(cell => cell.X == x && cell.Y == y);
        }

        // Variables de calcul des coordonnées utilisées lors de la construction statique
        private static sbyte x = 0;
        private static sbyte y = 0;
        private static short ID = 0;

        // Génère les 560 cellules avec leurs coordonnées isométriques en deux passes :
        // - Première passe : colonnes 0 à 13 (moitié gauche de la grille, X=Y=0..13)
        // - Deuxième passe : colonnes 14 à 27 (moitié droite, X part de 1, Y part de 0)
        // Puis calcule les voisins de chaque cellule (ligne et diagonale).
        static CoordCells()
        {
            var cells = new List<CellData>();
            // Première passe : colonnes paires de la grille isométrique
            for (short i = 0; i < 14; i++)
            {
                var data = new sbyte[] { x, y };
                ID = i;
                // Parcourt toutes les cellules de cette colonne (pas de 28 dans le tableau linéaire)
                for (short j = i; j <= 560 - (28 - i); j += 28)
                {
                    cells.Add(new CellData() { Id = ID, X = data[0], Y = data[1] });
                    data[0]++;
                    data[1]--;
                    ID += 28;
                }
                x++;
                y++;
            }
            // Deuxième passe : colonnes impaires de la grille isométrique
            x = 1;
            y = 0;
            for (short i = 14; i < 28; i++)
            {
                var data = new sbyte[] { x, y };
                ID = i;
                for (short j = i; j <= 560 - (28 - i); j += 28)
                {
                    cells.Add(new CellData() { Id = ID, X = data[0], Y = data[1] });
                    data[0]++;
                    data[1]--;
                    ID += 28;
                }
                x++;
                y++;
            }
            // Trie par Id pour garantir un accès cohérent à l'index
            Cells = cells.OrderBy(cell => cell.Id).ToList();

            // Précalcule les voisins de chaque cellule (utilisé par le pathfinder)
            SearNeighbors();
        }

        // Pour chaque cellule, pré-calcule ses 4 voisins en ligne (orthogonaux)
        // et ses 4 voisins en diagonale, stockés dans CellData.Line et CellData.Diagonal.
        private static void SearNeighbors()
        {
            foreach (var cell in Cells)
            {
                // Voisins orthogonaux : gauche, droite, haut, bas (déplacement d'1 pas en X ou Y)
                var cells = Cells.FindAll(x => (x.X == cell.X - 1 && x.Y == cell.Y) ||
                                               (x.X == cell.X + 1 && x.Y == cell.Y) ||
                                               (x.Y == cell.Y - 1 && x.X == cell.X) ||
                                               (x.Y == cell.Y + 1 && x.X == cell.X));
                cell.Line = cells;

                // Voisins diagonaux : les 4 coins (déplacement d'1 pas en X et en Y simultanément)
                cells = Cells.FindAll(x => (x.X == cell.X - 1 && x.Y == cell.Y - 1) ||
                                           (x.X == cell.X - 1 && x.Y == cell.Y + 1) ||
                                           (x.X == cell.X + 1 && x.Y == cell.Y - 1) ||
                                           (x.X == cell.X + 1 && x.Y == cell.Y + 1));

                cell.Diagonal = cells;
            }
        }

        /// <summary>
        /// Données d'une cellule de la grille isométrique Dofus.
        /// Contient l'identifiant réseau, les coordonnées X/Y et les listes de voisins précalculées.
        /// </summary>
        public class CellData
        {
            public short Id { get; set; }   // Identifiant réseau de la cellule (0-559)
            public sbyte X { get; set; }    // Coordonnée X isométrique
            public sbyte Y { get; set; }    // Coordonnée Y isométrique

            public List<CellData> Line { get; set; }      // Voisins orthogonaux (4 directions)
            public List<CellData> Diagonal { get; set; }  // Voisins diagonaux (4 coins)
        }
    }
}
