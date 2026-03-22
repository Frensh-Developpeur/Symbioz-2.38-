using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Monsters
{
    /// <summary>
    /// Action de fin de combat : téléporte les combattants vers une nouvelle map à la fin du fight.
    /// Utilisé principalement dans les donjons pour enchaîner les salles automatiquement.
    /// Lorsqu'un combat se termine sur MapId, tous les survivants sont envoyés sur TeleportMapId/TeleportCellId.
    /// </summary>
    [Table("EndFightActions")]
    public class EndFightActionRecord : ITable
    {
        // Liste de toutes les actions de fin de combat chargées en mémoire
        public static List<EndFightActionRecord> EndFightActions = new List<EndFightActionRecord>();

        [Primary]
        public int Id;              // Identifiant unique de l'action

        public int MapId;           // Map sur laquelle cette action est déclenchée (après un combat)

        public int TeleportMapId;   // Map de destination vers laquelle les joueurs sont téléportés

        public ushort TeleportCellId; // Cellule d'arrivée sur la map de destination

        public EndFightActionRecord(int id, int mapId, int teleportMapId, ushort teleportCellId)
        {
            this.Id = id;
            this.MapId = mapId;
            this.TeleportMapId = teleportMapId;
            this.TeleportCellId = teleportCellId;
        }

        public static EndFightActionRecord GetEndFightAction(int mapId)
        {
            return EndFightActions.Find(x => x.MapId == mapId);
        }
    }
}
