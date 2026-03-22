using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core.DesignPattern.StartupEngine
{
    /// <summary>
    /// Moteur de démarrage du serveur.
    /// Permet d'appeler automatiquement toutes les méthodes marquées [StartupInvoke]
    /// dans l'ordre de priorité défini par StartupInvokePriority (Primitive → Last).
    ///
    /// Supporte deux types de méthodes :
    ///   - 'public static' : appelée directement
    ///   - 'public' (instance de Singleton) : récupère l'instance via la propriété "Instance"
    ///
    /// En cas d'exception dans une méthode de démarrage → arrêt immédiat du serveur.
    /// </summary>
    public class StartupManager : Singleton<StartupManager>
    {
        // Nom de la propriété statique utilisée pour récupérer l'instance d'un Singleton
        public static string SingletonInstancePropretyName = "Instance";

        Logger logger = new Logger();

        /// <summary>
        /// Lance toutes les méthodes [StartupInvoke] dans l'assembly donné,
        /// en respectant l'ordre de priorité (Primitive=0, First=1, ..., Last=11).
        /// Mesure et affiche le temps total d'initialisation.
        /// </summary>
        public void Initialize(Assembly startupAssembly)
        {
            logger.Color1("-- Initialisation --");
            Stopwatch watch = Stopwatch.StartNew();

            // Itère sur chaque niveau de priorité (0 à 11) dans l'ordre
            foreach (var pass in Enum.GetValues(typeof(StartupInvokePriority)))
            {
                foreach (var item in startupAssembly.GetTypes())
                {
                    // Trouve toutes les méthodes de cette classe avec l'attribut [StartupInvoke]
                    var methods = item.GetMethods().ToList().FindAll(x => x.GetCustomAttribute(typeof(StartupInvoke), false) != null);
                    // Filtre uniquement celles ayant la priorité du niveau actuel
                    var attributes = methods.ConvertAll<KeyValuePair<StartupInvoke, MethodInfo>>(x => new KeyValuePair<StartupInvoke, MethodInfo>(x.GetCustomAttribute(typeof(StartupInvoke), false) as StartupInvoke, x)).FindAll(x => x.Key.Type == (StartupInvokePriority)pass); ;

                    foreach (var data in attributes)
                    {
                        if (!data.Key.Hided)
                        {
                            // Affiche "(Priorité) Loading NomÉtape ..." si Hided = false
                            logger.White("(" + pass + ") Loading " + data.Key.Name + " ...");
                        }

                        Delegate del = null;

                        if (data.Value.IsStatic)
                        {
                            // Méthode statique : on peut l'appeler directement sans instance
                            del = Delegate.CreateDelegate(typeof(Action), data.Value);
                        }
                        else
                        {
                            // Méthode d'instance (Singleton) : récupère l'instance via la propriété "Instance"
                            PropertyInfo field = data.Value.DeclaringType.BaseType.GetProperty(SingletonInstancePropretyName);
                            Object singletonInstance = field.GetValue(null);
                            del = Delegate.CreateDelegate(typeof(Action), singletonInstance, data.Value.Name);
                        }
                        try
                        {
                            del.DynamicInvoke(); // Exécute la méthode de démarrage
                        }
                        catch (Exception ex)
                        {
                            // Erreur fatale au démarrage → affiche l'erreur, attend une touche, puis quitte
                            logger.Error(ex.ToString());
                            Console.ReadKey();
                            Environment.Exit(0);
                            return;
                        }
                    }


                }
            }
            watch.Stop();
            logger.Color1("-- Initialisation Complete (" + watch.Elapsed.Minutes + "min" + watch.Elapsed.Seconds + "s) --");
        }
    }
}
