using Symbioz.ORM;
using Symbioz.Protocol.Types;
using Symbioz.World.Records.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Characters
{
    /// <summary>
    /// Action de démarrage : liste d'objets à remettre à un compte lors de sa prochaine connexion.
    /// Utilisé pour distribuer des récompenses, des cadeaux ou des objets de départ spéciaux
    /// (ex: pack de démarrage, récompenses d'événements) sans que le joueur soit connecté.
    /// GetStartupActions() lit directement en BDD (non mis en cache en mémoire) pour éviter les conflits.
    /// </summary>
    [Table("StartupActions", false),Resettable]
    public class StartupActionRecord : ITable
    {
        // Liste statique (non utilisée en cache, lue directement en BDD via DatabaseReader)
        public static List<StartupActionRecord> StartupActions = new List<StartupActionRecord>();

        [Primary]
        public int Id;              // Identifiant unique de l'action de démarrage

        public string Title;        // Titre affiché au joueur lors de la remise des objets

        public int AccountId;       // ID du compte qui recevra ces objets à la connexion

        public List<ushort> GIds;       // IDs des templates d'objets à remettre (GId = ItemRecord.Id)

        public List<uint> Quantities;   // Quantités correspondant à chaque objet de GIds

        public StartupActionRecord(int id, string title, int accountId, List<ushort> gIds,
            List<uint> quantities)
        {
            this.Id = id;
            this.Title = title;
            this.AccountId = accountId;
            this.GIds = gIds;
            this.Quantities = quantities;
        }
        public StartupActionAddObject GetStartupActionAddObject()
        {
            List<ObjectItemInformationWithQuantity> items = new List<ObjectItemInformationWithQuantity>();

            for (int i = 0; i < GIds.Count; i++)
            {
                ItemRecord item = ItemRecord.GetItem(GIds[i]);

                if (item != null)
                {
                    items.Add(item.GetObjectItemInformationWithQuantity(Quantities[i]));
                }
            }
            return new StartupActionAddObject(Id, Title, string.Empty, string.Empty, string.Empty, items.ToArray());
        }
        public static List<StartupActionRecord> GetStartupActions(int accountId)
        {
            lock (StartupActions)
                return DatabaseReader<StartupActionRecord>.Read("AccountId = " + accountId);
        }
    }
}
