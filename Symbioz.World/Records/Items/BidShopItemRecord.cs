using Symbioz.ORM;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Items;
using Symbioz.World.Providers.Items;
using Symbioz.World.Records.Characters;
using Symbioz.World.Records.Npcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Objet mis en vente dans un hôtel des ventes (BidShop).
    /// Hérite de AbstractItem (UId, GId, Position, Quantity, Effects, AppearanceId).
    ///
    /// Un joueur peut déposer ses objets dans un BidShop (hôtel des ventes spécialisé).
    /// Chaque objet posté a un prix fixé par le vendeur et est lié à son BidShopId et AccountId.
    /// GetSellerItems() permet à un vendeur de récupérer ses propres lots en vente.
    /// GetBidShopItems() retourne tous les lots en vente dans un hôtel des ventes donné.
    /// </summary>
    [Table("BidShopItems"), Resettable]
    public class BidShopItemRecord : AbstractItem, ITable
    {
        // Liste de tous les objets en vente dans tous les hôtels des ventes chargés en mémoire
        public static List<BidShopItemRecord> BidShopItems = new List<BidShopItemRecord>();

        public int BidShopId;   // ID de l'hôtel des ventes dans lequel cet objet est posté

        public int AccountId;   // ID du compte du vendeur qui a mis cet objet en vente

        public uint Price;      // Prix de vente fixé par le vendeur (en kamas)

        public BidShopItemRecord(int bidshopId, int accountId, uint price, uint uid, ushort gid, byte position,
            uint quantity, List<Effect> effects, ushort appearanceId)
        {
            this.UId = uid;
            this.GId = gid;
            this.Position = position;
            this.Quantity = quantity;
            this.Effects = effects;
            this.AppearanceId = appearanceId;
            this.BidShopId = bidshopId;
            this.AccountId = accountId;
            this.Price = price;
        }
        public ObjectItemToSellInBid GetObjectItemToSellInBid()
        {
            return new ObjectItemToSellInBid(GId, Effects.ConvertAll<ObjectEffect>(x => x.GetObjectEffect()).ToArray(),
                UId, Quantity, Price, 50);
        }
        public BidExchangerObjectInfo GetBidExchangerObjectInfo(int[] prices)
        {
            return new BidExchangerObjectInfo(UId, Effects.ConvertAll<ObjectEffect>(x => x.GetObjectEffect()).ToArray(), prices);
        }
        public static List<BidShopItemRecord> GetSellerItems(int bidshopId, long accountId)
        {
            return BidShopItems.FindAll(x => x.BidShopId == bidshopId && x.AccountId == accountId);
        }
        public static List<BidShopItemRecord> GetBidShopItems(int bidshopId)
        {
            return BidShopItems.FindAll(x => x.BidShopId == bidshopId);
        }
        public override AbstractItem CloneWithUID()
        {
            return new BidShopItemRecord(BidShopId, AccountId, Price, UId, GId, Position, Quantity, new List<Effect>(Effects), AppearanceId);
        }

        public override AbstractItem CloneWithoutUID()
        {
            return new BidShopItemRecord(BidShopId, AccountId,
                Price, ItemUIdPopper.PopUID(), GId, Position, Quantity, new List<Effect>(Effects), AppearanceId);
        }

    }
}
