using SSync;
using Symbioz.Core;
using Symbioz.Core.Commands;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.ORM;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.RawMessages;
using Symbioz.Protocol.Types;
using Symbioz.Tools.D2P;
using Symbioz.Tools.SWL;
using Symbioz.World.Models.Entities.Jobs;
using Symbioz.World.Network;
using Symbioz.World.Providers.Fights.Results;
using Symbioz.World.Records;
using Symbioz.World.Records.Characters;
using Symbioz.World.Records.Guilds;
using Symbioz.World.Records.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Symbioz.World
{
    /// <summary>
    /// Point d'entrée principal du serveur de jeu Symbioz.
    /// Cette classe initialise tous les composants du serveur dans le bon ordre :
    /// configuration, base de données, protocole réseau, et démarrage du serveur TCP.
    /// </summary>
    class Program
    {
        // Référence à l'assembly courant, utilisée pour la réflexion (chargement des handlers, records, etc.)
        public static Assembly WorldAssembly = Assembly.GetAssembly(typeof(WorldConfiguration));

        static Logger logger = new Logger();

        /// <summary>
        /// Méthode principale : affiche le logo de démarrage, lance le thread principal
        /// et attend des commandes console de l'administrateur.
        /// </summary>
        static void Main(string[] args)
        {
            logger.OnStartup();
            // Le thread principal est séparé pour que la console reste réactive
            Thread safeThread = new Thread(new ThreadStart(RunMainThread));
            safeThread.Start();
            // Bloque le thread principal en attente de commandes console
            ConsoleCommands.Instance.WaitHandle(9601, "symbiozadmin");
        }

        /// <summary>
        /// Lance le gestionnaire de démarrage qui exécute toutes les méthodes
        /// marquées [StartupInvoke] dans le bon ordre de priorité,
        /// puis autorise les connexions réseau.
        /// </summary>
        static void RunMainThread()
        {
            StartupManager.Instance.Initialize(WorldAssembly);
            TransitionServerManager.Instance.AllowConnections();

        }

        /// <summary>
        /// Active le mode "SafeRun" si configuré : intercepte les exceptions non gérées
        /// pour éviter un crash brutal du serveur.
        /// </summary>
        [StartupInvoke(StartupInvokePriority.First)]
        public static void SafeRun()
        {
            if (WorldConfiguration.Instance.SafeRun)
                ExceptionsHandler.SafeRun();
        }

        /// <summary>
        /// Tente de se connecter au serveur d'authentification (AuthServer)
        /// via la connexion de transition (TransitionServer).
        /// </summary>
        [StartupInvoke(StartupInvokePriority.First)]
        public static void JoinAuthAuth()
        {
            TransitionServerManager.Instance.TryToJoinAuthServer();
        }

        /// <summary>
        /// Initialise le protocole réseau SSync :
        /// - charge tous les types de messages depuis l'assembly du protocole
        /// - charge tous les handlers depuis l'assembly du monde
        /// - initialise le gestionnaire de types de protocole
        /// </summary>
        [StartupInvoke("SSync", StartupInvokePriority.First)]
        public static void InitializeSSync()
        {
            SSyncCore.Initialize(Assembly.GetAssembly(typeof(RawDataMessage)),
                WorldAssembly, WorldConfiguration.Instance.ShowProtocolMessages);
            ProtocolTypeManager.Initialize();
        }

        /// <summary>
        /// Démarre le serveur TCP principal qui accepte les connexions des joueurs.
        /// </summary>
        [StartupInvoke("Server", StartupInvokePriority.Tenth)]
        public static void StartServers()
        {
            WorldServer.Instance.Start();

        }

        /// <summary>
        /// Établit la connexion à la base de données MySQL et charge toutes les tables
        /// (records) en mémoire via le DatabaseManager.
        /// </summary>
        [StartupInvoke("SQL Connection", StartupInvokePriority.Second)]
        public static void Connect()
        {

            DatabaseManager manager = new DatabaseManager(WorldAssembly, WorldConfiguration.Instance.DatabaseHost,
                                           WorldConfiguration.Instance.DatabaseName,
                                            WorldConfiguration.Instance.DatabaseUser,
                                           WorldConfiguration.Instance.DatabasePassword);

            manager.UseProvider();

            manager.LoadTables();

        }

        /// <summary>
        /// Charge et enregistre toutes les commandes console disponibles
        /// pour l'administration du serveur.
        /// </summary>
        [StartupInvoke("Console Commands", StartupInvokePriority.First)]
        public static void LoadConsoleCommands()
        {
            ConsoleCommands.Instance.Initialize(WorldAssembly);
        }




    }
}
