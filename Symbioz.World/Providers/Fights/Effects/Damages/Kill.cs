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

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    /// <summary>
    /// Tue instantanément la cible en forçant ses HP à 0.
    /// Ne passe pas par InflictDamages (pas de résistances, pas de bouclier).
    /// Fight.CheckDeads() est appelé ensuite pour déclencher la mort.
    /// Utilisé par des sorts comme Mot de Sacrifice ou des mécaniques spéciales.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Kill)]
    public class Kill : SpellEffectHandler
    {
        public Kill(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
            Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                // Bypass total des résistances et boucliers — mort immédiate
                target.Stats.CurrentLifePoints = 0;
            }
            return true;
        }
    }
}
