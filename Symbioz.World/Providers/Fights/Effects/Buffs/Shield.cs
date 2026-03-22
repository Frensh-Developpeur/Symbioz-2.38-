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

namespace Symbioz.World.Providers.Fights.Effects.Buffs
{
    /// <summary>
    /// Bouclier en pourcentage des HP max du LANCEUR.
    /// Ex: DiceMin = 20, Source a 1000 HP max → bouclier de 200 HP sur chaque cible.
    /// Le bouclier absorbe les dégâts reçus avant les HP réels.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Eff_AddShieldPercent)]
    public class ShieldPercent : SpellEffectHandler
    {
        public ShieldPercent(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // La taille du bouclier est calculée sur les HP max du LANCEUR (pas de la cible)
            double num = (double)Source.Stats.CurrentMaxLifePoints * ((double)Effect.DiceMin / 100.0);

            foreach (Fighter current in targets)
            {
                this.AddShieldBuff(current, FightDispellableEnum.DISPELLABLE, (short)num);
            }
            return true;
        }
    }

    /// <summary>
    /// Bouclier en valeur fixe (DiceMin HP absorbés).
    /// Pose un ShieldBuff qui intercepte les dégâts avant les HP réels.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Eff_AddShield)]
    public class Shield : SpellEffectHandler
    {
        public Shield(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                AddShieldBuff(current, FightDispellableEnum.DISPELLABLE, (short)Effect.DiceMin);
            }
            return true;
        }
    }
}
