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

namespace Symbioz.World.Providers.Fights.Effects.Debuffs
{
    /// <summary>
    /// Vol de PM : retire des PM à la cible ET en donne au lanceur. Même logique que ApSteal.
    /// La cible perd les PM (icône 169), le lanceur les gagne (icône 128) ou les regagne ce tour.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_StealMP_441)]
    public class MpSteal : SpellEffectHandler
    {
        public MpSteal(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
              Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                // La cible perd des PM (icône 169 = perte de PM)
                base.AddStatBuff(current, (short)-Effect.DiceMin, current.Stats.MovementPoints, FightDispellableEnum.DISPELLABLE, 169);

                if (this.Effect.Duration > 0)
                {
                    // Le lanceur gagne un buff temporaire de PM (icône 128 = gain de PM)
                    base.AddStatBuff(Source, (short)Effect.DiceMin, Source.Stats.MovementPoints, FightDispellableEnum.DISPELLABLE, 128);
                }
                else
                {
                    // Le lanceur regagne les PM immédiatement ce tour
                    Source.RegainMp(Source.Id, (short)Effect.DiceMin);
                }
            }
            return true;
        }
    }
}
