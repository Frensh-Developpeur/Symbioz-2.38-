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

namespace Symbioz.World.Providers.Fights.Effects.Summons
{
    /// <summary>
    /// Ressuscite le dernier allié mort de l'équipe du lanceur (Laisse Spirituelle de l'Osamodas).
    /// DiceMin = pourcentage des HP max avec lesquels le combattant revit.
    /// Mécanique liée : si le lanceur meurt après avoir ressuscité quelqu'un,
    /// le combattant ressuscité meurt également (Source_OnDeadEvt).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Revive)]
    public class Revive : SpellEffectHandler
    {
        // Référence au combattant ressuscité, pour le tuer si le lanceur meurt
        private Fighter RevivedFighter
        {
            get;
            set;
        }
        public Revive(Fighter source, SpellLevelRecord level, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {
        }
        public override bool Apply(Fighter[] targets)
        {
            // Cible le dernier combattant mort de l'équipe (pas forcément la première cible du sort)
            Fighter target = Source.Team.LastDead();

            if (target != null)
            {
                this.RevivedFighter = target;
                this.Fight.ReviveFighter(Source, target, CastPoint.CellId, (short)Effect.DiceMin);
                // Abonne l'événement : si le lanceur meurt, le ressuscité meurt aussi
                this.Source.BeforeDeadEvt += Source_OnDeadEvt;
            }

            return true;
        }

        // Déclenché quand le lanceur meurt → tue le combattant qu'il avait ressuscité
        void Source_OnDeadEvt(Fighter obj)
        {
            RevivedFighter.Die(Source);
        }
    }
}
