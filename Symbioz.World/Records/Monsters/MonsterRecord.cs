using Symbioz.Core;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.ORM;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Providers.Brain.Actions;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Monsters
{
    /// <summary>
    /// Enregistrement d'un monstre dans la base de données.
    /// Contient toutes les données statiques du template de monstre :
    ///   - Apparence visuelle (Look), identité (nom, race, boss/mini-boss)
    ///   - Sorts utilisables (liste d'IDs + SpellRecord chargés en mémoire)
    ///   - Grades (niveaux du monstre, avec stats différentes par grade)
    ///   - Loots possibles (MonsterDrop), kamas droppés (min/max), puissance
    ///   - Comportement IA (BehaviorName) pour les monstres spéciaux
    /// </summary>
    [Table("Monsters", true, 9)]
    public class MonsterRecord : ITable
    {
        // Liste statique de tous les templates de monstres chargés en mémoire au démarrage
        public static List<MonsterRecord> Monsters = new List<MonsterRecord>();

        [Primary]
        public ushort Id;       // Identifiant unique du monstre

        public string Name;     // Nom du monstre (ex: "Tofu", "Dragon Cochon")

        [Update]
        public ContextActorLook Look; // Apparence visuelle du monstre (sprite, couleurs)

        public bool IsBoss;     // True si le monstre est un boss (drops spéciaux, comportement)

        public bool IsMiniBoss; // True si le monstre est un mini-boss

        public short Race;      // Race du monstre (pour les immunités, calculs de dégâts de zone)

        public bool UseSummonSlot;  // True si ce monstre occupe un slot d'invocation
        public bool UseBombSlot;    // True si ce monstre occupe un slot de bombe (Roublard)

        [Xml, Update]
        // Liste des objets que ce monstre peut dropper, avec leurs probabilités
        public List<MonsterDrop> Drops;

        // IDs des sorts que ce monstre peut lancer (clés vers SpellRecord)
        public List<ushort> Spells;

        [Ignore]
        // Sorts chargés en mémoire (non sérialisés, calculés depuis Spells au chargement)
        public List<SpellRecord> SpellRecords;

        [Xml]
        // Grades du monstre (grade 1 = le plus faible, grade N = le plus fort)
        // Chaque grade a ses propres stats (vie, force, agilité...)
        public List<MonsterGrade> Grades;

        [Update]
        public int MinDroppedKamas; // Kamas minimum droppés à la mort

        [Update]
        public int MaxDroppedKamas; // Kamas maximum droppés à la mort

        [Update]
        public int Power;           // Puissance du monstre (utilisé dans les calculs de XP)

        // Nom du comportement IA personnalisé (ex: "Agressive", "DragonPig")
        // Vide/null = comportement générique via EnvironmentAnalyser
        public string BehaviorName;

        public MonsterRecord(ushort id, string name, ContextActorLook look, bool isBoss, bool isMiniboss, short race,
            bool useSummonSlot, bool useBombSlot, List<MonsterDrop> drops, List<ushort> spells, List<MonsterGrade> grades,
            int minDroppedKamas, int maxDroppedKamas, int power, string behaviorName)
        {
            this.Id = id;
            this.Name = name;
            this.Look = look;
            this.IsBoss = isBoss;
            this.IsMiniBoss = isMiniboss;
            this.Race = race;
            this.UseSummonSlot = useSummonSlot;
            this.UseBombSlot = useBombSlot;
            this.Drops = drops;
            this.Spells = spells;
            this.Grades = grades;
            this.MinDroppedKamas = minDroppedKamas;
            this.MaxDroppedKamas = maxDroppedKamas;
            this.SpellRecords = Spells.ConvertAll<SpellRecord>(x => SpellRecord.GetSpellRecord(x));
            this.Power = power;
            this.BehaviorName = behaviorName;
        }
        // Vérifie si le grade demandé existe pour ce monstre
        public bool GradeExist(sbyte gradeId)
        {
            return Grades.Count >= gradeId;
        }
        // Retourne le dernier grade (le plus puissant)
        public MonsterGrade LastGrade()
        {
            return Grades.Last();
        }
        // Retourne le grade à l'index donné (grade 1 = index 0 dans la liste)
        public MonsterGrade GetGrade(sbyte gradeId)
        {
            return Grades[gradeId - 1];
        }
        // Tire aléatoirement un grade entre 1 et le nombre total de grades
        public sbyte RandomGrade(AsyncRandom random)
        {
            sbyte value = (sbyte)(random.Next(1, Grades.Count + 1));
            return value;
        }
        // Recherche un template de monstre par son ID dans la liste statique
        public static MonsterRecord GetMonster(ushort id)
        {
            return Monsters.Find(x => x.Id == id);
        }

    }
}
