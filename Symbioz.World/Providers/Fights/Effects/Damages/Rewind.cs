using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Damages;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using Symbioz.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Protocol.Enums;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    /// <summary>
    /// Effet de ricochet : chaque coup reçu par la cible se propage aux combattants adjacents.
    /// ImpactPercentage = 20% des dégâts reçus sont propagés.
    /// BEFORE_ATTACKED → augmente les dégâts reçus de 20% (le coup est amplifié).
    /// AFTER_ATTACKED  → inflige 20% des dégâts aux combattants sur les cellules adjacentes.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Rewind)]
    public class Rewind : SpellEffectHandler
    {
        // Pourcentage des dégâts propagés aux cibles adjacentes (fixé à 20%)
        public short ImpactPercentage
        {
            get;
            private set;
        }
        public Rewind(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
            this.ImpactPercentage = 20;
        }

        public override bool Apply(Fighter[] targets)
        {
            // Écoute BEFORE et AFTER pour modifier les dégâts puis les propager
            foreach (var target in targets)
            {
                base.AddTriggerBuff(target, FightDispellableEnum.REALLY_NOT_DISPELLABLE, TriggerType.BEFORE_ATTACKED | TriggerType.AFTER_ATTACKED, AfterAttacked);
            }
            return true;
        }
        private bool AfterAttacked(TriggerBuff buff, TriggerType trigger, object token)
        {
            Damage damages = (Damage)token;
            short impactDamages = (short)(((int)damages.Delta).GetPercentageOf(ImpactPercentage));

            if (trigger == TriggerType.AFTER_ATTACKED)
            {
                // Propage 20% des dégâts à tous les combattants adjacents à la cible
                MapPoint[] points = damages.Target.Point.GetNearPoints();
                List<Fighter> targets = new List<Fighter>();

                foreach (var point in points)
                {
                    Fighter target = buff.Caster.Fight.GetFighter(point);
                    if (target != null)
                        targets.Add(target);
                }

                foreach (var target in targets)
                {
                    target.InflictDamages(impactDamages, buff.Caster);
                }
            }
            else if (trigger == TriggerType.BEFORE_ATTACKED)
            {
                // Amplifie les dégâts reçus de 20% avant leur application
                damages.Delta += impactDamages;
            }
            return false;
        }
    }
}
