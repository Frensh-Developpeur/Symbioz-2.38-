using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Marks;
using Symbioz.World.Models.Fights.Spells;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Others
{
    /// <summary>
    /// Force le lanceur à lancer un autre sort sur chaque cible.
    /// Effect.DiceMin = ID du sort à lancer automatiquement.
    /// Effect.DiceMax = niveau du sort forcé.
    /// Utilisé pour chaîner des sorts (ex: sorts passifs qui déclenchent un autre sort).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_CastSpell)]
    public class CastSpell : SpellEffectHandler
    {
        public CastSpell(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            SpellRecord record = SpellRecord.GetSpellRecord(Effect.DiceMin);

            foreach (var target in targets)
            {
                // Lance le sort automatiquement sur la cellule de chaque cible
                Source.ForceSpellCast(record, (sbyte)Effect.DiceMax, target.CellId);
            }

            return true;
        }
    }
}
