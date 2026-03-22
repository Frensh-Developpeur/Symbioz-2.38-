using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbioz.Core;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    /// <summary>
    /// Le lanceur se sacrifie d'un % de ses HP pour soigner ses cibles.
    /// DiceMin = pourcentage des HP actuels du lanceur sacrifiés ET transférés.
    /// Le lanceur perd ces HP (InflictDamages sur lui-même), les cibles les gagnent (Heal).
    /// Utilisé par des sorts de soin par sacrifice (Sacrieur, Eniripsa...).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_GiveHPPercent)]
    public class GiveHPPercent : SpellEffectHandler
    {
        public GiveHPPercent(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Calcule X% des HP actuels du lanceur
            short num = (short)Source.Stats.CurrentLifePoints.GetPercentageOf(Effect.DiceMin);

            // Le lanceur perd ces HP
            Source.InflictDamages(num, Source);

            // Les cibles gagnent ces HP
            foreach (var target in targets)
            {
                target.Heal(Source, num);
            }

            return true;
        }
    }
}
