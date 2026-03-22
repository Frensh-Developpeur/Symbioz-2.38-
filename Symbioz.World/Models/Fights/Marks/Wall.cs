using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Spells;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Marks
{
    /// <summary>
    /// Mur de bombes : créé automatiquement quand deux bombes du Roublard se trouvent proches.
    /// Le mur est une ligne de cellules entre les deux bombes (forme 'L' = ligne).
    /// Il se déclenche à chaque tour (ON_TURN_STARTED), au passage d'un combattant (AFTER_MOVE),
    /// et lorsqu'un sort est lancé à travers lui (ON_CAST).
    /// Quand les deux bombes explosent ou disparaissent, le mur est détruit (Destroy).
    /// </summary>
    public class Wall : Mark
    {
        // Code de forme utilisé par la Zone pour calculer les cellules en ligne entre deux points
        public const char WALL_SHAPE = 'L';

        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.WALL;
            }
        }
        // Le mur interrompt le déplacement ; le combattant reçoit les dégâts du mur
        public override bool BreakMove
        {
            get
            {
                return true;
            }
        }
        // Première bombe du Roublard qui forme ce mur
        public BombFighter FirstBomb
        {
            get;
            private set;
        }
        // Deuxième bombe du Roublard qui forme ce mur
        public BombFighter SecondBomb
        {
            get;
            private set;
        }
        public Wall(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
            MapPoint startPoint, Color color, BombFighter firstBomb, BombFighter secondBomb, byte delta, DirectionsEnum direction)
            : base(id, source, spellLevel, effect, startPoint, new Zone(WALL_SHAPE, delta, direction), color,
            // Le mur se déclenche à 3 occasions : début de tour, lancer de sort, passage
            MarkTriggerTypeEnum.ON_TURN_STARTED | MarkTriggerTypeEnum.ON_CAST | MarkTriggerTypeEnum.AFTER_MOVE)
        {
            this.FirstBomb = firstBomb;
            this.SecondBomb = secondBomb;
        }

        /// <summary>
        /// Vérifie si le mur est toujours valide :
        /// - Les deux bombes doivent être à l'intérieur ou adjacentes à la zone du mur
        /// - Aucune bombe du poseur ne doit se trouver sur les cellules du mur
        /// Si le mur n'est plus valide, il sera supprimé.
        /// </summary>
        public bool Valid()
        {
            var cells = this.Zone.GetCells(CenterPoint.CellId, Fight.Map);

            // Calcule la direction de chaque bombe vers l'autre
            var firstDirection = this.FirstBomb.Point.OrientationTo(SecondBomb.Point);
            var secondDirection = this.SecondBomb.Point.OrientationTo(FirstBomb.Point);

            // Vérifie que la cellule juste devant chaque bombe (vers l'autre) est dans le mur
            var firstPoint = FirstBomb.Point.GetCellInDirection(firstDirection, 1);
            var secondPoint = SecondBomb.Point.GetCellInDirection(secondDirection, 1);

            if (!cells.Contains(firstPoint.CellId) || !cells.Contains(secondPoint.CellId))
            {
                return false;
            }

            // Le mur est invalide si une bombe du même poseur se trouve sur ses cellules
            foreach (var cell in cells)
            {
                var bomb = Fight.GetFighter(cell) as BombFighter;

                if (bomb != null && bomb.Owner == this.Source)
                {
                    return false;
                }
            }

            return true;
        }

        // Déclenché quand un combattant traverse le mur : applique les dégâts du SpellLevel sur lui
        public override void Trigger(Fighter source, MarkTriggerTypeEnum type, object token)
        {
            bool seq = Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);
            SpellEffectsManager.Instance.HandleEffects(Source, SpellLevel, source.Point, false);

            if (seq)
                Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }
        // Détruit le mur : le retire des listes de murs des deux bombes et du terrain de combat
        public void Destroy()
        {
            FirstBomb.Walls.Remove(this);
            SecondBomb.Walls.Remove(this);
            Fight.RemoveMark(FirstBomb, this);
        }
    }
}
