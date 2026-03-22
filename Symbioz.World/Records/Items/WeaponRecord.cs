using Symbioz.ORM;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Template d'une arme du jeu. Étend ItemRecord avec des propriétés spécifiques aux armes :
    ///   - Coût en PA, portée min/max, contraintes de lancer (ligne, diagonale, LOS)
    ///   - Coup critique : bonus et probabilité
    ///   - Arme à deux mains, échangeable, éthérée (disparaît après utilisation)
    ///
    /// À la construction, ToItemRecord() crée automatiquement un ItemRecord correspondant
    /// et l'insère dans ItemRecord.Items pour que les armes soient accessibles via GetItem().
    /// </summary>
    [Table("Weapons", true, 7)]
    public class WeaponRecord : ITable
    {
        // Liste de tous les templates d'armes chargés en mémoire au démarrage
        public static List<WeaponRecord> Weapons = new List<WeaponRecord>();

        [Primary]
        public ushort Id;               // Identifiant unique de l'arme (même que GId dans ItemRecord)

        public string Name;             // Nom de l'arme

        public short CraftXpRatio;      // Ratio d'XP accordé lors du craft de cette arme

        public short MaxRange;          // Portée maximale de l'arme (en cases)

        public sbyte CriticalHitBonus;  // Bonus de dégâts supplémentaires lors d'un coup critique

        public short MinRange;          // Portée minimale de l'arme (en cases, 0 = corps à corps)

        public short MaxCastPerTurn;    // Nombre maximum d'utilisations par tour

        public bool Etheral;            // Si true, l'arme disparaît après utilisation

        [Ignore]
        public ItemRecord Template;     // ItemRecord généré automatiquement depuis cette arme (non persisté)

        [Update]
        public ushort AppearanceId;     // Identifiant de l'apparence graphique de l'arme

        public ushort Level;            // Niveau requis pour équiper cette arme

        public bool Exchangeable;       // Si true, l'arme peut être échangée entre joueurs

        public int RealWeight;          // Poids réel de l'arme (en pods)

        public bool CastTestLos;        // Si true, vérifie la ligne de vue avant le lancer

        public string Criteria;         // Conditions d'équipement (ex: classe, niveau minimum)

        public sbyte CriticalHitProbability; // Probabilité de coup critique (1/N)

        public bool TwoHanded;          // Si true, l'arme occupe les deux mains (incompatible avec bouclier)

        public int ItemSetId;           // ID du set d'équipement auquel appartient cette arme (0 si aucun)

        public bool CastInDiagonal;     // Si true, l'arme peut être utilisée en diagonale

        public int Price;               // Prix de vente de base chez les marchands PNJ

        public short ApCost;            // Coût en Points d'Action pour utiliser cette arme

        public bool CastInLine;         // Si true, l'arme ne peut être utilisée qu'en ligne droite

        [Xml, Update]
        public List<EffectInstance> Effects; // Effets de l'arme (dégâts, effets spéciaux) avec min/max

        public ushort TypeId;           // Type d'arme en valeur numérique

        [Ignore]
        public ItemTypeEnum TypeEnum    // Type d'arme sous forme d'enum (calculé depuis TypeId, non persisté)
        {
            get { return (ItemTypeEnum)TypeId; }
        }

        public WeaponRecord(ushort id, string name, short craftXpRatio, short maxRange, sbyte criticalHitBonus,
            short minRange, short maxCastPerTurn, bool etheral, ushort appearanceId, ushort level,
           bool exchangeable, int realWeight, bool castTestLos, string criteria, sbyte criticalHitProbability,
            bool twoHanded, int itemSetId, bool castInDiagonal, int price, short apCost, bool castInLine,
            List<EffectInstance> effects, ushort typeId)
        {
            this.Id = id;
            this.Name = name;
            this.CraftXpRatio = craftXpRatio;
            this.MaxRange = maxRange;
            this.CriticalHitBonus = criticalHitBonus;
            this.MinRange = minRange;
            this.MaxCastPerTurn = maxCastPerTurn;
            this.Etheral = etheral;
            this.AppearanceId = appearanceId;
            this.Level = level;
            this.Exchangeable = exchangeable;
            this.RealWeight = realWeight;
            this.CastTestLos = castTestLos;
            this.Criteria = criteria;
            this.CriticalHitProbability = criticalHitProbability;
            this.TwoHanded = twoHanded;
            this.ItemSetId = itemSetId;
            this.CastInDiagonal = castInDiagonal;
            this.Price = price;
            this.ApCost = apCost;
            this.CastInLine = castInLine;
            this.Effects = effects;
            this.TypeId = typeId;
            this.Template = ToItemRecord();

            ItemRecord.Items.Add(Template);
        }
        private ItemRecord ToItemRecord()
        {
            return new ItemRecord(Id, Name, TypeId, AppearanceId, Level, Price, RealWeight, Effects, Criteria);
        }
        public static WeaponRecord GetWeapon(ushort id)
        {
            return Weapons.Find(x => x.Id == id);
        }
    }
}
