using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
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
    /// Handler qui crée un piège (Trap) sur le terrain de combat.
    /// Instancie un objet Trap avec la zone définie par ShapeType/Radius de l'effet,
    /// la couleur encodée dans Effect.Value, et l'enregistre dans le combat via AddMark.
    /// Le piège sera invisible pour les ennemis jusqu'à ce qu'il soit déclenché.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Trap)]
    public class TrapSpawn : SpellEffectHandler
    {
        public TrapSpawn(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        // Crée et ajoute le piège sur la cellule ciblée avec la zone et couleur de l'effet
        public override bool Apply(Fighter[] targets)
        {
            Zone zone = new Zone(Effect.ShapeType, Effect.Radius);
            // La couleur du piège est encodée dans le champ Value de l'effet (ARGB)
            Color color = Color.FromArgb(Effect.Value);
            Trap trap = new Trap(Fight.PopNextMarkId(), Source, SpellLevel, Effect, CastPoint, zone, color);
            Fight.AddMark(trap);
            return true;
        }
    }
}
