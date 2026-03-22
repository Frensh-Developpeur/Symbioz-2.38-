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
    /// Boost de sort : augmente temporairement les dégâts d'un sort spécifique.
    /// Utilisé par ex. pour les sorts de l'Osamodas qui boostent les invocations.
    /// Le SpellBoost est marqué REALLY_NOT_DISPELLABLE (ne peut pas être dissipé).
    /// Le sort ciblé et la valeur du boost sont stockés dans l'Effect (DiceMin/Value).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_SpellBoost)]
    public class BoostSpell : SpellEffectHandler
    {
        public BoostSpell(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                SpellBoost buff = new SpellBoost(target.BuffIdProvider.Pop(), target, Source, SpellLevel, Effect, SpellId, Critical, FightDispellableEnum.REALLY_NOT_DISPELLABLE);
                target.AddAndApplyBuff(buff);
            }
            return true;
        }
    }
}
