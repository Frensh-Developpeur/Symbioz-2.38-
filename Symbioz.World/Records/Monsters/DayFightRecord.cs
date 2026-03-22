using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Monsters
{
    /// <summary>
    /// Combat journalier : définit un groupe de monstres spéciaux apparaissant sur une map
    /// un jour de la semaine précis (ex: chaque vendredi sur la map des arènes).
    /// GetDayFight() retourne l'entrée correspondant au jour actuel du serveur.
    /// </summary>
    [Table("DayFights")]
    public class DayFightRecord : ITable
    {
        // Liste de tous les combats journaliers chargés en mémoire
        public static List<DayFightRecord> DayFights = new List<DayFightRecord>();

        public string DayOfWeek;        // Jour de la semaine en anglais (ex: "Monday", "Friday")

        public int MapId;               // ID de la map sur laquelle le combat journalier a lieu

        public List<ushort> Monsters;   // Liste des IDs de monstres composant ce combat journalier

        [Ignore]
        public DayOfWeek DayOfWeekEnum
        {
            get
            {
                return (DayOfWeek)Enum.Parse(typeof(DayOfWeek), DayOfWeek);
            }
        }
        public DayFightRecord(string dayOfWeek, int mapId, List<ushort> monsters)
        {
            this.DayOfWeek = dayOfWeek;
            this.MapId = mapId;
            this.Monsters = monsters;
        }

        public static DayFightRecord GetDayFight()
        {
            return DayFights.Find(x => x.DayOfWeekEnum == DateTime.Now.DayOfWeek);
        }
    }
}
