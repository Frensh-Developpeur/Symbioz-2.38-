using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Others
{
    /// <summary>
    /// Fait passer le tour de la cible au prochain déclenchement (TURN_BEGIN).
    /// Pose un TriggerBuff REALLY_NOT_DISPELLABLE qui retourne true lors du début de tour :
    /// retourner true dans un TriggerBuff signifie que le buff est consommé/supprimé,
    /// mais ici c'est le mécanisme qui indique au combat de sauter ce tour.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_SkipTurn)]
    public class SkipTurn : SpellEffectHandler
    {
        public SkipTurn(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
             Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                base.AddTriggerBuff(target, FightDispellableEnum.REALLY_NOT_DISPELLABLE, TriggerType.TURN_BEGIN, TurnBegin);
            }
            return true;
        }
        // Retourne true = le buff se supprime après déclenchement (un seul saut de tour)
        private bool TurnBegin(TriggerBuff buff, TriggerType trigger, object token)
        {
            return true;
        }
    }
}
