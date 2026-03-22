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

namespace Symbioz.World.Providers.Fights.Effects.Summons
{
    /// <summary>
    /// Invoque un Double du lanceur : une copie du personnage avec ses stats.
    /// Seul un CharacterFighter peut invoquer un double (pas une invocation elle-même).
    /// Le DoubleFighter hérite des stats du joueur source et agit dans la même équipe.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Double)]
    public class Double : SpellEffectHandler
    {
        public Double(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            if (Source is CharacterFighter)
            {
                DoubleFighter fighter = new DoubleFighter((CharacterFighter)Source, Source.Team, CastPoint.CellId);
                Fight.AddSummon(fighter, (CharacterFighter)Source);
                return true;
            }
            else
            {
                // Cas anormal : une invocation tente de se dupliquer
                Fight.Reply("An non character fighter try to summon a double...");
                return false;
            }
        }
    }
}
