using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Characters
{
    /// <summary>
    /// Correspondance entre un ID de cosmétique de tête et le SkinId utilisé dans le Look du personnage.
    /// Lors de la création d'un personnage, CosmeticId (choix de la tête) est traduit en SkinId
    /// grâce à cette table pour construire le ContextActorLook correct.
    /// </summary>
    [Table("Heads")]
    public class HeadRecord : ITable
    {
        // Liste de toutes les têtes cosmétiques chargées en mémoire
        public static List<HeadRecord> Heads = new List<HeadRecord>();

        public int Id;          // ID du cosmétique de tête (CosmeticId dans CharacterRecord)

        public ushort SkinId;   // ID du skin visuel correspondant (ajouté au Look du personnage)

        public HeadRecord(int id, ushort skinid)
        {
            this.Id = id;
            this.SkinId = skinid;
        }

        public static ushort GetSkin(int cosmeticid)
        {
            return Heads.Find(x => x.Id == cosmeticid).SkinId;
        }
    }
}
