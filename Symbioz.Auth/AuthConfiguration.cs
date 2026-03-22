using Symbioz.Core.DesignPattern.StartupEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Symbioz.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using YAXLib.Attributes;
using Symbioz.Protocol.Types;

namespace Symbioz.Auth
{
    /// <summary>
    /// Configuration du serveur d'authentification, chargée depuis le fichier "auth.xml".
    /// Cette classe hérite de ServerConfiguration qui fournit le mécanisme de sérialisation XML
    /// (lecture/écriture du fichier de configuration au démarrage).
    ///
    /// Si le fichier auth.xml n'existe pas au démarrage, il est créé automatiquement
    /// avec les valeurs définies dans la méthode Default().
    ///
    /// Toutes les propriétés sont publiques pour que YAXLib (le sérialiseur XML) puisse
    /// les lire et les écrire depuis/vers le fichier XML.
    /// </summary>
    [YAXComment("AuthServer Configuration")] // Commentaire qui apparaîtra dans le fichier XML généré.
    public class AuthConfiguration : ServerConfiguration
    {
        /// <summary>Nom du fichier de configuration XML du serveur d'authentification.</summary>
        public const string CONFIG_NAME = "auth.xml";

        #region Public Static

        /// <summary>
        /// Méthode de démarrage appelée automatiquement par le StartupManager (priorité Primitive).
        /// Charge la configuration depuis "auth.xml". Si le fichier n'existe pas,
        /// Default() est appelée et un nouveau fichier est créé.
        /// </summary>
        [StartupInvoke("Configuration", StartupInvokePriority.Primitive)]
        public static void Initialize()
        {
            Instance = ServerConfiguration.Load<AuthConfiguration>(CONFIG_NAME);
        }

        /// <summary>
        /// Instance unique de la configuration (pattern Singleton).
        /// Accessible partout dans le projet via AuthConfiguration.Instance.NomDeLaPropriété.
        /// </summary>
        public static AuthConfiguration Instance = null;

        #endregion

        #region Public

        /// <summary>
        /// Numéro de version du protocole Dofus attendu.
        /// Si le client envoie une version différente, la connexion est refusée.
        /// Exemple : 1709 correspond à Dofus 2.34.
        /// </summary>
        public int DofusProtocolVersion { get; set; }

        /// <summary>Type d'installation du client (1 = normal).</summary>
        public sbyte VersionInstall { get; set; }

        /// <summary>Numéro de version majeure du client Dofus attendu (ex : 2 pour Dofus 2.x).</summary>
        public sbyte VersionMajor { get; set; }

        /// <summary>Numéro de version mineure du client Dofus attendu (ex : 38 pour Dofus 2.38).</summary>
        public sbyte VersionMinor { get; set; }

        /// <summary>Numéro de release du client Dofus attendu.</summary>
        public sbyte VersionRelease { get; set; }

        /// <summary>Numéro de patch du client Dofus attendu.</summary>
        public sbyte VersionPatch { get; set; }

        /// <summary>Numéro de révision précis du client Dofus attendu (ex : 113902).</summary>
        public int VersionRevision { get; set; }

        /// <summary>Type de technologie utilisée par le client (1 = Adobe AIR).</summary>
        public sbyte VersionTechnology { get; set; }

        /// <summary>Type de build du client (0 = Release).</summary>
        public sbyte VersionBuildType { get; set; }

        /// <summary>
        /// Construit et retourne un objet VersionExtended à partir des paramètres de configuration.
        /// Cet objet est utilisé dans les messages de protocole Dofus pour comparer la version
        /// du client avec la version attendue par le serveur.
        /// </summary>
        /// <returns>Objet VersionExtended prêt à être envoyé dans un message de protocole.</returns>
        public VersionExtended GetVersionExtended()
        {
            return new VersionExtended(VersionMajor, VersionMinor, VersionRelease,
                VersionRevision, VersionPatch, VersionBuildType, VersionInstall, VersionTechnology);
        }

        /// <summary>
        /// Définit les valeurs par défaut de la configuration.
        /// Cette méthode est appelée automatiquement si le fichier auth.xml n'existe pas encore.
        /// Elle initialise toutes les propriétés avec des valeurs fonctionnelles pour Dofus 2.38.
        /// </summary>
        public override void Default()
        {
            // Paramètres de connexion à la base de données MySQL.
            DatabaseHost = "127.0.0.1";     // Serveur MySQL local.
            DatabaseUser = "root";           // Utilisateur MySQL.
            DatabasePassword = string.Empty; // Mot de passe MySQL (vide par défaut).
            DatabaseName = "auth";           // Nom de la base de données.

            // Adresse IP et port sur lesquels l'AuthServer écoute les clients Dofus.
            Host = "127.0.0.1";
            Port = 443; // Port 443 (normalement HTTPS, utilisé ici pour passer les pare-feux).

            // Afficher ou non les messages de protocole dans la console (utile pour le debug).
            ShowProtocolMessages = true;

            // Le mode SafeRun intercepte les exceptions non gérées pour éviter les crashs.
            SafeRun = false;

            // Version du protocole Dofus (1709 = Dofus 2.34, mais le serveur tourne en 2.38).
            DofusProtocolVersion = 1709;

            // Adresse IP et port du TransitionServer (communication interne avec les serveurs World).
            TransitionHost = "127.0.0.1";
            TransitionPort = 600;

            // Paramètres de version du client Dofus 2.38.1 attendu.
            VersionInstall = 1;
            VersionTechnology = 1;
            VersionBuildType = 0;
            VersionMajor = 2;
            VersionMinor = 38;
            VersionPatch = 1;
            VersionRelease = 0;
            VersionRevision = 113902;
        }

        #endregion
    }
}
