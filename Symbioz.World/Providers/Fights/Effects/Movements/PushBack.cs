using Symbioz.Core;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
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
    /// Repousse la cible en ligne droite, dans la direction opposée au lanceur.
    /// DiceMin = nombre de cases de poussée.
    /// CastPoint = point d'origine du sort (détermine la direction de poussée).
    /// Si la cible heurte un obstacle, elle subit des dégâts de poussée.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_PushBack)]
    [SpellEffectHandler(EffectsEnum.Effect_PushBack_1103)]
    public class PushBack : SpellEffectHandler
    {
        public PushBack(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }

        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                target.Abilities.PushBack(Source, (short)Effect.DiceMin, CastPoint);
            }
            return true;
        }
    }
}
