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
    /// Handler pour les dégâts directs élémentaires (feu, eau, terre, air, neutre).
    /// Gère à la fois les dégâts immédiats (Duration == 0) et les dégâts sur la durée
    /// (Duration > 0 → pose un TriggerBuff qui déclenche les dégâts à chaque début de tour).
    /// Le jet de dé est calculé via FormulasProvider.EvaluateJet selon l'élément du sort.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_DamageFire)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageEarth)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageAir)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageWater)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageNeutral)]
    public class DirectDamage : SpellEffectHandler
    {
        // Élément des dégâts (déterminé en fonction de l'EffectsEnum dans le constructeur)
        public EffectElementType ElementType
        {
            get;
            set;
        }

        public DirectDamage(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
            Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
            // Mappe l'ID d'effet vers le type d'élément utilisé dans les calculs de dégâts
            switch (effect.EffectEnum)
            {
                case EffectsEnum.Effect_DamageEarth:
                    ElementType = EffectElementType.Earth;
                    break;
                case EffectsEnum.Effect_DamageWater:
                    ElementType = EffectElementType.Water;
                    break;
                case EffectsEnum.Effect_DamageFire:
                    ElementType = EffectElementType.Fire;
                    break;
                case EffectsEnum.Effect_DamageAir:
                    ElementType = EffectElementType.Air;
                    break;
                case EffectsEnum.Effect_DamageNeutral:
                    ElementType = EffectElementType.Neutral;
                    break;
            }
        }
        // Applique les dégâts :
        // - Si Duration > 0 : pose un buff sur chaque cible qui inflige les dégâts à chaque début de tour
        // - Sinon : inflige les dégâts immédiatement sur toutes les cibles
        public override bool Apply(Fighter[] targets)
        {
            if (Effect.Duration > 0)
            {
                // Dégâts périodiques : crée un TriggerBuff déclenché au début de chaque tour
                foreach (var target in targets)
                {
                    base.AddTriggerBuff(target, FightDispellableEnum.DISPELLABLE, TriggerType.TURN_BEGIN, DamageTrigger);
                }
            }
            else
            {
                // Dégâts immédiats : calcule le jet et inflige à chaque cible
                Jet jet = FormulasProvider.Instance.EvaluateJet(Source, ElementType, Effect, this.SpellId);

                foreach (var target in targets)
                {
                    // Clone le jet pour chaque cible (chaque cible peut avoir une résistance différente)
                    target.InflictDamages(new Damage(Source, target, jet.Clone(), ElementType, Effect, Critical));
                }
            }
            return true;
        }

        // Callback du TriggerBuff : recalcule et inflige les dégâts à chaque début de tour
        // Retourne false = le buff n'est pas supprimé après déclenchement (dure sa durée normale)
        private bool DamageTrigger(TriggerBuff buff, TriggerType trigger, object token)
        {
            Jet jet = FormulasProvider.Instance.EvaluateJet(Source, ElementType, Effect, this.SpellId);
            buff.Target.InflictDamages(new Damage(Source, buff.Target, jet, ElementType, Effect, Critical));
            return false;
        }

    }
}
