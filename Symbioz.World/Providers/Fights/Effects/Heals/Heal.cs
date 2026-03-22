using Symbioz.Core;
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

namespace Symbioz.World.Providers.Fights.Effects.Heals
{
    /// <summary>
    /// Soin direct : restaure des HP à chaque cible.
    /// Le jet est tiré aléatoirement entre DiceMin et DiceMax,
    /// puis amplifié par GetHealDelta (bonus de soin du lanceur inclus).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_HealHP_108)]
    public class Heal : SpellEffectHandler
    {
        public Heal(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
            Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }

        public override bool Apply(Fighter[] targets)
        {
            // Tire un jet entre DiceMin et DiceMax (ou DiceMin fixe si DiceMax == 0)
            short jet = Effect.DiceMax > 0 ? (short)new AsyncRandom().Next(Effect.DiceMin, Effect.DiceMax + 1) : (short)Effect.DiceMin;
            // Applique le bonus de soin du lanceur (Intelligence, HealBonus, etc.)
            short num = FormulasProvider.Instance.GetHealDelta(Source, jet);

            foreach (var target in targets)
            {
                target.Heal(Source, num);
            }

            return true;
        }
    }
}
