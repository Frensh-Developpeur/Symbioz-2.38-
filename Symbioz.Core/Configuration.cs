using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core
{
    /// <summary>
    /// Classe de base abstraite pour toutes les configurations du serveur Symbioz.
    /// Gère la sérialisation/désérialisation XML via YAXLib.
    ///
    /// Utilisation typique :
    ///   1. Créer une classe dérivée (ex. WorldConfiguration) avec les champs de config.
    ///   2. Implémenter Default() pour remplir les valeurs par défaut.
    ///   3. Appeler Load<T>("config.xml") au démarrage : charge si le fichier existe,
    ///      le crée avec les valeurs par défaut sinon.
    /// </summary>
    public abstract class Configuration
    {
        static Logger logger = new Logger();

        /// <summary>
        /// Sauvegarde une instance de configuration dans un fichier XML.
        /// Le fichier est créé dans le répertoire courant de l'application.
        /// </summary>
        public static void Save(Configuration instance, string fileName)
        {
            File.WriteAllText(Environment.CurrentDirectory + "/" + fileName, instance.XMLSerialize());
        }

        /// <summary>
        /// Lit et désérialise une configuration XML depuis un chemin absolu.
        /// </summary>
        public static T ReadConfiguration<T>(string path)
        {
            return File.ReadAllText(path).XMLDeserialize<T>();
        }

        /// <summary>
        /// À implémenter dans chaque sous-classe pour initialiser les valeurs par défaut.
        /// Appelé automatiquement quand aucun fichier de configuration n'existe.
        /// </summary>
        public abstract void Default();

        /// <summary>
        /// Charge une configuration depuis un fichier XML, ou la crée avec les valeurs par défaut.
        /// Si le fichier existe mais est corrompu, propose à l'utilisateur de le réinitialiser.
        /// </summary>
        /// <typeparam name="T">Type de configuration à charger (doit hériter de Configuration).</typeparam>
        /// <param name="fileName">Nom du fichier (relatif au répertoire courant).</param>
        public static T Load<T>(string fileName) where T : Configuration
        {
            string path = Environment.CurrentDirectory + "/" + fileName;
            if (File.Exists(path))
            {
                try
                {
                    T configuration = ReadConfiguration<T>(path);
                    return configuration;
                }
                catch (Exception ex)
                {
                label:
                    logger.Error(ex.Message);
                    logger.Color2("Unable to load configuration do you want to use default configuration?");
                    logger.Color2("y/n?");
                    ConsoleKeyInfo answer = Console.ReadKey(true);
                    if (answer.Key == ConsoleKey.Y)
                    {
                        // Supprime le fichier corrompu et recharge (créera le fichier par défaut)
                        File.Delete(path);
                        return Load<T>(fileName);
                    }
                    if (answer.Key == ConsoleKey.N)
                    {
                        Environment.Exit(0);
                    }
                    goto label;
                }
            }
            else
            {
                // Le fichier n'existe pas : crée une instance avec les valeurs par défaut et la sauvegarde
                T configuration = Activator.CreateInstance<T>();
                configuration.Default();
                Save(configuration, fileName);
                logger.Color2("Configuration Created");
                return configuration;
            }

        }
    }
}
