using Symbioz.ORM;
using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Spells
{
    /// <summary>
    /// Enregistrement d'un sort dans la base de données.
    /// Un sort (Spell) contient les métadonnées générales (nom, description, catégorie)
    /// et une liste de niveaux (SpellLevelRecord) correspondant aux différents grades du sort.
    /// Par exemple, "Boule de feu" peut avoir 6 niveaux, chacun avec des paramètres différents
    /// (coût PA, portée, effets, probabilité de critique...).
    /// </summary>
    [Table("Spells", true, 5)]
    public class SpellRecord : ITable
    {
        // Liste statique de tous les sorts chargés en mémoire au démarrage
        public static List<SpellRecord> Spells = new List<SpellRecord>();

        [Primary]
        public ushort Id;       // Identifiant unique du sort

        public string Name;     // Nom du sort (ex: "Ronce", "Boule de feu")

        public string Description; // Description textuelle affichée au joueur

        // Liste des IDs de niveaux associés à ce sort (clés étrangères vers SpellLevelRecord)
        public List<int> SpellsLevels;

        [Ignore]
        // Niveaux chargés en mémoire (non sérialisés en BDD, calculés depuis SpellsLevels)
        public List<SpellLevelRecord> Levels;

        [Update]
        public sbyte Category;  // Catégorie du sort (stockée comme sbyte en BDD)

        [Ignore]
        // Catégorie sous forme d'enum typé pour une utilisation pratique dans le code
        public SpellCategoryEnum CategoryEnum
        {
            get
            {
                return (SpellCategoryEnum)Category;
            }
            set
            {
                Category = (sbyte)value;
            }
        }

        // Retourne le niveau du sort correspondant au grade demandé (1, 2, 3...)
        public SpellLevelRecord GetLevel(sbyte grade)
        {
            return Levels.Find(x => x.Grade == grade);
        }
        // Retourne le dernier niveau disponible du sort (grade maximum)
        public SpellLevelRecord GetLastLevel()
        {
            return Levels.Last();
        }
        // Retourne le numéro de grade du dernier niveau (utile pour connaître le max)
        public sbyte GetLastLevelGrade()
        {
            return GetLastLevel().Grade;
        }

        public SpellRecord(ushort id, string name, string description, List<int> spellLevels, sbyte category)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
            this.SpellsLevels = spellLevels;
            this.Levels = SpellLevelRecord.GetSpellLevels(Id);
            this.Category = category;
        }

        public static SpellRecord GetSpellRecord(ushort id)
        {
            return Spells.Find(x => x.Id == id);
        }
    }
}
