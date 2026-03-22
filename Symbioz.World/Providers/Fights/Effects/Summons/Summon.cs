using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Summons
{
    /// <summary>
    /// Invoque un monstre allié sur la cellule ciblée.
    /// Effect.DiceMin = ID du monstre à invoquer (MonsterRecord).
    /// Le grade de l'invocation correspond au niveau du sort, ou au dernier grade disponible
    /// si le monstre n'a pas de grade correspondant au niveau du sort.
    /// SummonFighter est statique pour être réutilisable depuis d'autres handlers (ex: ReplacePerInvocation).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Summon)]
    public class Summon : SpellEffectHandler
    {
        public Summon(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            var template = MonsterRecord.GetMonster(Effect.DiceMin);
            // Utilise le grade du sort ou le dernier grade disponible si le monstre n'en a pas autant
            SummonFighter(template, (sbyte)(template.GradeExist(SpellLevel.Grade) ? SpellLevel.Grade : template.LastGrade().Id),
                Source, CastPoint);
            return true;
        }

        // Méthode statique réutilisable pour invoquer un monstre depuis d'autres handlers
        public static SummonedFighter SummonFighter(MonsterRecord template, sbyte gradeId, Fighter source, MapPoint castPoint)
        {
            SummonedFighter fighter = new SummonedFighter(template, gradeId, source, source.Team, castPoint.CellId);
            source.Fight.AddSummon(fighter);
            return fighter;
        }
    }
}
