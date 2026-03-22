using SSync;
using Symbioz.Auth;
using Symbioz.Auth.Records;
using Symbioz.Auth.Transition;
using Symbioz.Core;
using Symbioz.Core.Commands;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.Network;
using Symbioz.Network.Servers;
using Symbioz.ORM;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Auth
{
    /// <summary>
    /// Point d'entrée du serveur d'authentification Symbioz.
    /// Cette classe contient la méthode Main() qui démarre le processus,
    /// ainsi que les méthodes d'initialisation annotées [StartupInvoke] qui sont
    /// appelées automatiquement par le StartupManager dans un ordre de priorité défini.
    ///
    /// Ordre de démarrage général :
    ///   1. Primitive  : chargement de la configuration (auth.xml)
    ///   2. First      : activation du SafeRun et chargement des commandes console
    ///   3. Second     : connexion à la base de données MySQL et chargement des tables
    ///   4. Tenth      : démarrage des serveurs réseau (AuthServer + TransitionServer)
    /// </summary>
    class Program
    {
        /// <summary>
        /// Référence à l'assembly courant (Symbioz.Auth).
        /// Utilisée par le StartupManager pour scanner les attributs [StartupInvoke],
        /// par SSync pour enregistrer les handlers de messages, etc.
        /// </summary>
        public static Assembly AuthAssembly = Assembly.GetAssembly(typeof(AuthConfiguration));

        // Logger utilisé pour afficher les messages de démarrage dans la console.
        static Logger logger = new Logger();

        /// <summary>
        /// Point d'entrée principal du serveur d'authentification.
        /// Affiche le message de démarrage, initialise le moteur de démarrage,
        /// puis met le thread principal en attente des commandes console.
        /// </summary>
        static void Main(string[] args)
        {
            // Affiche le logo/message de bienvenue dans la console.
            logger.OnStartup();

            // Le StartupManager parcourt l'assembly, trouve toutes les méthodes [StartupInvoke]
            // et les appelle dans l'ordre de priorité (Primitive < First < Second < ... < Tenth).
            StartupManager.Instance.Initialize(Assembly.GetExecutingAssembly());

            // Après l'initialisation, le thread principal attend des commandes tapées dans la console
            // (ex : "infos", "banip", "addaccount").
            ConsoleCommands.Instance.WaitHandle();
        }

        /// <summary>
        /// Active le mode SafeRun si configuré.
        /// En mode SafeRun, les exceptions non gérées sont interceptées globalement
        /// pour éviter un crash total du serveur.
        /// </summary>
        [StartupInvoke(StartupInvokePriority.First)]
        public static void SafeRun()
        {
            if (AuthConfiguration.Instance.SafeRun)
                ExceptionsHandler.SafeRun(); // Installe un handler global d'exceptions.
        }

        /// <summary>
        /// Initialise SSync (le framework réseau de Symbioz) avec les assemblies de messages.
        /// SSync utilise ces assemblies pour découvrir automatiquement les types de messages
        /// (Protocol) et les handlers ([MessageHandler]) qui doivent les traiter.
        /// </summary>
        [StartupInvoke("SSync", StartupInvokePriority.First)]
        public static void InitializeSSync()
        {
            // Enregistre les assemblies contenant les messages Dofus et les handlers Auth.
            SSyncCore.Initialize(Assembly.GetAssembly(typeof(RawDataMessage)),
                Assembly.GetExecutingAssembly(), AuthConfiguration.Instance.ShowProtocolMessages);

            // Initialise le gestionnaire de types du protocole Dofus (sérialisation/désérialisation).
            ProtocolTypeManager.Initialize();
        }

        /// <summary>
        /// Démarre les serveurs réseau du serveur d'authentification.
        /// - TransitionServer : écoute les connexions des serveurs World (communication interne Auth <=> World).
        /// - AuthServer : écoute les connexions des clients Dofus (joueurs).
        /// Cette méthode est appelée en priorité Tenth (la dernière), car les serveurs ne doivent
        /// démarrer qu'après que la BDD soit prête et les tables chargées.
        /// </summary>
        [StartupInvoke("Server", StartupInvokePriority.Tenth)]
        public static void StartServers()
        {
            // Démarre d'abord le TransitionServer (port interne, pour les serveurs World).
            TransitionServer.Instance.Start(AuthConfiguration.Instance.TransitionHost, AuthConfiguration.Instance.TransitionPort);

            // Démarre ensuite l'AuthServer (port public, pour les clients Dofus).
            AuthServer.Instance.Start();
        }

        /// <summary>
        /// Établit la connexion à la base de données MySQL et charge toutes les tables en mémoire.
        /// Le DatabaseManager lit l'assembly pour trouver toutes les classes [Table(catchAll=true)]
        /// et les charge dans leurs listes statiques respectives.
        /// </summary>
        [StartupInvoke("SQL Connection", StartupInvokePriority.Second)]
        public static void Connect()
        {
            // Crée le gestionnaire de base de données avec les paramètres de connexion de la configuration.
            DatabaseManager manager = new DatabaseManager(AuthAssembly,
                                           AuthConfiguration.Instance.DatabaseHost,
                                           AuthConfiguration.Instance.DatabaseName,
                                           AuthConfiguration.Instance.DatabaseUser,
                                           AuthConfiguration.Instance.DatabasePassword);

            // Ouvre et teste la connexion MySQL.
            manager.UseProvider();

            // Charge toutes les tables SQL en mémoire (celles avec catchAll = true dans [Table]).
            manager.LoadTables();
        }

        /// <summary>
        /// Initialise le système de commandes console.
        /// Scanne l'assembly pour trouver toutes les méthodes annotées [ConsoleCommand]
        /// et les enregistre pour pouvoir les appeler depuis la console du serveur.
        /// </summary>
        [StartupInvoke("Console Commands", StartupInvokePriority.First)]
        public static void LoadConsoleCommands()
        {
            ConsoleCommands.Instance.Initialize(AuthAssembly);
        }
    }
}
