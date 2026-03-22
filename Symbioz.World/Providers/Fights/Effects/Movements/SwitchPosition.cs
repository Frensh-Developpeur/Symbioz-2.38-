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

namespace Symbioz.World.Providers.Fights.Effects.Movements
{
    /// <summary>
    /// Échange de position : le lanceur et la première cible swappent leurs cellules.
    /// Retourne false si aucune cible n'est présente (sort annulé).
    /// Utilisé par Transposition (Sacrieur), Transfert (Xélor), etc.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_SwitchPosition)]
    public class SwitchPosition : SpellEffectHandler
    {
        public SwitchPosition(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            if (targets.Count() > 0)
            {
                targets.First().SwitchPosition(Source);
                return true;
            }
            else
                return false;
        }
    }
}
