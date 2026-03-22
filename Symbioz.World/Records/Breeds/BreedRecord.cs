using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.ORM;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Records.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Breeds
{
    /// <summary>
    /// Données statiques d'une classe de personnage (race) dans Dofus.
    ///
    /// Contient :
    ///   - Apparences (look homme/femme, couleurs par défaut)
    ///   - Stats de départ (HP, Prospection)
    ///   - Tables de coût de boost par caractéristique (SPForX : points de caractéristiques nécessaires
    ///     pour augmenter d'1 point selon le total déjà investi, ex. Iop force 1:1 jusqu'à 100, puis 1:2...)
    ///
    /// AvailableBreeds liste les classes jouables sur ce serveur.
    /// GetThreshold() / GetThresholds() permettent de calculer le coût d'un boost de statistique.
    /// </summary>
    [Table("Breeds")]
    public class BreedRecord : ITable
    {
        // Liste de toutes les classes chargées en mémoire
        public static List<BreedRecord> Breeds = new List<BreedRecord>();

        // Masque de bits des classes disponibles (envoyé au client lors de la création de personnage)
        public static uint AvailableBreedsFlags
        {
            get
            {
                return (uint)AvailableBreeds.Aggregate(0, (int current, PlayableBreedEnum breedEnum) => current | 1 << breedEnum - PlayableBreedEnum.Feca);
            }
        }

        // Classes jouables sur ce serveur (certaines peuvent être commentées pour les désactiver)
        public static readonly List<PlayableBreedEnum> AvailableBreeds = new List<PlayableBreedEnum>
        {
            PlayableBreedEnum.Feca ,
            PlayableBreedEnum.Enutrof ,
            PlayableBreedEnum.Sram,
            PlayableBreedEnum.Ecaflip,
            PlayableBreedEnum.Eniripsa ,
            PlayableBreedEnum.Iop ,
            PlayableBreedEnum.Cra ,
            PlayableBreedEnum.Sacrieur ,
            PlayableBreedEnum.Pandawa ,
            PlayableBreedEnum.Sadida,
            PlayableBreedEnum.Zobal ,
            PlayableBreedEnum.Eliotrope,
            PlayableBreedEnum.Huppermage,

           PlayableBreedEnum.Osamodas,
          PlayableBreedEnum.Xelor,
            PlayableBreedEnum.Roublard,
         // PlayableBreedEnum.Steamer,
        };

        public sbyte Id;          // Identifiant de la classe (1=Féca, 2=Osa, etc.)
        public string Name;       // Nom de la classe
        public string MaleLook;   // Look par défaut version masculine
        public string FemaleLook; // Look par défaut version féminine
        public List<int> MaleColors;   // Palette de couleurs masculines proposées à la création
        public List<int> FemaleColors; // Palette de couleurs féminines proposées à la création

        // Tables de boost par stat : CSVDoubleArray encode les seuils [points investis → coût en PS]
        // ex. [[0, 1], [100, 2], [200, 3]] = 1 PS pour les 100 premiers points, 2 PS ensuite, etc.
        public CSVDoubleArray SPForIntelligence;
        public CSVDoubleArray SPForAgility;
        public CSVDoubleArray SPForStrength;
        public CSVDoubleArray SPForVitality;
        public CSVDoubleArray SPForWisdom;
        public CSVDoubleArray SPForChance;

        // HP de départ pour un nouveau personnage de cette classe (niveau 1)
        public short StartLifePoints;

        // Prospection de départ pour un nouveau personnage de cette classe
        public short StartProspecting;

        public BreedRecord(sbyte id, string name, string malelook, string femalelook, List<int> malecolors, List<int> femalecolors,
            CSVDoubleArray spforintelligence, CSVDoubleArray spforagility, CSVDoubleArray SPForStrength, CSVDoubleArray spforvitality,
            CSVDoubleArray spforwisdom, CSVDoubleArray spforchance, short startlifepoints, short startprospecting)
        {
            this.Id = id;
            this.Name = name;
            this.MaleLook = malelook;
            this.FemaleLook = femalelook;
            this.MaleColors = malecolors;
            this.FemaleColors = femalecolors;
            this.StartLifePoints = startlifepoints;
            this.StartProspecting = startprospecting;
            this.SPForIntelligence = spforintelligence;
            this.SPForAgility = spforagility;
            this.SPForStrength = SPForStrength;
            this.SPForVitality = spforvitality;
            this.SPForWisdom = spforwisdom;
            this.SPForChance = spforchance;
        }

        // Retourne le seuil de coût actuel pour une stat donnée (ex. coût = 1 ou 2 PS selon les points investis)
        public uint[] GetThreshold(short actualpoints, StatsBoostEnum statsid)
        {
            uint[][] thresholds = this.GetThresholds(statsid);
            return thresholds[this.GetThresholdIndex((int)actualpoints, thresholds)];
        }

        // Recherche par dichotomie l'index du seuil correspondant au nombre de points investis
        public int GetThresholdIndex(int actualpoints, uint[][] thresholds)
        {
            int result;
            for (int i = 0; i < thresholds.Length - 1; i++)
            {
                if ((ulong)thresholds[i][0] <= (ulong)((long)actualpoints) && (ulong)thresholds[i + 1][0] > (ulong)((long)actualpoints))
                {
                    result = i;
                    return result;
                }
            }
            result = thresholds.Length - 1;
            return result;
        }

        // Retourne la table de seuils pour une caractéristique donnée
        public uint[][] GetThresholds(StatsBoostEnum statsid)
        {
            uint[][] result = null;
            switch (statsid)
            {
                case StatsBoostEnum.Strength:
                    result = this.SPForStrength.Values;
                    break;
                case StatsBoostEnum.Vitality:
                    result = this.SPForVitality.Values;
                    break;
                case StatsBoostEnum.Wisdom:
                    result = this.SPForWisdom.Values;
                    break;
                case StatsBoostEnum.Chance:
                    result = this.SPForChance.Values;
                    break;
                case StatsBoostEnum.Agility:
                    result = this.SPForAgility.Values;
                    break;
                case StatsBoostEnum.Intelligence:
                    result = this.SPForIntelligence.Values;
                    break;
            }
            return result;
        }
        public static BreedRecord GetBreed(int id)
        {
            return Breeds.Find(x => x.Id == id);
        }

        public static ContextActorLook GetBreedLook(int breedid, bool sex, int cosmeticid, IEnumerable<int> colors)
        {
            var breed = GetBreed(breedid);
            ContextActorLook result = sex ? ContextActorLook.Parse(breed.FemaleLook) : ContextActorLook.Parse(breed.MaleLook);
            result.AddSkin(HeadRecord.GetSkin(cosmeticid));

            int[] simpleColors = VerifiyColors(colors, sex, breed);

            result.SetColors(ContextActorLook.GetConvertedColors(simpleColors));

            return result;
        }
        public static int[] VerifiyColors(IEnumerable<int> colors, bool sex, BreedRecord breed)
        {
            List<int> defaultColors = (!sex) ? breed.MaleColors : breed.FemaleColors;

            if (colors.Count() == 0)
            {
                return defaultColors.ToArray();
            }

            int num = 0;

            List<int> simpleColors = new List<int>();
            foreach (int current in colors)
            {
                if (defaultColors.Count > num)
                {
                    simpleColors.Add((current == -1) ? (int)defaultColors[num] : current);
                }
                num++;
            }

            return simpleColors.ToArray();
        }
        public static ushort GetSkinFromCosmeticId(int cosmecticId)
        {
            return HeadRecord.GetSkin(cosmecticId);
        }
        public IEnumerable<ushort> GetSpellsForLevel(ushort level, List<CharacterSpell> actualspells)
        {
            List<BreedSpellRecord> breedSpells = BreedSpellRecord.GetBreedSpells(this.Id).FindAll(x => x.ObtainLevel <= level);

            foreach (var spell in breedSpells)
            {
                if (actualspells.Find(x => x.SpellId == spell.SpellId) == null)
                    yield return spell.SpellId;
            }

        }

    }

}
