using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    /// <summary>
    /// Dégâts proportionnels au nombre d'invocations du lanceur (non encore implémenté).
    /// L'idée : plus le lanceur a d'invocations en jeu, plus les dégâts sont importants.
    /// Utilisé par des sorts de l'Osamodas par exemple.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_DamageFromSummonsCount)]
    public class DamageFromSummonCount : SpellEffectHandler
    {
        public DamageFromSummonCount(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect, Fighter[] targets, MapPoint castPoint, bool critical) : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }

        public override bool Apply(Fighter[] targets)
        {
            // Todo : implémenter les dégâts selon le nombre d'invocations actives
            var e = Effect;
            return true;
        }
    }
}
