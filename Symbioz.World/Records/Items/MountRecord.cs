using Symbioz.ORM;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities.Look;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Template d'un modèle de monture (Dragodinde, Mulou, etc.).
    /// Contient les données partagées entre toutes les montures de ce modèle :
    ///   - Apparence visuelle (Look), nom du modèle
    ///   - GId du certificat de monture correspondant (objet en inventaire)
    ///   - Effets de départ (statistiques de base, tirées à la création dans CharacterMountRecord)
    ///
    /// GetMount() peut être appelé soit par ItemGId (depuis un certificat), soit par Id (ModelId).
    /// </summary>
    [Table("Mounts")]
    public class MountRecord : ITable
    {
        // Liste de tous les templates de montures chargés en mémoire au démarrage
        public static List<MountRecord> Mounts = new List<MountRecord>();

        [Primary]
        public int Id;          // Identifiant unique du modèle de monture

        public string Name;     // Nom du modèle (ex: "Dragodinde Rousse", "Mulou")

        public ContextActorLook Look;   // Apparence visuelle de la monture

        [Update]
        public ushort ItemGId;  // GId de l'objet "Certificat de monture" correspondant (dans ItemRecord)

        [Xml, Update]
        public List<EffectInstance> Effects; // Statistiques de base de ce modèle (tirées à la création)

        public MountRecord(int id, string name, ContextActorLook look, ushort itemGId, List<EffectInstance> effects)
        {
            this.Id = id;
            this.Name = name;
            this.Look = look;
            this.ItemGId = itemGId;
            this.Effects = effects;
        }

        public static MountRecord GetMount(ushort itemGID)
        {
            return Mounts.Find(x => x.ItemGId == itemGID);
        }
        public static MountRecord GetMount(int modelId)
        {
            return Mounts.Find(x => x.Id == modelId);
        }

    }
}
