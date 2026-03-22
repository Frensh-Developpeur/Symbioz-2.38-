using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Marks;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Marks
{
    /// <summary>
    /// Active toutes les runes du lanceur en même temps.
    /// Déclenche l'effet de chaque rune posée par ce combattant sur le terrain.
    /// Utilisé par le sort "Explosion" du Roublard pour faire sauter toutes ses runes.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_ActiveRunes)]
    public class ActiveRunes : SpellEffectHandler
    {
        public ActiveRunes(Fighter source, SpellLevelRecord level, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Active toutes les runes qui appartiennent au lanceur
            foreach (var rune in Fight.GetMarks<Models.Fights.Marks.Rune>(x => x.Source == Source))
            {
                rune.Activate(Source);
            }
            return true;
        }
    }
}
