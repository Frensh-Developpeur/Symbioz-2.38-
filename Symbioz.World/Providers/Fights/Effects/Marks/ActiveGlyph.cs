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
    /// Active manuellement les glyphes du lanceur qui contiennent sa cellule actuelle.
    /// Déclenche uniquement les glyphes de type Effect_Glyph (pas Effect_Glyph_402)
    /// qui appartiennent au lanceur ET sur lesquels il se trouve.
    /// Utilisé par des sorts qui permettent d'activer ses propres glyphes à volonté.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_ActiveGlyph)]
    public class ActiveGlyph : SpellEffectHandler
    {
        public ActiveGlyph(Fighter source, SpellLevelRecord level, EffectInstance effect,
        Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Filtre : glyphes standard du lanceur qui couvrent sa cellule actuelle
            foreach (var glyph in Fight.GetMarks<Glyph>(x => x.BaseEffect.EffectEnum == EffectsEnum.Effect_Glyph && x.Source == Source && x.ContainsCell(Source.CellId)))
            {
                glyph.Activate(Source);
            }
            return true;
        }
    }
}
