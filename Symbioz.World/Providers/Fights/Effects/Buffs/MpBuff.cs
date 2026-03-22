using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Buffs
{
    /// <summary>
    /// Buff de Points de Mouvement. Même logique que ApBuff :
    /// - Duration > 0 : buff temporaire sur les PM (dissipable)
    /// - Duration == 0 : gain immédiat de PM pour ce tour (RegainMp)
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_AddMP_128), SpellEffectHandler(EffectsEnum.Effect_AddMP)]
    public class MPBuff : SpellEffectHandler
    {
        public MPBuff(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                if (this.Effect.Duration > 0)
                {
                    // Buff qui dure N tours
                    base.AddStatBuff(current, (short)Effect.DiceMin, current.Stats.MovementPoints, FightDispellableEnum.DISPELLABLE);
                }
                else
                {
                    // Gain immédiat de PM pour ce tour uniquement
                    current.RegainMp(Source.Id, (short)Effect.DiceMin);
                }
            }
            return true;
        }
    }
}
