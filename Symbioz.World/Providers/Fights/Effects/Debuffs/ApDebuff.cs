using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Providers.Fights.Effects;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Debuffs
{
    /// <summary>
    /// Retrait de Points d'Action.
    /// - Duration > 0 : buff négatif temporaire sur les PA (id 168 = icône de perte de PA côté client)
    /// - Duration == 0 : perte immédiate et définitive de PA pour ce tour (LostAp)
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_SubAP)]
    public class ApDebuff : SpellEffectHandler
    {
        public ApDebuff(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
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
                    // Retrait temporaire : valeur négative, affiché avec l'icône 168 (perte de PA)
                    base.AddStatBuff(current, (short)-Effect.DiceMin, current.Stats.ActionPoints, FightDispellableEnum.DISPELLABLE, 168);
                }
                else
                {
                    // Retrait immédiat et permanent pour ce tour
                    current.LostAp(Source.Id, (short)Effect.DiceMin);
                }
            }
            return true;
        }
    }
}
