using SSync;
using SSync.Messages;
using SSync.Transition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Transition
{
    /// <summary>
    /// Pool de requêtes asynchrones inter-serveurs (World ↔ Auth).
    ///
    /// Le système Transition permet au serveur World d'envoyer une requête au serveur Auth
    /// et d'attendre une réponse asynchrone via un callback.
    ///
    /// Fonctionnement :
    ///   1. SendRequest() envoie un message au serveur Auth avec un GUID unique et enregistre
    ///      le callback dans m_requests
    ///   2. Quand le serveur Auth répond (HandleAnswer()), le GUID permet de retrouver
    ///      la requête en attente et d'appeler le callback
    ///   3. HandleRequest() traite les requêtes entrantes (Auth → World)
    ///
    /// Exemple d'usage : demander au serveur Auth si la suppression d'un personnage est autorisée.
    /// </summary>
    public class MessagePool
    {
        // Liste des requêtes en attente de réponse (protégée par lock)
        private static List<IMessageRequest> m_requests = new List<IMessageRequest>();

        // Traite une requête entrante venant du serveur Auth (ex. notification de déconnexion)
        public static void HandleRequest(TransitionClient client, TransitionMessage message)
        {
            SSyncCore.HandleMessage(message, client, true);
        }

        // Retrouve la requête en attente via son GUID et appelle son callback avec la réponse
        public static void HandleAnswer(TransitionClient client, TransitionMessage message)
        {
            lock (m_requests)
            {
                var request = m_requests.FirstOrDefault(x => x.Guid == message.Guid);
                request.ProcessMessage(message);
                m_requests.Remove(request);
            }
        }

        // Envoie une requête au serveur Auth et enregistre le callback pour la réponse de type T.
        // Le GUID unique permet d'associer la réponse à la bonne requête en attente.
        public static void SendRequest<T>(TransitionClient client, TransitionMessage message, RequestCallbackDelegate<T> requestCallback, RequestCallbackErrorDelegate errorCallback = null) where T : TransitionMessage
        {
            lock (m_requests)
            {
                var messageRequest = new MessageRequest<T>(requestCallback, Guid.NewGuid(), errorCallback);
                m_requests.Add(messageRequest);
                client.Send(messageRequest.Guid, message, true);
            }
        }
    }
}
