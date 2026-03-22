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
    /// Glyphe : marque au sol qui se déclenche quand un combattant entre ou reste dessus.
    /// Un glyphe a une durée limitée (IDurationMark) et s'efface après N tours.
    /// Contrairement au piège (Trap), le glyphe ne disparaît pas au premier déclenchement.
    /// Il peut se déclencher à chaque tour (ON_TURN_STARTED) ou au passage (AFTER_MOVE).
    /// </summary>
    public class Glyph : Mark, IDurationMark
    {
        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.GLYPH;
            }
        }

        // Zone de déclenchement : P1 = cellule centrale uniquement (le joueur doit être dessus)
        public const string TriggerRawZone = "P1";

        // Durée restante du glyphe en tours ; décrémentée chaque fin de tour
        public short Duration
        {
            get;
            set;
        }
        // Un glyphe n'interrompt pas le déplacement (on peut traverser sa zone)
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

        public Glyph(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
         MapPoint centerPoint, Zone zone, Color color, MarkTriggerTypeEnum triggerType)
            : base(id, source, spellLevel, effect, centerPoint, zone, color, triggerType)
        {
            this.Duration = (short)effect.Duration;
        }

        // Décrémente la durée et retourne true si le glyphe doit être supprimé (durée = 0)
        public bool DecrementDuration()
        {
            return Duration-- <= 0;
        }

        // Déclenché quand un combattant entre dans la zone du glyphe ou à chaque tour
        // Le sort de déclenchement est appliqué centré sur la position du combattant (TriggerRawZone)
        public override void Trigger(Fighter source, MarkTriggerTypeEnum type, object token)
        {
            bool seq = Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);
            // DiceMax contient le grade du sort de déclenchement
            SpellLevelRecord triggerLevel = TriggerSpell.GetLevel((sbyte)BaseEffect.DiceMax);
            var effects = new List<EffectInstance>(triggerLevel.Effects);
            // Les effets sont inversés pour respecter l'ordre de priorité du protocole
            effects.Reverse();
            SpellEffectsManager.Instance.HandleEffects(Source, effects.ToArray(), triggerLevel, source.Point, TriggerRawZone, false);

            if (seq)
                Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }
        // Active le glyphe manuellement (ex: à la pose initiale) : applique les effets sur la zone centrale
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
