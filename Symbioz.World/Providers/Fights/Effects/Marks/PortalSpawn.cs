using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Fights.Marks;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Marks
{
    /// <summary>
    /// Crée un portail (Xélor) sur la cellule ciblée.
    /// Limite : max 4 portails par équipe (CanSpawnPortal). Si la limite est atteinte,
    /// le plus ancien portail est supprimé avant d'en créer un nouveau.
    /// La couleur du portail dépend de l'équipe (bleu = défenseur, rouge = attaquant).
    /// Quand 2 portails sont actifs, tout combattant qui entre dans l'un ressort par l'autre.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_SpawnPortal)]
    public class PortalSpawn : SpellEffectHandler
    {
        public PortalSpawn(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
           Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Si l'équipe a déjà 4 portails, supprime le plus ancien avant d'en créer un
            if (!Source.Team.CanSpawnPortal())
            {
                Source.Team.RemoveFirstPortal(Source);
            }
            Zone zone = new Zone(Effect.ShapeType, Effect.Radius);
            Color color = GetColorByTeam(Source.Team);
            Portal portal = new Portal(Fight.PopNextMarkId(), Source, SpellLevel, Effect, CastPoint, zone, color);
            Fight.AddMark(portal);

            //if (Source.Team.GetAllPortals().Length <= 2)
            //{
            //    ContextHandler.SendGameActionFightActivateGlyphTrapMessage(Fight.Clients, 1, 1181, Caster, Caster.Fight.GetAllPortalsByTeam(Caster.Team).Count > 1);
            //}
            return true;
        }

        // Couleur visuelle du portail selon l'équipe (côté client)
        private static Color GetColorByTeam(FightTeam team)
        {
            return team.TeamEnum == TeamEnum.TEAM_CHALLENGER ? Color.Blue : Color.Red;
        }
    }
}
