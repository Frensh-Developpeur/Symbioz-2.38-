using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Marks.Shapes;
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
    /// Classe abstraite représentant une marque posée sur le terrain de combat.
    /// Les marques sont des zones d'effet persistantes placées par des sorts :
    ///   - Trap (piège) : se déclenche quand un combattant marche dessus
    ///   - Glyph (glyphe) : se déclenche à chaque tour ou au passage
    ///   - Portal (portail) : téléporte les combattants qui le traversent
    ///   - Wall (mur) : créé par les bombes du Roublard
    ///   - Rune, AuraGlyph...
    ///
    /// Chaque marque a un ID unique, une cellule centrale, une zone d'effet,
    /// un sort à déclencher (TriggerSpell) et un combattant source.
    /// </summary>
    public abstract class Mark
    {
        // Si true, le passage sur une cellule de cette marque interrompt le déplacement
        public abstract bool BreakMove
        {
            get;
        }
        // Type de marque (TRAP, GLYPH, PORTAL, WALL...)
        public abstract GameActionMarkTypeEnum Type
        {
            get;
        }
        // Identifiant unique de cette marque dans le combat
        public short Id
        {
            get;
            private set;
        }
        // Combattant qui a posé cette marque
        public Fighter Source
        {
            get;
            private set;
        }
        // Combat dans lequel cette marque existe
        public Fight Fight
        {
            get
            {
                return Source.Fight;
            }
        }
        // Niveau du sort qui a créé cette marque (pour les effets)
        public SpellLevelRecord SpellLevel
        {
            get;
            private set;
        }
        // Effet de base associé à cette marque
        public EffectInstance BaseEffect
        {
            get;
            private set;
        }
        // Cellule centrale de la marque
        public MapPoint CenterPoint
        {
            get;
            private set;
        }
        // Zone d'effet de la marque (forme géométrique)
        public Zone Zone
        {
            get;
            private set;
        }
        // Formes visuelles de la marque affichées côté client
        public MarkShape[] Shapes
        {
            get;
            private set;
        }
        // Couleur d'affichage de la marque (différente selon la classe/équipe)
        protected Color Color
        {
            get;
            set;
        }
        // Sort déclenché quand la marque est activée (différent du sort de création)
        protected SpellRecord TriggerSpell
        {
            get;
            private set;
        }
        public MarkTriggerTypeEnum TriggerType
        {
            get;
            private set;
        }
        public virtual bool Active
        {
            get;
            protected set;
        }
        public bool ContainsCell(short cellId)
        {
            return this.Shapes.Any((MarkShape entry) => entry.Point.CellId == cellId);
        }
        protected Mark(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect, MapPoint centerPoint, Zone zone,
            Color color, MarkTriggerTypeEnum triggerType)
        {
            this.Id = id;
            this.Source = source;
            this.SpellLevel = spellLevel;
            this.BaseEffect = effect;
            this.CenterPoint = centerPoint;
            this.Zone = zone;
            this.Color = color;
            this.TriggerSpell = SpellRecord.GetSpellRecord(effect.DiceMin);
            this.BuildShapes();
            this.TriggerType = triggerType;
            this.Active = true;
        }
        private void BuildShapes()
        {
            short[] cells = GetCells();

            Shapes = new MarkShape[cells.Length];

            for (int i = 0; i < cells.Length; i++)
            {
                Shapes[i] = new MarkShape(Source.Fight, new MapPoint(cells[i]), Color);
            }
        }
        public short[] GetCells()
        {
            return Zone.GetCells(CenterPoint.CellId, Source.Fight.Map);
        }
        public virtual GameActionMark GetGameActionMark()
        {
            return new GameActionMark(Source.Id, Source.Team.Id, SpellLevel.SpellId, SpellLevel.Grade, Id, (sbyte)Type,
              CenterPoint.CellId, (from entry in this.Shapes
                                   select entry.GetGameActionMarkedCell()).ToArray(), Active);
        }

        public virtual void OnFighterEnter(Fighter fighter)
        {

        }
        public virtual void OnFighterLeave(Fighter fighter)
        {

        }
        public abstract void Trigger(Fighter source, MarkTriggerTypeEnum type, object token = null);

        public virtual GameActionMark GetHiddenGameActionMark()
        {
            return null;
        }
        public virtual bool IsVisibleFor(Fighter fighter)
        {
            return true;
        }
    }

}
