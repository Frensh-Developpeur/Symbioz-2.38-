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

namespace Symbioz.World.Providers.Fights.Effects.Others
{
    /// <summary>
    /// Dissipe tous les buffs provenant d'un sort spécifique sur chaque cible.
    /// Effect.Value = l'ID du sort dont les buffs seront supprimés.
    /// Contrairement à DispelMagicEffects (qui dissipe tout), celui-ci cible un sort précis.
    /// Utilisé par ex. pour retirer l'effet d'un sort d'invocation ou d'un buff unique.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_DispellSpell)]
    public class DispellSpell : SpellEffectHandler
    {
        public DispellSpell(Fighter source, SpellLevelRecord level, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }

        public override bool Apply(Fighter[] targets)
        {
            ushort spellId = (ushort)Effect.Value;

            foreach (var target in targets)
            {
                target.DispellSpellBuffs(Source, spellId);
            }
            return true;
        }
    }
}
