using Symbioz.Protocol.Enums;
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
    /// Glyphe d'aura : variante du Glyph déclenchée centrée sur le poseur plutôt que sur la cible.
    /// Fonctionne comme un Glyph classique (durée limitée, IDurationMark, type GLYPH),
    /// mais le sort de déclenchement est appliqué autour du poseur de la marque.
    /// Utilisé notamment pour les sorts d'aura du Sacrieur ou de l'Eniripsa.
    /// </summary>
    public class AuraGlyph : Mark, IDurationMark
    {
        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.GLYPH;
            }
        }

        // Zone de déclenchement : P1 = cellule centrale uniquement
        public const string TriggerRawZone = "P1";

        // Durée restante de l'aura en tours
        public short Duration
        {
            get;
            set;
        }
        // L'aura n'interrompt pas le déplacement
        public override bool BreakMove
        {
            get
            {
                return false;
            }
        }
        public override GameActionMark GetGameActionMark()
        {
            return new GameActionMark(Source.Id, Source.Team.Id, SpellLevel.SpellId, SpellLevel.Grade, Id, (sbyte)Type,
               CenterPoint.CellId, (from entry in base.Shapes
                                    select entry.GetGameActionMarkedCell()).ToArray(), true);
        }

        public AuraGlyph(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
         MapPoint centerPoint, Zone zone, Color color, MarkTriggerTypeEnum triggerType)
            : base(id, source, spellLevel, effect, centerPoint, zone, color, triggerType)
        {
            this.Duration = (short)effect.Duration;
        }

        // Décrémente la durée d'un tour ; retourne true quand la durée atteint 0 (suppression)
        public bool DecrementDuration()
        {
            return Duration-- <= 0;
        }

        // Déclenché quand un combattant entre dans la zone de l'aura ou lors de son tour
        // Applique les effets centrés sur la position du combattant déclencheur
        public override void Trigger(Fighter source, MarkTriggerTypeEnum type, object token)
        {
            bool seq = Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);

            SpellLevelRecord triggerLevel = TriggerSpell.GetLevel((sbyte)BaseEffect.DiceMax);
            var effects = new List<EffectInstance>(triggerLevel.Effects);
            // Inversion des effets pour respecter l'ordre de priorité du protocole
            effects.Reverse();
            SpellEffectsManager.Instance.HandleEffects(Source, effects.ToArray(), triggerLevel, source.Point, TriggerRawZone, false);

            if (seq)
                Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }
        // Active l'aura manuellement (ex: pose initiale) sur la zone centrale du poseur
        public void Activate(Fighter source)
        {
            bool seq = Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);
            SpellLevelRecord triggerLevel = TriggerSpell.GetLevel(SpellLevel.Grade);
            SpellEffectsManager.Instance.HandleEffects(Source, triggerLevel, CenterPoint, false);
            if (seq)
                Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }
    }
}
