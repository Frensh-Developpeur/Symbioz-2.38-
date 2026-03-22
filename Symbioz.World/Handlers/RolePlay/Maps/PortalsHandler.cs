using SSync.Messages;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Entities;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay.Maps
{
    /// <summary>
    /// Gère les messages liés aux portails de téléportation sur les cartes.
    /// Un portail est un objet interactif spécial qui téléporte le joueur
    /// vers une destination prédéfinie (autre carte, autre cellule).
    /// </summary>
    class PortalsHandler
    {
        /// <summary>
        /// Reçu quand un joueur interagit avec un portail (clic dessus ou passage dessus).
        /// Recherche le portail par son ID sur la map courante et déclenche son utilisation.
        /// Si le portail est introuvable (déjà fermé, mauvaise map), l'action est ignorée.
        /// </summary>
        [MessageHandler]
        public static void HandlePortalUseRequest(PortalUseRequestMessage message, WorldClient client)
        {
            // Cherche le portail par son identifiant dans la liste des portails de la map
            Portal portal = client.Character.Map.Instance.GetPortal((int)message.portalId);

            if (portal != null)
                portal.Use(client.Character); // Téléporte le joueur vers la destination du portail
        }
    }
}
