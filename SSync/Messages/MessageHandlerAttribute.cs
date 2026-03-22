using System;

namespace SSync.Messages
{
    /// <summary>
    /// Attribut C# à placer sur les méthodes qui traitent un type de message réseau.
    ///
    /// Usage :
    ///   [MessageHandler]
    ///   public static void HandleMapMovementRequest(GameMapMovementRequestMessage message, WorldClient client)
    ///   { ... }
    ///
    /// Le SSyncCore parcourt tous les types de l'assembly au démarrage,
    /// trouve les méthodes avec cet attribut, et les enregistre dans le dictionnaire Handlers.
    /// Quand un message du type correspondant est reçu, la méthode est appelée automatiquement.
    ///
    /// Convention de nommage : Handle + NomDuMessage (ex: HandleMapMovementRequest)
    /// </summary>
    public class MessageHandlerAttribute : Attribute
    {
        public MessageHandlerAttribute() { }
    }
}
