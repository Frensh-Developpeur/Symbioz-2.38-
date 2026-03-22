using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using Symbioz.World.Models.Fights.Damages;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    [SpellEffectHandler(EffectsEnum.Effect_DamageMpPercentageAir)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageMpPercentageNeutral)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageMpPercentageEarth)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageMpPercentageWater)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageMpPercentageFire)]
    /// <summary>
    /// Dégâts modulés par le pourcentage de PM restants du lanceur.
    /// Plus le lanceur a de PM restants (en %), plus les dégâts sont élevés.
    /// Note : le jet est toujours calculé en Feu (EffectElementType.Fire) même si l'élément diffère —
    /// seul le delta final est ajusté via MpPercentage. Comportement à vérifier selon le sort.
    /// </summary>
    public class DamageFromMpPercentage : SpellEffectHandler
    {
        private EffectElementType ElementType { get; set; }

        public DamageFromMpPercentage(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect, Fighter[] targets,
            MapPoint castPoint, bool critical) : base(source, spellLevel, effect, targets, castPoint, critical)
        {
            switch (effect.EffectEnum)
            {
                case EffectsEnum.Effect_DamageMpPercentageFire:
                    ElementType = EffectElementType.Fire;
                    break;
                case EffectsEnum.Effect_DamageMpPercentageWater:
                    ElementType = EffectElementType.Water;
                    break;
                case EffectsEnum.Effect_DamageMpPercentageEarth:
                    ElementType = EffectElementType.Earth;
                    break;
                case EffectsEnum.Effect_DamageMpPercentageAir:
                    ElementType = EffectElementType.Air;
                    break;
                case EffectsEnum.Effect_DamageMpPercentageNeutral:
                    ElementType = EffectElementType.Neutral;
                    break;
            }
        }

        public override bool Apply(Fighter[] targets)
        {
            // Calcule le jet de base (en Feu) puis le réduit selon le % de PM restants
            Jet jet = FormulasProvider.Instance.EvaluateJet(Source, EffectElementType.Fire, Effect, this.SpellId);
            jet.Delta = (short)(jet.Delta * ((double)Source.Stats.MpPercentage / 100d));
            foreach (var target in targets)
            {
                target.InflictDamages(new Damage(Source, target, jet.Clone(), EffectElementType.Fire, Effect, Critical));
            }
            return true;
        }
    }
}
