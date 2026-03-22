using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;

namespace Symbioz.World.Providers.Fights.Effects.Debuffs
{
    /// <summary>
    /// Dissipation magique : supprime tous les buffs dissipables de chaque cible.
    /// GetDispelableBuffs(true) = uniquement les buffs marqués DISPELLABLE (pas REALLY_NOT_DISPELLABLE).
    /// Utilisé par les sorts comme Mot Fatidique (Eniripsa) ou Fourberie (Roublard).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_DispelMagicEffects)]
    public class DispelMagicEffects : SpellEffectHandler
    {
        public DispelMagicEffects(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect, Fighter[] targets, MapPoint castPoint, bool critical) : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }

        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                // Supprime uniquement les buffs dissipables (pas les passifs permanents)
                foreach (var buff in target.GetDispelableBuffs(true))
                {
                    target.RemoveAndDispellBuff(buff);
                }
            }
            return true;
        }
    }
}
