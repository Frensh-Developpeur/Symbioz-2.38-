using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.ORM;
using Symbioz.World.Models.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Template d'un familier (pet) : stocke les effets de base potentiellement octroyés par ce familier.
    /// L'Id correspond au GId de l'objet familier dans ItemRecord.
    /// Effects contient les statistiques que le familier peut conférer à son porteur.
    /// Utilisé lors de l'équipement d'un familier pour appliquer ses bonus au personnage.
    /// </summary>
    [Table("Pets")]
    public class PetRecord : ITable
    {
        // Liste de tous les templates de familiers chargés en mémoire au démarrage
        public static List<PetRecord> Pets = new List<PetRecord>();

        [Primary]
        public ushort Id;   // GId de l'objet familier (référence vers ItemRecord)

        [Xml]
        public List<EffectInstance> Effects;    // Statistiques conférées par ce familier (sérialisées en XML)

        public PetRecord(ushort id, List<EffectInstance> effects)
        {
            this.Id = id;
            this.Effects = effects;
        }


    }
}
