using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Objet vivant : un objet spécial dont l'apparence peut changer ("skinnable").
    /// Les objets vivants (ex: chapeau vivant, bouclier vivant) possèdent plusieurs apparences
    /// débloquées en leur faisant subir des actions spécifiques (ex: nourrir l'objet).
    /// GetSkin() retourne l'ID du skin visuel pour un skinId donné (index dans la liste).
    /// </summary>
    [Table("LivingObjects")]
    public class LivingObjectRecord : ITable
    {
        // Liste de tous les objets vivants chargés en mémoire au démarrage
        public static List<LivingObjectRecord> LivingObjects = new List<LivingObjectRecord>();

        [Primary]
        public ushort GId;          // ID du template de l'objet vivant (référence vers ItemRecord)

        public ushort ItemType;     // Type de l'objet vivant (coiffe, bouclier, etc.)

        public List<ushort> Skins;  // Liste des IDs de skins disponibles pour cet objet vivant

        [Ignore]
        public bool Skinnable
        {
            get
            {
                return Skins.Count > 0;
            }
        }

        public LivingObjectRecord(ushort gid, ushort itemType, List<ushort> skins)
        {
            this.GId = gid;
            this.ItemType = itemType;
            this.Skins = skins;
        }
        public ushort GetSkin(ushort skinId)
        {
            return Skins[skinId - 1];
        }
        public static LivingObjectRecord GetLivingObjectDatas(ushort gid)
        {
            return LivingObjects.Find(x => x.GId == gid);
        }
        public static bool IsLivingObject(ushort gid)
        {
            return LivingObjects.Find(x => x.GId == gid) != null;
        }
    }
}
