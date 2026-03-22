using Symbioz.ORM;
using Symbioz.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Core;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Entities.Alignment;
using Symbioz.World.Models.Entities.Jobs;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Entities.HumanOptions;
using Symbioz.World.Models.Entities.Arena;
using Symbioz.World.Models.Entities.Shortcuts;
using Symbioz.Protocol.Enums;

namespace Symbioz.World.Records.Characters
{
    /// <summary>
    /// Enregistrement base de données d'un personnage joueur.
    /// Mappé sur la table SQL "Characters".
    /// Les champs marqués [Update] sont sauvegardés en base lors des sauvegardes automatiques.
    /// Les champs marqués [Ignore] ne sont pas sérialisés en base (données calculées en mémoire).
    /// [Primary] = clé primaire de la table.
    /// </summary>
    [Table("Characters", true, 1), Resettable]
    public class CharacterRecord : ITable
    {
        // Liste statique de tous les personnages chargés en mémoire
        public static List<CharacterRecord> Characters = new List<CharacterRecord>();

        [Primary]
        public long Id;         // Identifiant unique du personnage (auto-incrémenté en BDD)

        [Update]
        public string Name;     // Nom du personnage (modifiable via remodelage)

        public int AccountId;   // ID du compte propriétaire de ce personnage

        [Update]
        public ContextActorLook Look; // Apparence visuelle (classe, couleurs, accessoires)

        [Update]
        public sbyte BreedId;   // Classe du personnage (1=Féca, 2=Osa, etc.)

        [Update]
        public ushort CosmeticId; // ID de la tête/apparence cosmétique

        public bool Sex;        // Sexe du personnage (false=homme, true=femme)

        [Update]
        public int MapId;       // ID de la map où se trouve le personnage

        [Update]
        public ushort CellId;   // ID de la cellule sur la map actuelle

        [Update]
        public sbyte Direction; // Direction vers laquelle le personnage regarde (0-7)

        [Update]
        public int Kamas;       // Kamas (monnaie du jeu) détenus par le personnage

        [Update]
        public ulong Exp;       // Points d'expérience totaux du personnage

        [Update]
        public int SpawnPointMapId;

        [Update]
        public List<byte> KnownEmotes;

        [Xml, Update]
        public Stats Stats;

        [Update]
        public ushort SpellPoints;

        [Update]
        public ushort StatsPoints;

        [Xml, Update]
        public CharacterAlignment Alignment;

        [Update]
        public List<ushort> KnownOrnaments;

        [Update]
        public List<ushort> KnownTitles;

        [Xml, Update]
        public List<CharacterJob> Jobs;

        [Update]
        public List<short> DoneObjectives;

        [Xml, Update]
        public List<CharacterSpell> Spells;

        [Xml, Update]
        public List<CharacterHumanOption> HumanOptions;

        [Xml, Update]
        public ArenaRank ArenaRank;

        [Xml, Update]
        public List<CharacterShortcut> Shortcuts;

        [Update]
        public int LastAlmanachDay;

        [Update]
        public int GuildId;

        [Update]
        public int Prestige;

        [Ignore]
        public bool Muted;

        [Update]
        public sbyte RemodelingMask;

        [Update]
        public sbyte StatusId;

        [Ignore]
        public List<ushort> Idols;

        public CharacterRemodelingEnum RemodelingMaskEnum
        {
            get
            {
                return (CharacterRemodelingEnum)RemodelingMask;
            }
            set
            {
                RemodelingMask = (sbyte)value;
            }
        }

        public CharacterRecord(long id, string name, int accountId, ContextActorLook look, sbyte breedId, ushort cosmeticId, bool sex, int mapId, ushort cellid,
            sbyte direction, int kamas, ulong exp, int spawnPointMapId, List<byte> knownEmotes, Stats stats,
            ushort spellsPoints, ushort statsPoints, CharacterAlignment alignment, List<ushort> knownOrnaments, List<ushort> knownTitles,
            List<CharacterJob> jobs, List<short> doneObjectives, List<CharacterSpell> spells,
            List<CharacterHumanOption> humanOptions, ArenaRank arenaRank, List<CharacterShortcut> shortcuts, int lastAlmanachDay,
            int guildId, int prestige, sbyte remodelingMask, sbyte statusId)
        {
            this.Id = id;
            this.Name = name;
            this.AccountId = accountId;
            this.Look = look;
            this.BreedId = breedId;
            this.CosmeticId = cosmeticId;
            this.Sex = sex;
            this.MapId = mapId;
            this.CellId = cellid;
            this.Direction = direction;
            this.Kamas = kamas;
            this.Exp = exp;
            this.SpawnPointMapId = spawnPointMapId;
            this.KnownEmotes = knownEmotes;
            this.Stats = stats;
            this.SpellPoints = spellsPoints;
            this.StatsPoints = statsPoints;
            this.Alignment = alignment;
            this.KnownOrnaments = knownOrnaments;
            this.KnownTitles = knownTitles;
            this.Jobs = jobs;
            this.DoneObjectives = doneObjectives;
            this.Spells = spells;
            this.HumanOptions = humanOptions;
            this.ArenaRank = arenaRank;
            this.Shortcuts = shortcuts;
            this.LastAlmanachDay = lastAlmanachDay;
            this.GuildId = guildId;
            this.Prestige = prestige;
            this.Muted = false;
            this.RemodelingMask = remodelingMask;
            this.Idols = new List<ushort>();
            this.StatusId = statusId;
        }
        public void Restat(bool addStatPoints)
        {
            int vitality = this.Stats.Vitality.Base;
            this.Stats.LifePoints -= vitality;
            this.Stats.MaxLifePoints -= vitality;
            this.Stats.Vitality.Base = 0;
            this.Stats.Agility.Base = 0;
            this.Stats.Intelligence.Base = 0;
            this.Stats.Chance.Base = 0;
            this.Stats.Strength.Base = 0;
            this.Stats.Wisdom.Base = 0;

            if (addStatPoints)
                this.StatsPoints = (ushort)(5 * ExperienceRecord.GetCharacterLevel(this.Exp) - 5);
        }
        public void AddToRemodelingMask(CharacterRemodelingEnum mask)
        {
            RemodelingMaskEnum = RemodelingMaskEnum | mask;
        }
        public CharacterToRemodelInformations GetCharacterToRemodelInformations()
        {
            return new CharacterToRemodelInformations((ulong)Id, Name, BreedId, Sex, CosmeticId, Look.Colors.ToArray(), RemodelingMask, 0);
        }
        public CharacterBaseInformations GetCharacterBaseInformations()
        {
            return new CharacterBaseInformations((ulong)Id, Name, (byte)ExperienceRecord.GetCharacterLevel(Exp),
                Look.ToEntityLook(), BreedId, Sex);
        }

        public static List<CharacterRecord> GetCharactersByAccountId(int accountId)
        {
            return Characters.FindAll(x => x.AccountId == accountId);
        }

        public static CharacterRecord New(long id, string name, int accountId, ContextActorLook look, sbyte breedId, ushort cosmeticId,
            bool sex)
        {
            ushort level = WorldConfiguration.Instance.StartLevel;
            var record = new CharacterRecord(id, name, accountId, look, breedId, cosmeticId, sex
                , WorldConfiguration.Instance.StartMapId,
                WorldConfiguration.Instance.StartCellId, 1, WorldConfiguration.Instance.StartKamas,
                ExperienceRecord.GetExperienceForLevel(level).Player, -1
                , new List<byte>() { 1 }, Stats.New(level, breedId), 0, 0
                , CharacterAlignment.New(), new List<ushort>(), new List<ushort>(), CharacterJob.New().ToList(),
                new List<short>(), new List<CharacterSpell>(), new List<CharacterHumanOption>(), ArenaRank.New(), new List<CharacterShortcut>(), 0, 0, 0, 0, (sbyte)PlayerStatusEnum.PLAYER_STATUS_AVAILABLE);

            return record;

        }
        public static bool NameExist(string name)
        {
            return Characters.Find(x => x.Name == name) != null;
        }
        /// <summary>
        /// Slow!
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static long GetIdFromName(string name)
        {
            return Characters.Find(x => x.Name == name).Id;
        }
        /// <summary>
        /// Slow
        /// </summary>
        public static string GetNameFromId(long id)
        {
            return Characters.FirstOrDefault(x => x.Id == id).Name;
        }
        public static CharacterRecord GetCharacterFromIndexByPrestige(int index)
        {
            var characters = CharacterRecord.Characters.OrderByDescending(x => x.Prestige).Take(12).ToArray();
            if (index < characters.Count())
                return characters[index - 1];
            else
                return null;
        }
        [RemoveWhereId]
        public static List<CharacterRecord> Remove(long id)
        {
            return new List<CharacterRecord>() { Characters.Find(x => x.Id == (long)id) };
        }
        /// <summary>
        /// Slow
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CharacterRecord GetRecord(long id)
        {
            return Characters.FirstOrDefault(x => x.Id == id);
        }

    }
}
