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
using Symbioz.Core;
using Symbioz.World.Models.Fights.Damages;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    [SpellEffectHandler(EffectsEnum.Effect_DamageHPLostPercentAir)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageHPLostPercentFire)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageHPLostPercentNeutral)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageHPLostPercentStrenght)]
    [SpellEffectHandler(EffectsEnum.Effect_DamageHPLostPercentWater)]
    /// <summary>
    /// Dégâts proportionnels aux HP PERDUS de la cible (pas ses HP actuels).
    /// DiceMin = pourcentage des HP perdus utilisé comme base de dégâts.
    /// Ex: cible a perdu 600 HP, DiceMin = 50 → 300 dégâts de base.
    /// Plafonné à 500 pour éviter les dégâts excessifs.
    /// Utilisé par des sorts dont la puissance augmente quand la cible est blessée.
    /// </summary>
    public class DamageHPLostPercent : SpellEffectHandler
    {
        public EffectElementType ElementType { get; set; }

        public DamageHPLostPercent(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
            switch (effect.EffectEnum)
            {
                case EffectsEnum.Effect_DamageHPLostPercentStrenght:
                    ElementType = EffectElementType.Earth;
                    break;
                case EffectsEnum.Effect_DamageHPLostPercentWater:
                    ElementType = EffectElementType.Water;
                    break;
                case EffectsEnum.Effect_DamageHPLostPercentFire:
                    ElementType = EffectElementType.Fire;
                    break;
                case EffectsEnum.Effect_DamageHPLostPercentAir:
                    ElementType = EffectElementType.Air;
                    break;
                case EffectsEnum.Effect_DamageHPLostPercentNeutral:
                    ElementType = EffectElementType.Neutral;
                    break;
            }
        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                // Calcule X% des HP perdus de la cible comme base de dégâts
                short num = (short)target.Stats.LifeLost.GetPercentageOf(Effect.DiceMin);
                // Plafond à 500 pour éviter des dégâts trop élevés
                if (num > 500)
                    num = 500;
                target.InflictDamages(new Damage(Source, target, num, ElementType));
            }
            return true;
        }
    }
}
