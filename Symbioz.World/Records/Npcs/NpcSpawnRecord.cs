using Symbioz.ORM;
using Symbioz.Protocol.Enums;
using Symbioz.World.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Core;

namespace Symbioz.World.Records.Npcs
{
    /// <summary>
    /// Position d'un PNJ sur une map : associe un NpcRecord (template) à une position précise.
    ///
    /// Un même NpcRecord peut avoir plusieurs NpcSpawnRecord (même PNJ sur plusieurs maps).
    /// CellId et Direction peuvent être modifiés en jeu (GMs) et sont marqués [Update].
    ///
    /// AddNpc() crée un spawn dynamiquement et l'insère immédiatement en base de données.
    /// </summary>
    [Table("NpcsSpawns", true, 3)]
    public class NpcSpawnRecord : ITable
    {
        // Liste de tous les spawns de PNJ chargés en mémoire
        public static List<NpcSpawnRecord> NpcsSpawns = new List<NpcSpawnRecord>();

        [Primary]
        public int Id;              // Identifiant unique du spawn

        public ushort TemplateId;   // Id du NpcRecord (template du PNJ)

        public int MapId;           // Id de la map sur laquelle ce PNJ est placé

        [Update]
        public ushort CellId;       // Cellule sur laquelle se trouve le PNJ (modifiable en jeu)

        [Update]
        public sbyte Direction;     // Direction du PNJ en valeur numérique (0-7)

        // Direction sous forme d'enum (calculée depuis Direction, non persistée)
        [Ignore]
        public DirectionsEnum DirectionEnum
        {
            get
            {
                return (DirectionsEnum)Direction;
            }
            set
            {
                Direction = (sbyte)value;
            }
        }

        // Template du PNJ chargé en mémoire (non persisté, résolu depuis TemplateId)
        [Ignore]
        public NpcRecord Template
        {
            get;
            set;
        }

        public NpcSpawnRecord(int id, ushort templateId, int mapId, ushort cellId, sbyte direction)
        {
            this.Id = id;
            this.TemplateId = templateId;
            this.Template = NpcRecord.GetNpc(templateId);
            this.MapId = mapId;
            this.CellId = cellId;
            this.Direction = direction;
        }

        // Retourne tous les PNJ présents sur une map donnée
        public static List<NpcSpawnRecord> GetMapNpcs(int mapId)
        {
            return NpcsSpawns.FindAll(x => x.MapId == mapId);
        }

        // Crée et insère immédiatement un nouveau spawn de PNJ en base de données
        public static NpcSpawnRecord AddNpc(ushort templateId, int mapId, ushort cellId, sbyte direction)
        {
            var spawn = new NpcSpawnRecord(NpcsSpawns.DynamicPop(x => x.Id), templateId, mapId, cellId, direction);
            spawn.AddInstantElement();
            return spawn;
        }

        // Retourne un spawn par son identifiant
        public static NpcSpawnRecord GetSpawn(int id)
        {
            return NpcsSpawns.FirstOrDefault(x => x.Id == id);
        }
    }
}
