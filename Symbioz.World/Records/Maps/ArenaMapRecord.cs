using Symbioz.Core;
using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Maps
{
    /// <summary>
    /// Map utilisée pour les combats d'arène (mode PvP organisé).
    /// La table ArenaMaps contient la liste des maps pouvant accueillir des matchs d'arène.
    /// GetArenaMap() sélectionne aléatoirement l'une d'elles pour chaque nouveau match.
    /// </summary>
    [Table("ArenaMaps")]
    public class ArenaMapRecord : ITable
    {
        // Liste de toutes les maps d'arène chargées en mémoire
        public static List<ArenaMapRecord> ArenaMaps = new List<ArenaMapRecord>();

        public int MapId;   // ID de la map pouvant accueillir un combat d'arène

        public ArenaMapRecord(int mapId)
        {
            this.MapId = mapId;
        }

        // Retourne une map d'arène choisie aléatoirement parmi celles enregistrées
        public static MapRecord GetArenaMap()
        {
            return MapRecord.GetMap(ArenaMaps.Random().MapId);
        }
    }
}
