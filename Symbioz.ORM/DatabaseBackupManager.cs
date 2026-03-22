using Symbioz.Core;
using Symbioz.Core.DesignPattern.StartupEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.ORM
{
    /// <summary>
    /// Gère la création de sauvegardes (dumps) automatiques de la base de données.
    /// Un backup SQL est une copie complète de la base de données sous forme de fichier .sql,
    /// permettant de restaurer les données en cas de problème.
    /// Cette classe est utilisée pour planifier et exécuter des sauvegardes périodiques.
    /// </summary>
    public class DatabaseBackupProvider
    {
        /// <summary>Extension des fichiers de sauvegarde générés (.sql).</summary>
        private const string BackupFileExtension = ".sql";

        // Logger utilisé pour afficher les messages dans la console du serveur.
        static Logger logger = new Logger();

        /// <summary>Chemin du dossier où sont stockés les fichiers de backup.</summary>
        private static string BackupDirectory;

        /// <summary>
        /// Initialise le gestionnaire de backup en définissant le dossier de destination.
        /// Si le dossier n'existe pas encore, il est créé automatiquement.
        /// </summary>
        /// <param name="directory">Chemin du dossier où seront stockés les backups.</param>
        public static void Initialize(string directory)
        {
            BackupDirectory = directory;

            // Si le dossier de backup n'existe pas, on le crée.
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Effectue un dump (sauvegarde complète) de la base de données dans un fichier .sql.
        /// Le nom du fichier est basé sur la date et l'heure courantes pour éviter les collisions.
        /// En cas d'erreur (permissions, espace disque...), l'exception est loguée sans planter le serveur.
        /// </summary>
        public static void Backup()
        {
            try
            {
                // Génère un nom de fichier basé sur la date (ex : 2024-01-15_12h30.sql)
                // puis demande au DatabaseManager de faire le dump vers ce fichier.
                DatabaseManager.GetInstance().Backup(BackupDirectory + DateTime.Now.ToFileNameDate() + BackupFileExtension);
                logger.Color2("Database Dumped");
            }
            catch (Exception ex)
            {
                // En cas d'erreur, on affiche le message mais on ne plante pas le serveur.
                logger.Error(ex);
            }
        }
    }
}
