using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Marks;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Marks
{
    /// <summary>
    /// Glyphe d'aura : effet de zone autour du lanceur (non encore implémenté).
    /// L'aura suit le lanceur contrairement au glyphe classique qui est posé sur une cellule fixe.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_GlyphAuraSpawn)]
    public class GlyphAuraSpawn : SpellEffectHandler
    {
        public GlyphAuraSpawn(Fighter source, SpellLevelRecord level, EffectInstance effect,
       Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Todo : implémenter l'aura mobile
            return true;
        }
    }
}
