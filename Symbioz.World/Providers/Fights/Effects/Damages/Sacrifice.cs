using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Damages;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    /// <summary>
    /// Sacrifice (Sacrieur) : redirige tous les dégâts reçus par la cible vers le lanceur.
    /// Le TriggerBuff BEFORE_ATTACKED intercepte chaque coup et l'inflige au lanceur à la place.
    /// Sécurité : ne s'applique pas si le lanceur est aussi la cible (évite une boucle infinie),
    /// et ne s'applique pas si le lanceur a déjà un buff Sacrifice actif (même raison).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Sacrifice)]
    public class Sacrifice : SpellEffectHandler
    {
        public Sacrifice(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }

        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                // Évite que le lanceur se sacrifice lui-même (stack overflow) ou double-sacrifice
                if (target.Id != Source.Id && !Source.HasBuff(EffectsEnum.Effect_Sacrifice))
                {
                    base.AddTriggerBuff(target, FightDispellableEnum.DISPELLABLE, TriggerType.BEFORE_ATTACKED, BeforeAttacked);
                }
            }
            return true;
        }
        // Redirige les dégâts vers le lanceur — retourne true (buff consommé après chaque coup)
        private bool BeforeAttacked(TriggerBuff buff, TriggerType trigger, object token)
        {
            buff.Caster.InflictDamages((Damage)token);
            return true;
        }
    }
}
