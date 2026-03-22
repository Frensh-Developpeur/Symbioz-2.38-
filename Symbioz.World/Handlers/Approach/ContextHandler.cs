using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.Approach
{
    /// <summary>
    /// Handler du message de création de contexte de jeu.
    /// Le "contexte" dans Dofus représente l'état actuel du personnage :
    ///   - ROLE_PLAY : monde ouvert (déplacement, PNJ, échanges...)
    ///   - FIGHT : en combat
    /// Ce handler est appelé après la sélection du personnage, quand le client
    /// demande à initialiser son contexte pour entrer dans le monde.
    /// </summary>
    class ContextHandler
    {
        /// <summary>
        /// Reçu une fois que le client a terminé le chargement initial et demande à entrer dans le monde.
        /// Détruit l'éventuel contexte précédent, crée un contexte RolePlay, puis téléporte le personnage
        /// sur la dernière carte et cellule enregistrées en base.
        /// </summary>
        [MessageHandler]
        public static void HandleCreateContextRequest(GameContextCreateRequestMessage message, WorldClient client)
        {
            // Nettoie tout contexte existant (combat en cours, etc.) avant d'en créer un nouveau
            client.Character.DestroyContext();

            // Crée le contexte RolePlay : envoie GameContextCreateMessage au client
            client.Character.CreateContext(GameContextEnum.ROLE_PLAY);

            // Notifie le système que le contexte vient d'être créé (lance les événements liés)
            client.Character.OnContextCreated();

            // Envoie les statistiques actualisées du personnage au client
            client.Character.RefreshStats();

            // Place le personnage sur la carte et la cellule enregistrées dans son record (dernière position)
            client.Character.Teleport(client.Character.Record.MapId, client.Character.Record.CellId);
        }

    }
}
