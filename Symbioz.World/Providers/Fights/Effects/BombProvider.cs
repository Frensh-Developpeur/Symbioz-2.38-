using Symbioz.Core.DesignPattern;
using Symbioz.Protocol.Enums;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Marks;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects
{
    /// <summary>
    /// Gestionnaire des murs de bombes du Roublard.
    /// Quand une bombe est posée, déplacée ou téléportée, UpdateWalls() est appelé
    /// pour recalculer les murs entre les bombes du même joueur.
    /// Un mur se forme entre deux bombes du même type (même SpellId), dans les 4 directions
    /// diagonales (NE, NO, SE, SO) à une distance maximale de WALL_MAX_DISTANCE cases.
    /// </summary>
    public class BombProvider : Singleton<BombProvider>
    {
        // Distance maximale (en cases diagonales) entre deux bombes pour former un mur
        public const int WALL_MAX_DISTANCE = 6;

        // Les murs ne se forment qu'en diagonale (les 4 directions obliques)
        public DirectionsEnum[] WallDirections = new DirectionsEnum[]
       {
           DirectionsEnum.DIRECTION_NORTH_EAST,
           DirectionsEnum.DIRECTION_NORTH_WEST,
           DirectionsEnum.DIRECTION_SOUTH_EAST,
           DirectionsEnum.DIRECTION_SOUTH_WEST,
       };

        /// <summary>
        /// Recalcule tous les murs liés à cette bombe :
        /// 1. Supprime les murs devenus invalides (bombes déplacées)
        /// 2. Pour chaque direction diagonale, cherche une bombe alliée du même type
        ///    dans un rayon de WALL_MAX_DISTANCE cases
        /// 3. Si trouvée, crée un nouveau mur entre les deux bombes
        /// </summary>
        public void UpdateWalls(BombFighter fighter)
        {
            bool seq = fighter.Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);

            // Étape 1 : supprime les murs existants qui ne sont plus valides
            foreach (var wall in new List<Wall>(fighter.Walls))
            {
                if (wall.Valid() == false)
                {
                    wall.Destroy();
                }
            }

            // Étape 2 : cherche des bombes alliées du même type dans les directions diagonales
            foreach (var direction in WallDirections)
            {
                MapPoint current = fighter.Point.GetCellInDirection(direction, 1);

                for (byte i = 0; i < WALL_MAX_DISTANCE; i++)
                {
                    if (current != null)
                    {
                        current = current.GetCellInDirection(direction, 1);

                        if (current != null)
                        {
                            BombFighter target = fighter.Fight.GetFighter(current.CellId) as BombFighter;

                            // Vérifie que la cible est une bombe du même propriétaire et du même sort
                            if (target != null && target.IsOwner(fighter.Owner) && target.SpellBombRecord.SpellId == fighter.SpellBombRecord.SpellId)
                            {
                                // Supprime les anciens murs de la bombe cible qui incluaient la cellule courante
                                foreach (var targetWall in target.Walls.ToArray())
                                {
                                    if (targetWall.ContainsCell(fighter.CellId))
                                    {
                                        targetWall.Destroy();
                                    }
                                }
                                // Crée le nouveau mur entre les deux bombes
                                Wall wall = fighter.Fight.AddWall(fighter.Owner, fighter.WallSpellLevel, fighter.WallSpellLevel.Effects.FirstOrDefault(), fighter, target, i);

                                // Supprime les vieux murs qui chevauchent les cellules du nouveau mur
                                foreach (var cell in wall.GetCells())
                                {
                                    var wall2 = fighter.Walls.FirstOrDefault(x => x.ContainsCell(cell));

                                    if (wall2 != null)
                                        wall2.Destroy();

                                }

                                fighter.Walls.Add(wall);
                                target.Walls.Add(wall);
                                break; // Un seul mur par direction
                            }
                        }
                    }
                }

            }
            if (seq)
                fighter.Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }
    }
}
