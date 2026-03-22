using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.Core;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Fights.Damages;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    /// <summary>
    /// Dégâts neutres en % des HP ACTUELS du lanceur.
    /// Ex: DiceMin = 50, Source a 600 HP → inflige 300 dégâts neutres.
    /// Contrairement aux dégâts normaux, ces dégâts ne dépendent pas de la Force du lanceur.
    /// Utilisé par des sorts de type "sacrifice" ou "cri de guerre".
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_DamagePercentNeutral)]
    public class DamagePercent : SpellEffectHandler
    {
        public DamagePercent(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Calcule X% des HP actuels du LANCEUR (pas de la cible)
            int num = Source.Stats.CurrentLifePoints.GetPercentageOf(Effect.DiceMin);

            foreach (var target in targets)
            {
                target.InflictDamages(new Damage(Source, target, (short)num, EffectElementType.Neutral));
            }
            return true;
        }
    }
}
