using Symbioz.Auth.Transition;
using Symbioz.Core;
using Symbioz.Core.Commands;
using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbioz.Protocol.Selfmade.Messages;
using System.Threading.Tasks;
using System.Threading;
using Symbioz.Auth.Records;
using Symbioz.Network.Servers;

namespace Symbioz.Auth
{
    /// <summary>
    /// Répertoire des commandes console disponibles sur le serveur d'authentification.
    /// Chaque méthode annotée [ConsoleCommand("nom")] peut être appelée depuis la console
    /// du serveur en tapant "nom" suivi éventuellement d'arguments.
    ///
    /// Ces commandes permettent à l'administrateur du serveur d'effectuer des actions
    /// de gestion en temps réel sans avoir à redémarrer le serveur.
    ///
    /// Exemple d'utilisation dans la console : "infos", "banip 192.168.1.1", "addaccount"
    /// </summary>
    class CommandsRepertory
    {
        // Logger pour afficher les résultats des commandes dans la console du serveur.
        static Logger logger = new Logger();

        /// <summary>
        /// Commande "infos" : affiche les statistiques de connexion actuelles.
        /// Montre le nombre de clients (joueurs) actuellement connectés au serveur d'authentification.
        /// </summary>
        /// <param name="input">Arguments de la commande (non utilisés ici).</param>
        [ConsoleCommand("infos")]
        public static void Infos(string input)
        {
            // Affiche le nombre de clients connectés en temps réel.
            logger.White("Clients Connecteds: " + AuthServer.Instance.ClientsCount);
        }

        /// <summary>
        /// Commande "banip" : bannit une adresse IP immédiatement.
        /// L'adresse IP bannie est enregistrée dans la table BanIps de la base de données.
        /// Tout client qui tente de se connecter depuis cette IP sera refusé.
        /// Exemple d'utilisation : "banip 192.168.1.100"
        /// </summary>
        /// <param name="input">L'adresse IP à bannir (ex : "192.168.1.100").</param>
        [ConsoleCommand("banip")]
        public static void BanIp(string input)
        {
            // Crée un nouvel enregistrement de ban IP et l'insère immédiatement en base de données.
            BanIpRecord record = new BanIpRecord(input);
            record.AddInstantElement(); // Sauvegarde immédiate sans passer par le SaveTask.
        }

        /// <summary>
        /// Commande "addaccount" : crée interactivement un nouveau compte joueur.
        /// Cette commande pose des questions dans la console (username, password, nickname, rôle)
        /// et insère directement le nouveau compte dans la base de données.
        ///
        /// Rôles disponibles (sbyte) :
        ///   0 = Joueur normal (Player)
        ///   1 = Modérateur
        ///   2 = Administrateur
        ///   (etc., selon l'énumération ServerRoleEnum)
        /// </summary>
        /// <param name="input">Arguments de la commande (non utilisés, les données sont saisies interactivement).</param>
        [ConsoleCommand("addaccount")]
        public static void Account(string input)
        {
            // Le prochain ID = nombre de comptes existants + 1 (fonctionne même si la base est vide)
            int id = (int)DatabaseReader<AccountRecord>.Count("Id") + 1;

            string username, password, nickname;
            sbyte role;

            // Si des arguments sont passés directement (ex: "addaccount admin1 admin1 1")
            // Format : username password role  OU  username password nickname role
            var args = input?.Split(' ');
            if (args != null && args.Length >= 3)
            {
                username = args[0];
                password = args[1];
                if (args.Length >= 4)
                {
                    nickname = args[2];
                    role = Convert.ToSByte(args[3]);
                }
                else
                {
                    nickname = args[0]; // nickname = username par défaut
                    role = Convert.ToSByte(args[2]);
                }
            }
            else
            {
                // Saisie interactive
                Console.Write("username : ");
                username = Console.ReadLine();

                Console.Write("password : ");
                password = Console.ReadLine();

                Console.Write("nickname : ");
                nickname = Console.ReadLine();

                Console.Write("role : ");
                role = Convert.ToSByte(Console.ReadLine());
            }

            // Crée le nouvel enregistrement avec les valeurs saisies.
            // Paramètres : id, username, password, nickname, role, banned=false, characterSlots=5, lastSelectedServerId=30
            var newAccount = new AccountRecord(id, username, password, nickname, role, false, 5, 30);

            // Insère immédiatement le compte en base de données (sans passer par la file SaveTask).
            DatabaseWriter<AccountRecord>.InstantInsert(newAccount);

            Console.WriteLine("Compte créé avec succès ! ");
        }
    }
}
