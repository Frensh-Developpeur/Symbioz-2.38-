using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Marks
{
    /// <summary>
    /// Rune : marque passive posée sur le terrain, sans déclenchement automatique.
    /// La rune ne réagit ni aux mouvements ni aux tours (TriggerType = NONE).
    /// Elle est activée manuellement via Activate(), ce qui force un lancer de sort
    /// centré sur la position de la rune, puis supprime la rune du combat.
    /// Utilisée notamment par le Xélor pour marquer des cellules.
    /// </summary>
    public class Rune : Mark
    {
        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.RUNE;
            }
        }
        // La rune ne bloque pas le déplacement (on peut marcher dessus librement)
        public override bool BreakMove
        {
            get
            {
                return false;
            }
        }
        public Rune(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
          MapPoint centerPoint, Zone zone, Color color)
            // NONE = la rune ne se déclenche jamais automatiquement
            : base(id, source, spellLevel, effect, centerPoint, zone, color, MarkTriggerTypeEnum.NONE)
        {

        }

        // La rune n'a pas de déclenchement automatique ; cette méthode ne fait rien
        public override void Trigger(Fighter source, MarkTriggerTypeEnum type, object token)
        {
        }
        // Active manuellement la rune : force le lanceur à rejouer le sort sur la position de la rune
        // puis supprime la rune du terrain
        internal void Activate(Fighter source)
        {
            this.Source.ForceSpellCast(TriggerSpell, SpellLevel.Grade, this.CenterPoint.CellId);
            this.Fight.RemoveMark(source, this);
        }
    }
}
