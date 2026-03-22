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
    /// Vol de PA : retire des PA à la cible ET en donne au lanceur.
    /// La cible perd toujours les PA (buff négatif permanent ou temporaire).
    /// Le lanceur :
    ///   - Duration > 0 : gagne un buff temporaire de PA (id 111 = icône gain de PA)
    ///   - Duration == 0 : regagne immédiatement les PA ce tour
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_StealAP_440)]
    public class ApSteal : SpellEffectHandler
    {
        public ApSteal(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
              Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                // La cible perd des PA (icône 168 = perte de PA)
                base.AddStatBuff(current, (short)-Effect.DiceMin, current.Stats.ActionPoints, FightDispellableEnum.DISPELLABLE, 168);

                if (this.Effect.Duration > 0)
                {
                    // Le lanceur gagne un buff temporaire de PA (icône 111 = gain de PA)
                    base.AddStatBuff(Source, (short)Effect.DiceMin, Source.Stats.ActionPoints, FightDispellableEnum.DISPELLABLE, 111);
                }
                else
                {
                    // Le lanceur regagne les PA immédiatement ce tour
                    Source.RegainAp(Source.Id, (short)Effect.DiceMin);
                }
            }
            return true;
        }
    }
}