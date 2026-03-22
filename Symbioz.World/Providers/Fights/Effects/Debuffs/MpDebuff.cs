using Symbioz.Protocol.Enums;
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

namespace Symbioz.World.Providers.Fights.Effects.Debuffs
{
    /// <summary>
    /// Retrait de Points de Mouvement. Même logique que ApDebuff :
    /// - Duration > 0 : buff négatif temporaire sur les PM (id 169 = icône perte de PM)
    /// - Duration == 0 : perte immédiate et définitive de PM pour ce tour (LostMp)
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_SubMP_1080)]
    [SpellEffectHandler(EffectsEnum.Effect_SubMP)]
    public class MpDebuff : SpellEffectHandler
    {
        public MpDebuff(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
            Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (Fighter current in targets)
            {
                if (this.Effect.Duration > 0)
                {
                    // Retrait temporaire, affiché avec l'icône 169 (perte de PM)
                    base.AddStatBuff(current, (short)-Effect.DiceMin, current.Stats.MovementPoints, FightDispellableEnum.DISPELLABLE, 169);
                }
                else
                {
                    // Retrait immédiat et permanent pour ce tour
                    current.LostMp(Source.Id, (short)Effect.DiceMin);
                }
            }
            return true;
        }
    }
}
