using SSync.Arc;
using SSync.IO;
using SSync.Messages;
using SSync.Sockets;
using Symbioz.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SSync
{
    /// <summary>
    /// Cœur de la bibliothèque SSync (Socket Synchronization), écrit par Skinz.
    /// Cette classe centrale gère :
    ///   - L'enregistrement de tous les types de messages du protocole Dofus
    ///   - L'enregistrement de tous les handlers (fonctions qui traitent les messages)
    ///   - La construction (désérialisation) des messages reçus depuis le réseau
    ///   - Le dispatch (routage) des messages vers le bon handler
    /// </summary>
        public class SSyncCore
        {
            static Logger logger = new Logger();

            /// <summary>
            /// Indique si le protocole a été initialisé. Doit être true avant toute utilisation.
            /// </summary>
            public static bool Initialized = false;

            /// <summary>
            /// Types des paramètres attendus par toutes les méthodes handler :
            /// (Message message, AbstractClient client)
            /// </summary>
            private static readonly Type[] HandlerMethodParameterTypes = new Type[] { typeof(Message), typeof(AbstractClient) };

            /// <summary>
            /// Dictionnaire : ID du message → délégué (fonction handler) à appeler
            /// Rempli au démarrage par Initialize() en parcourant tous les [MessageHandler]
            /// </summary>
            private static readonly Dictionary<uint, Delegate> Handlers = new Dictionary<uint, Delegate>();

            /// <summary>
            /// Dictionnaire : ID du message (ushort) → Type C# du message
            /// Permet de savoir quel type instancier pour un ID donné
            /// </summary>
            private static readonly Dictionary<ushort, Type> Messages = new Dictionary<ushort, Type>();

            /// <summary>
            /// Dictionnaire : ID du message → constructeur sans paramètre du message
            /// Optimisation : évite la réflexion à chaque réception de message
            /// </summary>
            private static readonly Dictionary<ushort, Func<Message>> Constructors = new Dictionary<ushort, Func<Message>>();

            // Si true, affiche dans la console chaque message envoyé/reçu (utile pour déboguer)
            public static bool ShowProtocolMessage;

            /// <summary>
            /// Initialise le protocole SSync :
            /// 1. Parcourt l'assembly des messages → enregistre tous les types héritant de Message
            /// 2. Parcourt l'assembly des handlers → enregistre toutes les méthodes [MessageHandler]
            /// </summary>
            /// <param name="messagesAssembly">Assembly contenant les classes de messages (Symbioz.Protocol)</param>
            /// <param name="handlersAssembly">Assembly contenant les handlers (Symbioz.World)</param>
            /// <param name="showProtocolMessages">Si true, affiche les messages dans la console</param>
            public static void Initialize(Assembly messagesAssembly, Assembly handlersAssembly, bool showProtocolMessages)
            {
                ShowProtocolMessage = showProtocolMessages;

                // Étape 1 : enregistrement des messages (parcourt toutes les classes héritant de Message)
                foreach (var type in messagesAssembly.GetTypes().Where(x => x.IsSubclassOf(typeof(Message))))
                {
                    FieldInfo field = type.GetField("Id"); // Champ statique "Id" = identifiant numérique du message
                    if (field != null)
                    {
                        ushort num = (ushort)field.GetValue(type);
                        if (Messages.ContainsKey(num))
                        {
                            throw new AmbiguousMatchException(string.Format("MessageReceiver() => {0} item is already in the dictionary, old type is : {1}, new type is  {2}",
                                num, Messages[num], type));
                        }
                        Messages.Add(num, type);
                        // Pré-compile le constructeur pour éviter la réflexion à chaque message reçu
                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                        if (constructor == null)
                        {
                            throw new Exception(string.Format("'{0}' doesn't implemented a parameterless constructor", type));
                        }
                        Constructors.Add(num, constructor.CreateDelegate<Func<Message>>());
                    }
                }

                // Étape 2 : enregistrement des handlers (cherche les méthodes avec [MessageHandler])
                foreach (var item in handlersAssembly.GetTypes())
                {
                    foreach (var subItem in item.GetMethods())
                    {
                        var attribute = subItem.GetCustomAttribute(typeof(MessageHandlerAttribute));
                        if (attribute != null)
                        {
                            // Le premier paramètre de la méthode détermine quel message elle gère
                            Type methodParameters = subItem.GetParameters()[0].ParameterType;
                            if (methodParameters.BaseType != null)
                            {
                                try
                                {
                                    Delegate target = subItem.CreateDelegate(HandlerMethodParameterTypes);
                                    FieldInfo field = methodParameters.GetField("Id");
                                    Handlers.Add((ushort)field.GetValue(null), target);
                                }
                                catch
                                {
                                    throw new Exception("Cannot register " + subItem.Name + " has message handler...");
                                }

                            }
                        }

                    }
                }
                Initialized = true;
                logger.Gray(Messages.Count + " Message(s) Loaded | " + Handlers.Count + " Handler(s) Loaded");
            }

            /// <summary>
            /// Construit (désérialise) un message à partir de son ID et du flux de lecture.
            /// Utilise le constructeur pré-compilé pour instancier le bon type de message,
            /// puis appelle Unpack() pour lire les données depuis le flux réseau.
            /// Retourne null si l'ID est inconnu.
            /// </summary>
            private static Message ConstructMessage(ushort id, ICustomDataInput reader)
            {
                if (!Messages.ContainsKey(id))
                {
                    return null; // Message inconnu : ignore
                }
                Message message = Constructors[id](); // Crée une instance vide du bon type
                if (message == null)
                {
                    return null;
                }
                message.Unpack(reader); // Lit les données du message depuis le réseau
                return message;
            }

            /// <summary>
            /// Point d'entrée pour les données reçues :
            /// 1. Crée un lecteur binaire depuis le buffer brut
            /// 2. Analyse le header du message (MessagePart.Build) pour extraire l'ID
            /// 3. Construit le message typé correspondant
            /// Retourne null si le buffer est invalide ou l'ID inconnu.
            /// </summary>
            public static Message BuildMessage(byte[] buffer)
            {
                var reader = new CustomDataReader(buffer);
                var messagePart = new MessagePart(false);

                if (messagePart.Build(reader))
                {
                    Message message;
                    try
                    {
                        message = ConstructMessage((ushort)messagePart.MessageId.Value, reader);
                        return message;
                    }
                    catch (Exception ex)
                    {
                        logger.Alert("Error while building Message :" + ex.Message);
                        return null;
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
                else
                    return null;

            }

            /// <summary>
            /// Dispatche un message vers le handler correspondant.
            /// Cherche dans le dictionnaire Handlers un délégué dont la clé = message.MessageId,
            /// puis l'invoque avec (message, client).
            /// - Retourne true si le message a été traité (ou s'il n'a pas de handler)
            /// - Retourne false si le message est null (client déconnecté)
            /// - mute = true : supprime les logs (utilisé pour les messages internes)
            /// </summary>
            public static bool HandleMessage(Message message, AbstractClient client, bool mute = false)
            {
                if (!Initialized)
                {
                    throw new LibraryNotLoadedException("SSync Library is not initialized, call the method SSyncCore.Initialize() before launch sockets");
                }

                if (message == null && !mute)
                {
                    logger.Color2("Cannot build datas from client " + client.Ip);
                    client.Disconnect();
                    return false;
                }

                var handler = Handlers.FirstOrDefault(x => x.Key == message.MessageId);

                if (handler.Value != null)
                {
                    {
                        if (ShowProtocolMessage && !mute)
                            logger.Gray("Receive " + message.ToString());
                        try
                        {
                            handler.Value.DynamicInvoke(null, message, client); // Appelle la méthode handler
                            return true;

                        }
                        catch (Exception ex)
                        {
                            logger.Alert(string.Format("Unable to handle message {0} {1} : '{2}'", message.ToString(), handler.Value.Method.Name, ex.InnerException.ToString()));
                            return false;
                        }
                    }
                }
                else
                {
                    // Aucun handler pour ce message : log si activé, mais pas d'erreur
                    if (ShowProtocolMessage && !mute)
                        logger.White(string.Format("No Handler: ({0}) {1}", message.MessageId, message.ToString()));
                    return true;
                }
            }
        }
        /// <summary>
        /// Exception thrown when the SSync Library is not loaded
        /// </summary>
        public class LibraryNotLoadedException : Exception
        {
            public LibraryNotLoadedException() { }
            public LibraryNotLoadedException(string message) : base(message) { }
            public LibraryNotLoadedException(string message, Exception inner) : base(message, inner) { }
            protected LibraryNotLoadedException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context)
                : base(info, context) { }
        }
}
