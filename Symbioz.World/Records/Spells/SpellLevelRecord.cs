using Symbioz.ORM;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Providers.Fights.Effects.Movements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Spells
{
    /// <summary>
    /// Enregistrement d'un niveau de sort (grade) en base de données.
    /// Chaque sort a plusieurs niveaux (SpellLevelRecord), correspondant aux grades 1 à N.
    /// Ce record contient tous les paramètres de lancement d'un sort à un grade donné :
    ///   - Coût en PA, portée min/max, contraintes de lancer
    ///   - Probabilité de coup critique, états requis/interdits
    ///   - Liste des effets normaux et des effets critiques (EffectInstance)
    /// C'est cette classe qui est utilisée pour valider et exécuter un lancer de sort.
    /// </summary>
    [Table("SpellsLevels", true, 4)]
    public class SpellLevelRecord : ITable
    {
        // Liste statique de tous les niveaux de sorts chargés en mémoire
        public static List<SpellLevelRecord> SpellsLevels = new List<SpellLevelRecord>();

        [Primary]
        public int Id;          // Identifiant unique de ce niveau de sort

        public ushort SpellId;  // ID du sort parent (clé étrangère vers SpellRecord)

        public sbyte Grade;     // Niveau du sort (1 = niveau 1, 6 = niveau maximum)

        public short ApCost;    // Coût en Points d'Action pour lancer ce sort

        public short MinRange;  // Portée minimale (en cases)

        public short MaxRange;  // Portée maximale (en cases)

        public bool CastInLine;      // Si true, le sort ne peut être lancé qu'en ligne droite

        public bool CastInDiagonal;  // Si true, le sort peut être lancé en diagonale

        public bool CastTestLos;     // Si true, le lancer vérifie la ligne de vue (LOS)

        public short CriticalHitProbability; // Probabilité de coup critique (1/N)

        public bool NeedFreeCell;    // Si true, la cellule cible doit être vide

        public bool NeedTakenCell;   // Si true, la cellule cible doit être occupée

        public bool NeedFreeTrapCell; // Si true, la cellule ne doit pas contenir de piège

        public bool RangeCanBeBoosted; // Si true, la portée peut être augmentée par les stats

        public short MaxStacks;      // Nombre maximum d'effets de ce sort empilables sur une cible

        public short MaxCastPerTurn; // Nombre maximum de lancers par tour

        public short MaxCastPerTarget; // Nombre maximum de lancers sur la même cible par tour

        public short MinCastInterval; // Délai minimum entre deux lancers (en tours)

        public short InitialCooldown; // Délai initial avant le premier lancer (en tours)

        public short GlobalCooldown;  // Temps de recharge global partagé entre sorts du même groupe

        // États requis sur le lanceur pour pouvoir utiliser ce sort
        public List<short> StatesRequired = new List<short>();

        // États interdits sur le lanceur (le sort est bloqué si le lanceur a cet état)
        public List<short> StatesForbidden = new List<short>();

        [Xml, Update]
        // Effets déclenchés lors d'un lancer normal (sérialisés en XML en BDD)
        public List<EffectInstance> Effects;

        [Xml, Update]
        // Effets déclenchés lors d'un coup critique (sérialisés en XML en BDD)
        public List<EffectInstance> CriticalEffects;

        [Ignore]
        // True si le sort ne produit aucun effet visible (pas d'animation côté client)
        // Un sort est "silencieux" si tous ses effets appartiennent à la liste NotSilentEffects
        public bool Silent
        {
            get
            {
                return !Effects.Any(x => Invisibility.NotSilentEffects.Contains(x.EffectEnum) && x.Delay == 0 && x.Duration == 0);
            }
        }

        public SpellLevelRecord(int id, ushort spellId, sbyte grade, short apCost, short minRange,
          short maxRange, bool castInLine, bool castInDiagonal, bool castTestLos, short criticalHitProbability,
          bool needFreeCell, bool needTakenCell, bool needFreeTrapCell, bool rangeCanBeBoosted, short maxStacks,
          short maxCastPerTurn, short maxCastPerTarget, short minCastInterval, short initialCooldown, short globalCooldown,
          List<short> statesRequired, List<short> statesForbidden, List<EffectInstance> effects, List<EffectInstance> criticalEffects)
        {
            this.Id = id;
            this.SpellId = spellId;
            this.Grade = grade;
            this.ApCost = apCost;
            this.MinRange = minRange;
            this.MaxRange = maxRange;
            this.CastInLine = castInLine;
            this.CastInDiagonal = castInDiagonal;
            this.CastTestLos = castTestLos;
            this.CriticalHitProbability = criticalHitProbability;
            this.NeedFreeCell = needFreeCell;
            this.NeedTakenCell = needTakenCell;
            this.NeedFreeTrapCell = needFreeTrapCell;
            this.RangeCanBeBoosted = rangeCanBeBoosted;
            this.MaxStacks = maxStacks;
            this.MaxCastPerTurn = maxCastPerTurn;
            this.MaxCastPerTarget = maxCastPerTarget;
            this.MinCastInterval = minCastInterval;
            this.InitialCooldown = initialCooldown;
            this.GlobalCooldown = globalCooldown;
            this.StatesRequired = statesRequired;
            this.StatesForbidden = statesForbidden;
            this.Effects = effects;
            this.CriticalEffects = criticalEffects;
        }

        public static ushort GetSpellId(int spellLevelId)
        {
            return SpellsLevels.FirstOrDefault(x => x.Id == spellLevelId).SpellId;
        }
        public static SpellLevelRecord GetSpellLevel(int id)
        {
            return SpellsLevels.Find(x => x.Id == id);
        }
        public static SpellLevelRecord GetSpellLevel(ushort spellId, sbyte grade)
        {
            return SpellsLevels.Find(x => x.SpellId == spellId && x.Grade == grade);
        }
        public static List<SpellLevelRecord> GetSpellLevels(ushort spellId)
        {
            return SpellsLevels.FindAll(x => x.SpellId == spellId);
        }
    
    }
}
