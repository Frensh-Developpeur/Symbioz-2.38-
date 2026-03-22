using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Core
{
    /// <summary>
    /// Utilitaire de logging console avec couleurs.
    /// Chaque instance mémorise la classe qui l'a créée (via la pile d'appel)
    /// pour afficher le nom de la classe entre crochets : [NomDeLaClasse] message
    /// </summary>
    public class Logger
    {
        // Type de la classe qui a créé ce Logger (pour afficher [NomClasse] dans les logs)
        public Type ClassType { get; set; }

        /// <summary>
        /// Constructeur : remonte la pile d'appel (StackFrame) pour trouver
        /// automatiquement la classe qui instancie ce Logger.
        /// Exemple : new Logger() dans WorldServer → ClassType = WorldServer
        /// </summary>
        public Logger()
        {
            StackFrame frame = new StackFrame(1, false);
            this.ClassType = frame.GetMethod().DeclaringType;
        }

        // Affiche une ligne vide dans la console
        public void NewLine()
        {
            Console.WriteLine(Environment.NewLine);
        }

        // Affiche en vert vif (utilisé pour les succès, étapes importantes)
        public void Color1(object value, bool writeType = true)
        {
            Write(value, ConsoleColor.Green, writeType == false ? null : ClassType);
        }

        // Affiche en vert foncé (utilisé pour les infos secondaires)
        public void Color2(object value, bool writeType = true)
        {
            Write(value, ConsoleColor.DarkGreen, writeType == false ? null : ClassType);
        }

        // Affiche en gris (messages standards, chargements)
        public void Gray(object value)
        {
            Write(value, ConsoleColor.Gray, ClassType);
        }

        // Affiche en gris foncé (messages très détaillés, protocole réseau)
        public void DarkGray(object value)
        {
            Write(value, ConsoleColor.DarkGray, ClassType);
        }

        // Affiche en blanc (messages neutres, handlers sans correspondance)
        public void White(object value)
        {
            Write(value, ConsoleColor.White, ClassType);
        }

        // Affiche en rouge (erreurs critiques)
        public void Error(object value)
        {
            Write(value, ConsoleColor.Red, ClassType);
        }

        // Affiche en rouge foncé (alertes, problèmes non bloquants)
        public void Alert(object value)
        {
            Write(value, ConsoleColor.DarkRed, ClassType);
        }

        /// <summary>
        /// Méthode centrale d'affichage :
        /// - Change la couleur de la console
        /// - Si classType != null, préfixe le message avec [NomDeLaClasse]
        /// </summary>
        public static void Write(object value, ConsoleColor color, Type classType = null)
        {
            Console.ForegroundColor = color;
            if (classType != null)
                Console.WriteLine("[" + classType.Name + "] " + value);
            else
                Console.WriteLine(value);
        }

        // Surcharge générique : permet d'utiliser Write<MaClasse>("msg", couleur)
        public static void Write<T>(string value, ConsoleColor color)
        {
            Write(value, color, typeof(T));
        }

        // Affiche le logo ASCII "Symbioz" en couleurs alternées au démarrage
        private void Logo()
        {
            Color1(@"  _________            ___.   .__              ", false);
            Color2(@" /   _____/__.__. _____\_ |__ |__| ____________", false);
            Color1(@" \_____  <   |  |/     \| __ \|  |/  _ \___   /", false);
            Color2(@" /        \___  |  Y Y  \ \_\ \  (  <_> )    / ", false);
            Color1(@"/_______  / ____|__|_|  /___  /__|\____/_____ \", false);
            Color2(@"        \/\/          \/    \/               \/", false);
            Color1(@"Dofus 2.38.0.113902.1", false);

        }

        /// <summary>
        /// Appelée au tout début du démarrage :
        /// Met le titre de la fenêtre console = nom de l'assembly (ex: "Symbioz.World")
        /// puis affiche le logo et les crédits.
        /// </summary>
        public void OnStartup()
        {
            Console.Title = Assembly.GetCallingAssembly().GetName().Name;
            Logo();
            NewLine();
            Color2("Written by Skinz", false);
            Color2("Check out my repo's!: https://github.com/Skinz3/", false);
            NewLine();
        }
    }
}
