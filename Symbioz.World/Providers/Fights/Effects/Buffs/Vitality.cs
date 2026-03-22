using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Buffs
{
    /// <summary>
    /// Buff de vitalité en % des HP max de la CIBLE.
    /// Augmente temporairement les HP max et les HP actuels de la cible.
    /// Quand le buff expire, les HP max redescendent (les HP actuels aussi si au-dessus du nouveau max).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_AddVitalityPercent)]
    public class VitalityPercent : SpellEffectHandler
    {
        public VitalityPercent(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                // Calcule X% des HP max de la cible
                double num = (double)current.Stats.MaxLifePoints * ((double)Effect.DiceMin / 100.0);
                this.AddVitalityBuff(current, FightDispellableEnum.DISPELLABLE, (short)num);
            }
            return true;
        }
    }

    /// <summary>
    /// Buff de vitalité en valeur fixe (DiceMin HP max temporaires).
    /// Même comportement que VitalityPercent mais avec une valeur fixe.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_AddVitality)]
    public class Vitality : SpellEffectHandler
    {
        public Vitality(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                this.AddVitalityBuff(current, FightDispellableEnum.DISPELLABLE, (short)Effect.DiceMin);
            }
            return true;
        }
    }
}
