
using SSync.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Messages
{
    /// <summary>
    /// Message de base pour les communications inter-serveurs (World ↔ Auth).
    /// Hérite de Message et ajoute les champs nécessaires au système de requête/réponse :
    ///   - Guid : identifiant unique permettant d'associer une réponse à sa requête d'origine.
    ///   - IsRequest : true si ce message est une requête (World → Auth), false si c'est une réponse.
    ///
    /// Les classes RequestMessage et ResponseMessage héritent de TransitionMessage.
    /// </summary>
    public abstract class TransitionMessage : Message
    {
        /// <summary>
        /// Identifiant unique du message, généré lors de l'envoi d'une requête.
        /// La réponse reprend le même GUID pour permettre l'appariement côté MessagePool.
        /// </summary>
        public Guid Guid = Guid.Empty;

        /// <summary>
        /// Indique si ce message est une requête (true) ou une réponse (false).
        /// Utilisé par TransitionClient pour router le message vers HandleRequest ou HandleAnswer.
        /// </summary>
        public bool IsRequest;


    }
}
