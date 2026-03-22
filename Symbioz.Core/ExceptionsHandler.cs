using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Symbioz.Core
{
    /// <summary>
    /// Gestionnaire global des exceptions non gérées du serveur.
    /// En appelant SafeRun() au démarrage, toute exception non interceptée
    /// déclenchera un log d'erreur et un arrêt propre du processus (code 1).
    ///
    /// Cela évite que le serveur reste dans un état corrompu après un crash inattendu.
    /// </summary>
    public class ExceptionsHandler
    {
        static Logger logger = new Logger();

        /// <summary>
        /// Active la surveillance globale des exceptions non gérées.
        /// À appeler une seule fois au démarrage de l'application, avant tout autre code.
        /// </summary>
        public static void SafeRun()
        {
            // Abonne le handler à l'événement global d'exception non gérée du domaine d'application
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            logger.Gray("Safe Run handled");
        }

        /// <summary>
        /// Appelé automatiquement par le runtime quand une exception n'est capturée nulle part.
        /// Logue l'erreur et termine le processus pour éviter un état incohérent.
        /// </summary>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error("a fatal error has occured.. restarting application");
            // Code 1 = arrêt anormal (peut être utilisé par un script de redémarrage automatique)
            Environment.Exit(1);
        }
    }
}
