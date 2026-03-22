using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbioz.Core;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Characters
{
    /// <summary>
    /// Notification en attente pour un compte joueur.
    /// Stocke des messages à afficher au prochain login du joueur (ex: récompenses reçues hors ligne,
    /// messages d'administration, résultats de transactions de l'hôtel des ventes).
    /// GetConnectionNotifications() est appelé lors de la connexion pour récupérer les messages en attente.
    /// </summary>
    [Table("Notifications"),Resettable]
    public class NotificationRecord : ITable
    {
        // Liste de toutes les notifications en attente chargées en mémoire
        public static List<NotificationRecord> Notifications = new List<NotificationRecord>();

        [Primary]
        public int Id;              // Identifiant unique de la notification

        public int AccountId;       // ID du compte destinataire de la notification

        public string Notification; // Texte du message à afficher lors de la connexion

        public NotificationRecord(int id, int accountId, string notification)
        {
            this.Id = id;
            this.AccountId = accountId;
            this.Notification = notification;
        }

        public static NotificationRecord[] GetConnectionNotifications(int accountId)
        {
            return Notifications.FindAll(x => x.AccountId == accountId).ToArray();
        }
        public static void Add(int accountId, string notification)
        {
            new NotificationRecord(Notifications.DynamicPop(x => x.Id), accountId, notification).AddElement();
        }
    }
}
