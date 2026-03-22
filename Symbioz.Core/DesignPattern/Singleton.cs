using System;
using System.Reflection;

namespace Symbioz.Core.DesignPattern
{
    /// <summary>
    /// Implémentation générique du patron de conception Singleton (inspiré de WCell/Bouh2).
    /// Garantit qu'une seule instance d'une classe existe dans tout le processus.
    ///
    /// Utilisation : hériter de Singleton&lt;MaClasse&gt; et accéder à l'instance via MaClasse.Instance.
    ///
    /// Fonctionnement interne :
    ///   - SingletonAllocator est une classe imbriquée statique dont le constructeur statique
    ///     crée l'instance unique dès le premier accès à Singleton&lt;T&gt;.
    ///   - Supporte les classes avec constructeur public ET les classes avec constructeur privé/protégé.
    /// </summary>
    /// <typeparam name="T">La classe dont on veut une instance unique.</typeparam>
    public abstract class Singleton<T> where T : class
    {
        /// <summary>
        /// Classe imbriquée chargée de créer et stocker l'instance unique.
        /// Le constructeur statique est exécuté automatiquement par le CLR lors du premier accès.
        /// </summary>
        internal static class SingletonAllocator
        {
            // L'instance unique de T
            internal static T instance;

            // Exécuté une seule fois par le CLR : crée l'instance de T
            static SingletonAllocator()
            {
                Singleton<T>.SingletonAllocator.CreateInstance(typeof(T));
            }

            /// <summary>
            /// Crée l'instance de T par réflexion.
            /// Préfère un constructeur public ; utilise le constructeur privé/protégé en fallback.
            /// </summary>
            public static T CreateInstance(Type type)
            {
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                T result;
                if (constructors.Length > 0)
                {
                    // Constructeur public disponible : utilise Activator.CreateInstance
                    result = (Singleton<T>.SingletonAllocator.instance = (T)((object)Activator.CreateInstance(type)));
                }
                else
                {
                    // Pas de constructeur public : cherche un constructeur privé ou protégé sans paramètre
                    ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, new ParameterModifier[0]);
                    if (constructor == null)
                    {
                        throw new Exception(type.FullName + " doesn't have a private/protected constructor so the property cannot be enforced.");
                    }
                    try
                    {
                        // Invoque le constructeur privé via réflexion
                        result = (Singleton<T>.SingletonAllocator.instance = (T)((object)constructor.Invoke(new object[0])));
                    }
                    catch (Exception innerException)
                    {
                        throw new Exception("The Singleton couldnt be constructed, check if " + type.FullName + " has a default constructor", innerException);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Accès à l'instance unique de T.
        /// Le getter est public ; le setter est protégé (utilisable dans la sous-classe si nécessaire).
        /// </summary>
        public static T Instance
        {
            get
            {
                return Singleton<T>.SingletonAllocator.instance;
            }
            protected set
            {
                Singleton<T>.SingletonAllocator.instance = value;
            }
        }
    }
}
