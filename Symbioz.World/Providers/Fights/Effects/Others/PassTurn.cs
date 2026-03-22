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
    /// Termine immédiatement le tour de la cible si c'est son tour de jouer.
    /// Vérifie IsFighterTurn avant d'agir — ne fait rien si ce n'est pas son tour.
    /// Utilisé par des sorts qui forcent la fin du tour (ex: certaines invocations).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_PassTurn)]
    public class PassTurn : SpellEffectHandler
    {
        public PassTurn(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
             Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            var target = targets.FirstOrDefault();

            // N'agit que si c'est réellement le tour de la cible
            if (target != null && target.IsFighterTurn)
            {
                target.PassTurn();
            }
            return true;
        }
    }
}
