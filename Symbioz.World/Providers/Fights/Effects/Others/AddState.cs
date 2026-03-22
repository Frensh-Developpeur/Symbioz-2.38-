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
    /// Applique un état (StateBuff) sur chaque cible.
    /// Les états sont des conditions spéciales (ex: "empoisonné", "étourdi", "porteur de bombe").
    /// Effect.Value = l'ID de l'état dans la base de données (SpellStateRecord).
    /// Retourne false si l'état n'existe pas en BDD (sort mal configuré).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_AddState)]
    public class AddState : SpellEffectHandler
    {
        public AddState(Fighter source, SpellLevelRecord level, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            SpellStateRecord stateRecord = SpellStateRecord.GetState(Effect.Value);

            if (stateRecord != null)
            {
                foreach (var target in targets)
                {
                    // DISPELLABLE_BY_STRONG_DISPEL = dissipable uniquement par une dissipation forte
                    base.AddStateBuff(target, stateRecord, Protocol.Enums.FightDispellableEnum.DISPELLABLE_BY_STRONG_DISPEL);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
