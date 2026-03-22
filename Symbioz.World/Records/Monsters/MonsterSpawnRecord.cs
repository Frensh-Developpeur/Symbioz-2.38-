using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Monsters
{
    /// <summary>
    /// Enregistrement de spawn de monstre : définit quel monstre peut apparaître
    /// dans quelle sous-zone, avec quelle probabilité.
    /// Ces données sont utilisées par MonsterSpawnManager lors de la création
    /// de groupes de monstres aléatoires sur les maps.
    /// </summary>
    [Table("MonsterSpawns")]
    public class MonsterSpawnRecord : ITable
    {
        // Liste statique de tous les spawns chargés en mémoire
        public static List<MonsterSpawnRecord> MonsterSpawns = new List<MonsterSpawnRecord>();

        [Primary]
        public int Id;              // Identifiant unique du spawn

        public ushort MonsterId;    // ID du monstre qui peut spawner

        public ushort SubareaId;    // Sous-zone dans laquelle ce monstre peut apparaître

        public sbyte Probability;   // Probabilité d'apparition (% ou poids relatif)

        public MonsterSpawnRecord(int id,ushort monsterId,ushort subareaId,sbyte probability)
        {
            this.Id = id;
            this.MonsterId = monsterId;
            this.SubareaId = subareaId;
            this.Probability = probability;
        }

        // Retourne tous les spawns définis pour une sous-zone donnée
        public static List<MonsterSpawnRecord> GetSpawns(ushort subareaId)
        {
            return MonsterSpawns.FindAll(x => x.SubareaId == subareaId);
        }
    }
}
