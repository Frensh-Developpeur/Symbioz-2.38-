using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Providers.Fights.Effects;
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
    /// Portail : marque au sol qui téléporte les combattants qui la traversent.
    /// Un portail ne fonctionne que si au moins 2 portails actifs existent sur le terrain.
    /// Quand un combattant entre dans un portail, il est téléporté vers le portail "jumeau"
    /// (via PortalProvider.GetPortalsTuple) si la cellule de destination est libre.
    /// BreakMove = true : le déplacement s'arrête à l'entrée du portail.
    /// </summary>
    public class Portal : Mark
    {
        public Portal(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
         MapPoint centerPoint, Zone zone, Color color)
            : base(id, source, spellLevel, effect, centerPoint, zone, color,
            MarkTriggerTypeEnum.AFTER_MOVE)
        {
        }
        // Le portail interrompt le déplacement pour téléporter le combattant
        public override bool BreakMove
        {
            get
            {
                return true;
            }
        }

        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.PORTAL;
            }
        }

        // Désactive le portail (ex: quand un 3ème portail est posé, le plus ancien est désactivé)
        public void Unactive(Fighter source)
        {
            Active = false;
        }

        // Déclenché quand un combattant entre dans le portail :
        // - Vérifie que le portail est actif et qu'il existe bien 2 portails (paire complète)
        // - Trouve le portail jumeau via PortalProvider
        // - Téléporte le combattant si la cellule de destination est libre
        public override void Trigger(Fighter source, MarkTriggerTypeEnum type, object token = null)
        {
            if (Active)
            {
                // Il faut au moins 2 portails actifs pour que la téléportation fonctionne
                if (this.Source.Team.GetActivePortalCount() >= 2)
                {
                    Tuple<Portal, Portal> pair = PortalProvider.Instance.GetPortalsTuple(Fight, source.CellId);

                    // Téléporte uniquement si la cellule de destination est libre
                    if (Fight.IsCellFree(pair.Item2.CenterPoint.CellId))
                        source.Teleport(Source, pair.Item2.CenterPoint);
                }
            }
        }
    }
}
