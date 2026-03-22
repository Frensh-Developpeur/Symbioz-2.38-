using Symbioz.Core;
using Symbioz.Core.DesignPattern.StartupEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Symbioz.World
{
    /// <summary>
    /// Classe de configuration du serveur de jeu (WorldServer).
    /// Elle est chargée depuis le fichier "world.xml" au démarrage.
    /// Hérite de ServerConfiguration qui contient les paramètres réseau de base
    /// (Host, Port, base de données, etc.).
    /// Les propriétés sont sérialisées/désérialisées automatiquement via YAXLib (XML).
    /// </summary>
    public class WorldConfiguration : ServerConfiguration
    {
        public const string CONFIG_NAME = "world.xml";

        // Instance singleton accessible partout dans le projet
        public static WorldConfiguration Instance;

        #region Public Static

        /// <summary>
        /// Charge la configuration depuis le fichier XML "world.xml".
        /// Si le fichier n'existe pas, il est créé avec les valeurs par défaut (méthode Default()).
        /// Priorité "Primitive" = s'exécute en tout premier au démarrage.
        /// </summary>
        [StartupInvoke("Configuration", StartupInvokePriority.Primitive)]
        public static void Initialize()
        {
            Instance = Configuration.Load<WorldConfiguration>(CONFIG_NAME);
        }



        #endregion

        #region Public

        // Si true, le serveur utilise CustomHost comme adresse IP publique
        public bool UseCustomHost
        {
            get;
            set;
        }

        // Adresse IP personnalisée pour les connexions entrantes (si UseCustomHost = true)
        public string CustomHost
        {
            get;
            set;
        }

        // Identifiant unique du serveur (ex: 30 = serveur Manya)
        public short ServerId
        {
            get;
            set;
        }

        // Nom affiché du serveur dans la liste des serveurs
        public string ServerName
        {
            get;
            set;
        }

        // Type de serveur (1 = standard, valeurs définies par le protocole Dofus)
        public sbyte ServerType
        {
            get;
            set;
        }

        // Intervalle en secondes entre deux sauvegardes automatiques des personnages
        public int SaveInterval
        {
            get;
            set;
        }

        // Si true, une sauvegarde de la BDD est effectuée régulièrement
        public bool PerformBackup
        {
            get;
            set;
        }

        // ID de la map de départ pour les nouveaux personnages
        public int StartMapId
        {
            get;
            set;
        }

        // ID de la cellule de départ sur la map de départ
        public ushort StartCellId
        {
            get;
            set;
        }

        // Quantité de kamas donnée à la création d'un personnage
        public int StartKamas
        {
            get;
            set;
        }

        // Multiplicateur de kamas gagnés lors des combats (1 = taux normal)
        public int KamasRate
        {
            get;
            set;
        }

        // Multiplicateur de chance de drop des items (1 = taux normal)
        public int DropsRate
        {
            get;
            set;
        }

        // Multiplicateur d'XP de métier lors des crafts
        public int CraftRate
        {
            get;
            set;
        }

        // Niveau de départ des nouveaux personnages
        public ushort StartLevel
        {
            get;
            set;
        }

        // Si true, la cinématique d'intro est jouée lors de la connexion
        public bool PlayDefaultCinematic
        {
            get;
            set;
        }

        // Message d'accueil affiché au joueur lors de sa connexion
        public string WelcomeMessage
        {
            get;
            set;
        }

        // Clé de chiffrement utilisée pour déchiffrer les maps (format D2P)
        public string MapKey
        {
            get;
            set;
        }

        // Limite maximale de PA (Points d'Action) qu'un personnage peut avoir
        public short ApLimit
        {
            get;
            set;
        }

        // Limite maximale de PM (Points de Mouvement) qu'un personnage peut avoir
        public short MpLimit
        {
            get;
            set;
        }


        /// <summary>
        /// Valeurs par défaut utilisées lors de la première génération du fichier world.xml.
        /// </summary>
        public override void Default()
        {
            this.DatabaseHost = "127.0.0.1";
            this.DatabaseUser = "root";
            this.DatabasePassword = string.Empty;
            this.DatabaseName = "world";
            this.Host = "127.0.0.1";
            this.Port = 5555;
            this.ShowProtocolMessages = true;
            this.SafeRun = true;
            this.StartMapId = 156762120;
            this.StartCellId = 142;
            this.ServerId = 30;
            this.ServerName = "Many";
            this.ServerType = 1;
            this.TransitionHost = "127.0.0.1";
            this.TransitionPort = 600;
            this.SaveInterval = 300;
            this.PerformBackup = true;
            this.MapKey = "649ae451ca33ec53bbcbcc33becf15f4";
            this.PlayDefaultCinematic = true;
            this.UseCustomHost = false;
            this.CustomHost = string.Empty;
            this.StartLevel = 1;
            this.StartKamas = 0;
            this.KamasRate = 1;
            this.DropsRate = 1;
            this.CraftRate = 20;
            this.WelcomeMessage = "Welcome on server";
            this.ApLimit = 12;
            this.MpLimit = 6;
        }

        #endregion
    }
}
