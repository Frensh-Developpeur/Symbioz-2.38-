using Symbioz.ORM;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Items;
using Symbioz.World.Providers.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Objet stocké dans la banque d'un compte.
    /// Hérite de AbstractItem (UId, GId, Position, Quantity, Effects, AppearanceId).
    ///
    /// Contrairement à CharacterItemRecord (objet dans l'inventaire d'un personnage),
    /// un BankItemRecord est lié à un AccountId (le compte), pas à un personnage.
    /// Tous les personnages du même compte partagent donc la même banque.
    /// </summary>
    [Table("BankItems"), Resettable]
    public class BankItemRecord : AbstractItem, ITable
    {
        // Liste de tous les objets en banque chargés en mémoire au démarrage
        public static List<BankItemRecord> BankItems = new List<BankItemRecord>();

        public int AccountId; // ID du compte propriétaire de cet objet en banque

        public BankItemRecord(int accountId, uint uid, ushort gid, byte position,
           uint quantity, List<Effect> effects, ushort appearanceId)
        {
            this.UId = uid;
            this.GId = gid;
            this.Position = position;
            this.Quantity = quantity;
            this.Effects = effects;
            this.AccountId = accountId;
            this.AppearanceId = appearanceId;
        }

        public override AbstractItem CloneWithUID()
        {
            return new BankItemRecord(this.AccountId, this.UId, this.GId, this.Position, this.Quantity, this.Effects, this.AppearanceId);
        }

        public override AbstractItem CloneWithoutUID()
        {
            return new BankItemRecord(this.AccountId, ItemUIdPopper.PopUID(), this.GId, this.Position, this.Quantity, this.Effects, this.AppearanceId);
        }

        public static List<BankItemRecord> GetBankItems(int accountId)
        {
            return BankItems.FindAll(x => x.AccountId == accountId);
        }
    }
}
