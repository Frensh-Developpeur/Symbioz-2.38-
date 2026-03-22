using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Maps.Path;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Movements
{
    /// <summary>
    /// Attire la cible vers le lanceur (direction opposée à PushBack).
    /// DiceMin = nombre de cases d'attraction.
    /// CastPoint = point d'origine du sort (détermine la direction d'attraction).
    /// Si la cible heurte un obstacle, elle subit des dégâts de poussée.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_PullForward)]
    public class PullForward : SpellEffectHandler
    {
        public PullForward(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter target in targets)
            {
                target.Abilities.PullForward(Source, (short)Effect.DiceMin, CastPoint);
            }

            return true;
        }
    }
}