using Symbioz.Core;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Maps
{
    /// <summary>
    /// Sous-zone de jeu (zone géographique regroupant plusieurs maps).
    ///
    /// Une SubareaRecord est utilisée pour :
    ///   - Regrouper les maps d'une même zone (ex. "Plaine des Scarafeuilles")
    ///   - Définir quels monstres peuvent y spawner (liste Monsters)
    ///   - Définir le taux d'expérience spécifique à cette zone (ExperienceRate)
    ///
    /// MonsterSpawnRecord référence SubareaId pour filtrer les spawns par zone.
    /// </summary>
    [Table("Subareas", true, 10)]
    public class SubareaRecord : ITable
    {
        // Liste de toutes les sous-zones chargées en mémoire
        public static List<SubareaRecord> Subareas = new List<SubareaRecord>();

        [Primary]
        public ushort Id;       // Identifiant unique de la sous-zone

        public string Name;     // Nom affiché de la zone

        // Liste des IDs de monstres pouvant spawner dans cette sous-zone
        public List<ushort> Monsters;

        // Multiplicateur d'expérience pour les combats dans cette zone (en pourcentage)
        public int ExperienceRate;

        public SubareaRecord(ushort id, string name, List<ushort> monsters, int experienceRate)
        {
            this.Id = id;
            this.Name = name;
            this.Monsters = monsters;
            this.ExperienceRate = experienceRate;
        }

        public static SubareaRecord GetSubarea(ushort id)
        {
            return Subareas.Find(x => x.Id == id);
        }

        public override string ToString()
        {
            return Name;
        }


    }
}
