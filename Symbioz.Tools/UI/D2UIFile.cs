using SSync.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Tools.UI
{
    /// <summary>
    /// Représente un fichier D2UI — le format propriétaire de Dofus pour les interfaces utilisateur (UI).
    /// Un fichier D2UI contient :
    ///   - Un en-tête "D2UI" pour identifier le format
    ///   - Du contenu XML décrivant la structure de l'interface
    ///   - Un dictionnaire associant des noms de composants UI à leurs positions dans le fichier
    /// Ces fichiers sont utilisés par le client Dofus pour afficher les fenêtres, boutons, etc.
    /// </summary>
    public class D2UIFile
    {
        /// <summary>
        /// Signature d'en-tête attendue au début de tout fichier D2UI valide.
        /// </summary>
        public const string FILE_HEADER = "D2UI";

        /// <summary>
        /// Chemin complet vers le fichier sur le disque.
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// Dictionnaire associant chaque nom de composant UI à sa position (offset) dans le fichier.
        /// Permet de retrouver rapidement où se trouve chaque élément de l'interface.
        /// </summary>
        public Dictionary<string, int> UIListPosition
        {
            get;
            private set;
        }

        /// <summary>
        /// Contenu XML de l'interface utilisateur.
        /// Ce XML décrit la hiérarchie et les propriétés des éléments visuels.
        /// </summary>
        public string Xml
        {
            get;
            set;
        }

        /// <summary>
        /// Constructeur qui ouvre un fichier D2UI existant depuis le disque.
        /// </summary>
        /// <param name="path">Chemin vers le fichier .d2ui à lire.</param>
        public D2UIFile(string path)
        {
            this.Path = path;

            // Si le fichier existe, on le lit immédiatement
            if (File.Exists(path))
                this.Open();
        }

        /// <summary>
        /// Constructeur pour créer un fichier D2UI vide (pour en créer un nouveau).
        /// </summary>
        public D2UIFile()
        {
            this.UIListPosition = new Dictionary<string, int>();
            this.Xml = "";
        }

        /// <summary>
        /// Lit et analyse le contenu d'un fichier D2UI depuis le disque.
        /// Structure binaire :
        ///   [UTF : "D2UI"] [UTF : contenu XML] [short : nombre de définitions] [UTF+int : nom/position * N]
        /// </summary>
        private void Open()
        {
            BigEndianReader reader = new BigEndianReader(File.ReadAllBytes(Path));

            // Vérification de la signature du fichier
            string header = reader.ReadUTF();

            if (header != FILE_HEADER)
            {
                throw new Exception("malformated file (wrong header)");
            }

            UIListPosition = new Dictionary<string, int>();

            uint loc7 = 0;

            // Lecture du contenu XML
            this.Xml = reader.ReadUTF();

            // Lecture du nombre de définitions de composants UI
            short definitionCount = reader.ReadShort();

            // Lecture de chaque paire (nom du composant, position dans le fichier)
            while (loc7 < definitionCount)
            {
                UIListPosition.Add(reader.ReadUTF(), reader.ReadInt());
                loc7++;
            }
        }

        /// <summary>
        /// Sauvegarde le fichier D2UI sur le disque au chemin défini dans Path.
        /// Réécrit l'en-tête, le XML, puis toutes les définitions de composants.
        /// </summary>
        public void Save()
        {
            BigEndianWriter writer = new BigEndianWriter();

            // Écriture de la signature du format
            writer.WriteUTF(FILE_HEADER);

            // Écriture du contenu XML
            writer.WriteUTF(Xml);

            // Écriture du nombre de composants définis
            writer.WriteShort((short)UIListPosition.Count);

            // Écriture de chaque paire (nom, position)
            foreach (var def in UIListPosition)
            {
                writer.WriteUTF(def.Key);
                writer.WriteInt(def.Value);
            }

            File.WriteAllBytes(Path, writer.Data);

        }
    }
}
