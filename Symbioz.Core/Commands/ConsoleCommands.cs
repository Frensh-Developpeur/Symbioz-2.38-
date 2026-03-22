using Symbioz.Core.DesignPattern;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Symbioz.Core.Commands
{
    /// <summary>
    /// Gestionnaire des commandes console du serveur Symbioz.
    /// Singleton qui enregistre les commandes décorées avec [ConsoleCommandAttribute]
    /// et les rend disponibles via la console locale ou une connexion TCP d'administration.
    ///
    /// Fonctionnement :
    ///   1. Initialize() scanne l'assembly pour trouver toutes les méthodes [ConsoleCommand].
    ///   2. WaitHandle() démarre une boucle infinie qui lit l'entrée console et exécute les commandes.
    ///   3. ListenTcp() écoute en parallèle sur un port TCP pour permettre l'administration à distance.
    /// </summary>
    public class ConsoleCommands : Singleton<ConsoleCommands>
    {
        // Signature des méthodes de commande : prennent une chaîne d'arguments en entrée
        private delegate void ConsoleCommandDelegate(string input);

        // Dictionnaire nom_de_commande → méthode à exécuter
        private readonly Dictionary<string, ConsoleCommandDelegate> m_commands = new Dictionary<string, ConsoleCommandDelegate>();

        static Logger logger = new Logger();

        // Port TCP d'écoute pour l'administration à distance (défaut : 9600)
        private int    m_adminPort     = 9600;

        // Mot de passe requis pour les connexions TCP d'administration
        private string m_adminPassword = "admin";

        /// <summary>
        /// Scanne l'assembly fourni pour enregistrer toutes les méthodes marquées
        /// avec [ConsoleCommandAttribute] comme commandes disponibles.
        /// Ajoute aussi la commande "help" intégrée.
        /// </summary>
        public void Initialize(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    var attribute = method.GetCustomAttribute(typeof(ConsoleCommandAttribute)) as ConsoleCommandAttribute;
                    if (attribute != null)
                        // Crée un délégué typé à partir de la méthode statique trouvée par réflexion
                        m_commands.Add(attribute.Name, (ConsoleCommandDelegate)method.CreateDelegate(typeof(ConsoleCommandDelegate)));
                }
            }
            m_commands.Add("help", HelpCommand);
            logger.Gray(m_commands.Count + " command(s) registered");
        }

        /// <summary>
        /// Affiche la liste de toutes les commandes disponibles.
        /// </summary>
        private void HelpCommand(string input)
        {
            logger.Color1("Commands :");
            foreach (var item in m_commands)
                logger.Color2("  - " + item.Key);
        }

        /// <summary>
        /// Démarre la boucle principale de lecture des commandes console.
        /// Lance également en parallèle un thread d'écoute TCP pour l'admin à distance.
        /// Bloque indéfiniment (à appeler en fin d'initialisation du serveur).
        /// </summary>
        public void WaitHandle(int port = 9600, string password = "admin")
        {
            m_adminPort     = port;
            m_adminPassword = password;
            // Thread d'administration TCP en arrière-plan (s'arrête si le processus principal s'arrête)
            new Thread(ListenTcp) { IsBackground = true, Name = "AdminTcp" }.Start();

            while (true)
            {
                string input = Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    try { Handle(input); }
                    catch (Exception ex) { logger.Alert(ex.ToString()); }
                }
            }
        }

        /// <summary>
        /// Écoute les connexions TCP d'administration sur le port configuré.
        /// Authentifie le client avec un mot de passe, puis redirige Console.Out
        /// vers le client TCP (TeeWriter) pour qu'il voie les logs en temps réel.
        /// </summary>
        private void ListenTcp()
        {
            var listener = new TcpListener(IPAddress.Any, m_adminPort);
            listener.Start();
            logger.Gray($"Admin TCP listening on port {m_adminPort}...");

            while (true)
            {
                TextWriter?  previousOut = null;
                TcpClient?   tcpClient   = null;
                try
                {
                    tcpClient = listener.AcceptTcpClient();
                    var stream = tcpClient.GetStream();
                    var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                    var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

                    // Authentification : le client doit envoyer le mot de passe en première ligne
                    string? pwd = reader.ReadLine();
                    if (pwd != m_adminPassword)
                    {
                        writer.WriteLine("AUTH_FAILED");
                        tcpClient.Close();
                        continue;
                    }
                    writer.WriteLine("AUTH_OK");

                    // Redirige Console.Out → écrit aussi vers le client TCP admin
                    previousOut = Console.Out;
                    Console.SetOut(new TeeWriter(previousOut, writer));
                    logger.Gray($"[AdminTcp] Client connecté — {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logger.Gray("[Admin] " + line);
                            try { Handle(line); }
                            catch (Exception ex) { logger.Alert(ex.ToString()); }
                        }
                    }
                }
                catch (IOException)
                {
                    // Client déconnecté normalement (fin de flux)
                }
                catch (Exception ex)
                {
                    logger.Alert("[AdminTcp] " + ex.Message);
                }
                finally
                {
                    // Restaure Console.Out à la sortie standard et ferme la connexion TCP
                    if (previousOut != null)
                        Console.SetOut(previousOut);
                    tcpClient?.Close();
                }
            }
        }

        /// <summary>
        /// Analyse l'entrée saisie, identifie la commande et l'exécute avec ses arguments.
        /// Affiche [OK] ou [FAIL] selon le résultat de l'exécution.
        /// </summary>
        private void Handle(string input)
        {
            // Le premier mot (avant l'espace) est le nom de la commande
            string commandName = input.Split(null).First().ToLower();
            var command = m_commands.FirstOrDefault(x => x.Key == commandName);

            if (command.Value == null)
            {
                logger.Color2($"'{commandName}' n'est pas une commande valide. (tapez 'help')");
                return;
            }

            // Le reste de la ligne (après le nom de commande) constitue les arguments
            string args = new string(input.Skip(commandName.Length + 1).ToArray());
            try
            {
                command.Value.DynamicInvoke(args);
                if (commandName != "cmd") // cmd a son propre log OK/FAIL
                    logger.Color2($"[OK] {commandName}" + (args.Trim().Length > 0 ? $" ({args.Trim()})" : ""));
            }
            catch (Exception ex)
            {
                logger.Alert($"[FAIL] {commandName} : {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }

    /// <summary>
    /// TextWriter qui écrit simultanément vers deux destinations :
    /// la console locale d'origine et le client TCP admin connecté.
    /// Permet à l'administrateur distant de voir les logs en temps réel.
    /// </summary>
    internal class TeeWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly TextWriter _pipe;

        public TeeWriter(TextWriter console, TextWriter pipe)
        {
            _console = console;
            _pipe    = pipe;
        }

        public override Encoding Encoding => _console.Encoding;

        public override void Write(char value)
        {
            _console.Write(value);
            // Silencieux en cas d'erreur réseau (client déconnecté)
            try { _pipe.Write(value); } catch { }
        }

        public override void Write(string? value)
        {
            _console.Write(value);
            try { _pipe.Write(value); } catch { }
        }

        public override void WriteLine(string? value)
        {
            _console.WriteLine(value);
            try { _pipe.WriteLine(value); } catch { }
        }

        public override void Flush()
        {
            _console.Flush();
            try { _pipe.Flush(); } catch { }
        }
    }
}
