using Symbioz.ORM;
using Symbioz.World.Models.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Protocol.Types;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Providers.Items;
using Symbioz.World.Models.Entities;
using Symbioz.World.Records.Characters;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.Protocol.Enums;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Template d'un objet du jeu (modèle de base partagé par tous les exemplaires).
    ///
    /// ItemRecord contient les données immuables d'un type d'objet :
    ///   - Identité : Id, Name, TypeId, Level
    ///   - Apparence : AppearanceId
    ///   - Prix de base et poids
    ///   - Effets de base : liste de EffectInstance (statistiques avec fourchette min/max)
    ///   - Criteria : conditions d'utilisation (ex. restrictions de classe ou de niveau)
    ///
    /// CharacterItemRecord hérite indirectement de ce template (via GId → ItemRecord.Id).
    /// GetCharacterItem() crée un exemplaire joueur à partir du template en tirant les effets aléatoires.
    /// </summary>
    [Table("Items", true, 6)]
    public class ItemRecord : ITable
    {
        // Liste statique de tous les templates d'objets chargés en mémoire au démarrage
        public static List<ItemRecord> Items = new List<ItemRecord>();

        [Primary]
        public ushort Id;           // Identifiant unique du type d'objet (GId)

        public string Name;         // Nom de l'objet affiché en jeu

        public ushort TypeId;       // Type numérique (coiffe, épée, parchemin, etc.)

        // Type d'objet sous forme d'enum (calculé depuis TypeId, non persisté)
        [Ignore]
        public ItemTypeEnum TypeEnum { get { return (ItemTypeEnum)TypeId; } }

        [Update]
        public ushort AppearanceId; // Identifiant de l'apparence graphique de l'objet

        public ushort Level;        // Niveau requis pour utiliser l'objet

        public int Price;           // Prix de vente de base (chez les PNJ marchands)

        public int Weight;          // Poids de l'objet (affecte la capacité de sac)

        [Xml, Update]
        public List<EffectInstance> Effects; // Effets du template (statistiques avec min/max)

        public string Criteria;     // Conditions d'équipement (ex. "PO>50" = niveau > 50)

        // Indique si cet objet fait partie d'un set d'équipement
        public bool HasSet { get { return ItemSetRecord.GetItemSet(Id) != null; } }

        // Retourne le set d'équipement auquel appartient cet objet (null si aucun)
        public ItemSetRecord ItemSet { get { return ItemSetRecord.GetItemSet(Id); } }

        // Indique si cet objet est une arme (présent dans WeaponRecord)
        public bool Weapon { get { return WeaponRecord.Weapons.Find(x => x.Id == Id) != null; } }

        public ItemRecord(ushort id, string name, ushort typeId, ushort apparenceId, ushort level,
            int price, int weight, List<EffectInstance> effects, string criteria)
        {
            this.Id = id;
            this.Name = name;
            this.TypeId = typeId;
            this.AppearanceId = apparenceId;
            this.Level = level;
            this.Price = price;
            this.Weight = weight;
            this.Effects = effects;
            this.Criteria = criteria;

        }
        // Crée un exemplaire joueur à partir de ce template.
        // Si perfect = true, les effets sont générés au maximum ; sinon les valeurs sont aléatoires.
        public CharacterItemRecord GetCharacterItem(long characterId, uint quantity, bool perfect = false)
        {
            var effects = Effects.ConvertAll<Effect>(x => x.GenerateEffect(perfect));
            effects.RemoveAll(x => x == null);
            var item = new CharacterItemRecord(characterId, 0, Id, (byte)CharacterInventoryPositionEnum.INVENTORY_POSITION_NOT_EQUIPED,
                quantity, effects, AppearanceId);
            return item;
        }

        // Retourne le prix à afficher.
        // Si levelPrice = true, calcule un prix approximatif basé sur le niveau (Level/2).
        // Sinon, retourne le prix de base du template.
        public int GetPrice(bool levelPrice)
        {
            if (levelPrice)
            {
                int result = (int)((double)Level / (double)2);
                return result == 0 ? 1 : result;
            }
            else
            {
                return Price;
            }
        }

        // Sérialise l'objet pour l'affichage dans une boutique PNJ (protocole)
        public ObjectItemToSellInNpcShop GetObjectItemToSellInNpcShop(bool levelPrice)
        {
            return new ObjectItemToSellInNpcShop(Id,
                Effects.ConvertAll<ObjectEffect>(w => w.GetTemplateObjectEffect()).ToArray(),
                (uint)GetPrice(levelPrice), string.Empty);
        }

        // Sérialise l'objet comme objet non contenu dans un inventaire (ex. objet au sol)
        public ObjectItemNotInContainer GetObjectItemNotInContainer(uint uid, uint quantity)
        {
            return new ObjectItemNotInContainer(Id, Effects.ConvertAll<ObjectEffect>(x => x.GetTemplateObjectEffect()).ToArray(),
                uid, quantity);
        }

        // Sérialise l'objet avec sa quantité (pour l'échange ou la boutique)
        public ObjectItemInformationWithQuantity GetObjectItemInformationWithQuantity(uint quantity)
        {
            return new ObjectItemInformationWithQuantity(Id, Effects.ConvertAll<ObjectEffect>(x => x.GetTemplateObjectEffect()).ToArray(),
                quantity);
        }

        // Retourne le template d'un objet par son GId
        public static ItemRecord GetItem(ushort gid)
        {
            return Items.Find(x => x.Id == gid);
        }

        // Retourne tous les templates d'un type d'objet donné
        public static ItemRecord[] GetItems(ItemTypeEnum type)
        {
            return Items.FindAll(x => x.TypeEnum == type).ToArray();
        }

        // Retourne un objet aléatoire d'un type donné
        public static ItemRecord RandomItem(ItemTypeEnum type)
        {
            return GetItems(type).Random();
        }

        // Retourne un objet aléatoire correspondant au prédicat donné
        public static ItemRecord RandomItem(Predicate<ItemRecord> predicate)
        {
            return Items.FindAll(predicate).Random();
        }
        public override string ToString()
        {
            return Name;
        }

    }
}
