using SSync.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Tools.D2I
{
    /// <summary>
    /// Parseur et éditeur du format D2I — le fichier de traductions/internationalisation de Dofus.
    /// Un fichier D2I contient toutes les chaînes de texte du jeu (noms d'objets, descriptions de sorts,
    /// textes d'interface, etc.) indexées par un identifiant entier ou une clé textuelle.
    ///
    /// Structure du fichier D2I :
    ///   [int : position de l'index]
    ///   [données texte brutes]
    ///   [index principal : tableau de (int key, bool diacritics, int dataPos)]
    ///   [index textuel : tableau de (UTF key, int dataPos)]
    ///   [index de tri : tableau de int (clés triées alphabétiquement)]
    /// </summary>
    public class D2IFile
    {
        // Entrées indexées par un identifiant entier (ex: nom d'un sort avec id=1234)
        private readonly Dictionary<int, D2IEntry<int>> m_entries = new Dictionary<int, D2IEntry<int>>();
        // Entrées indexées par une clé textuelle (ex: "ui.button.ok")
        private readonly Dictionary<string, D2IEntry<string>> m_textEntries = new Dictionary<string, D2IEntry<string>>();

        // Liste des clés triées par ordre alphabétique (pour la recherche rapide)
        private readonly List<int> m_textSortIndexes = new List<int>();
        // Nombre d'entrées textuelles lues
        private int textCount = 0;
        // Chemin vers le fichier .d2i sur le disque
        private readonly string m_uri;

        /// <summary>
        /// Ouvre un fichier D2I existant et le parse en mémoire.
        /// </summary>
        /// <param name="uri">Chemin vers le fichier .d2i.</param>
        public D2IFile(string uri)
        {
            m_uri = uri;
            if (File.Exists(m_uri))
                Initialize();
        }

        /// <summary>
        /// Chemin vers le fichier D2I sur le disque.
        /// </summary>
        public string FilePath
        {
            get { return m_uri; }
        }

        /// <summary>
        /// Lit et parse le contenu du fichier D2I.
        /// Le fichier utilise une structure d'index séparé des données pour un accès aléatoire rapide.
        /// </summary>
        private void Initialize()
        {
            using (var reader = new BigEndianReader(File.ReadAllBytes(m_uri)))
            {
                // Lecture de la position de l'index (stockée au tout début du fichier)
                int indexPos = reader.ReadInt();
                // On se déplace à la position de l'index
                reader.Seek(indexPos, SeekOrigin.Begin);
                // Taille totale de l'index principal en octets
                int indexLen = reader.ReadInt();

                // Lecture de chaque entrée de l'index : chaque entrée fait 9 octets minimum
                for (int i = 0; i < indexLen; i += 9)
                {
                    int key = reader.ReadInt();           // 4 octets : identifiant numérique
                    bool undiacritical = reader.ReadBoolean(); // 1 octet : a-t-on une version sans accents ?
                    int dataPos = reader.ReadInt();        // 4 octets : position des données texte
                    int pos = (int)reader.Position;

                    // On saute jusqu'aux données pour lire le texte
                    reader.Seek(dataPos, SeekOrigin.Begin);
                    var text = reader.ReadUTF();
                    // On revient à notre position dans l'index
                    reader.Seek(pos, SeekOrigin.Begin);

                    if (undiacritical)
                    {
                        // Cette entrée possède aussi une version sans accents ni majuscules (pour la recherche)
                        var criticalPos = reader.ReadInt();
                        i += 4; // L'entrée fait 13 octets au lieu de 9

                        pos = (int)reader.Position;
                        reader.Seek(criticalPos, SeekOrigin.Begin);
                        var undiacriticalText = reader.ReadUTF(); // Texte sans accents
                        reader.Seek(pos, SeekOrigin.Begin);

                        m_entries.Add(key, new D2IEntry<int>(key, text, undiacriticalText));
                    }
                    else
                        m_entries.Add(key, new D2IEntry<int>(key, text));

                }

                // Lecture de l'index des textes UI (clés textuelles comme "ui.ok")
                indexLen = reader.ReadInt();

                while (indexLen > 0)
                {
                    var pos = reader.Position;
                    string key = reader.ReadUTF();       // Clé sous forme de texte
                    int dataPos = reader.ReadInt();       // Position des données
                    indexLen -= ((int)reader.Position - (int)pos); // On décremente du nombre d'octets lus

                    textCount++;
                    pos = (int)reader.Position;
                    reader.Seek(dataPos, SeekOrigin.Begin);
                    m_textEntries.Add(key, new D2IEntry<string>(key, reader.ReadUTF()));
                    reader.Seek(pos, SeekOrigin.Begin);
                }

                // Lecture de l'index de tri (liste d'IDs dans l'ordre alphabétique)
                indexLen = reader.ReadInt();
                while (indexLen > 0)
                {
                    // Ces index de tri servent à la recherche alphabétique côté client
                    m_textSortIndexes.Add(reader.ReadInt());
                    indexLen -= 4;
                }
            }
        }

        /// <summary>
        /// Retourne le texte associé à un identifiant entier.
        /// </summary>
        /// <param name="id">Identifiant numérique du texte.</param>
        /// <returns>Le texte traduit, ou "{null}" si l'identifiant n'existe pas.</returns>
        public string GetText(int id)
        {
            if (m_entries.ContainsKey(id))
                return m_entries[id].Text;
            return "{null}";
        }

        /// <summary>
        /// Retourne le texte associé à une clé textuelle (format "module.clé").
        /// </summary>
        /// <param name="id">Clé textuelle du texte.</param>
        /// <returns>Le texte traduit, ou "{null}" si la clé n'existe pas.</returns>
        public string GetText(string id)
        {
            if (m_textEntries.ContainsKey(id))
                return m_textEntries[id].Text;
            return "{null}";
        }

        /// <summary>
        /// Modifie ou crée une entrée de texte par identifiant numérique.
        /// Calcule automatiquement la version sans accents si le texte contient des caractères accentués.
        /// </summary>
        public void SetText(int id, string value)
        {
            D2IEntry<int> entry;
            if (!m_entries.TryGetValue(id, out entry))
                m_entries.Add(id, entry = new D2IEntry<int>(id, value));
            else
                entry.Text = value;

            // Si le texte contient des accents ou des majuscules, on génère une version normalisée
            // pour faciliter la recherche insensible à la casse
            if (value.HasAccents() || value.Any(char.IsUpper))
            {
                entry.UnDiactricialText = value.RemoveAccents().ToLower();
                entry.UseUndiactricalText = true;
            }
            else
                entry.UseUndiactricalText = false;
        }

        /// <summary>
        /// Modifie ou crée une entrée de texte par clé textuelle.
        /// </summary>
        public void SetText(string id, string value)
        {
            D2IEntry<string> entry;
            if (!m_textEntries.TryGetValue(id, out entry))
                m_textEntries.Add(id, new D2IEntry<string>(id, value));
            else
                entry.Text = value;
        }

        /// <summary>
        /// Supprime une entrée de texte par identifiant numérique.
        /// </summary>
        /// <returns>True si l'entrée existait et a été supprimée.</returns>
        public bool DeleteText(int id)
        {
            return m_entries.Remove(id);
        }

        /// <summary>
        /// Supprime une entrée de texte par clé textuelle.
        /// </summary>
        /// <returns>True si l'entrée existait et a été supprimée.</returns>
        public bool DeleteText(string id)
        {
            return m_textEntries.Remove(id);
        }

        /// <summary>
        /// Retourne toutes les entrées de l'index numérique sous forme de dictionnaire (id -> texte).
        /// </summary>
        public Dictionary<int, string> GetAllText()
        {
            return m_entries.ToDictionary(x => x.Key, x => x.Value.Text);
        }

        /// <summary>
        /// Retourne toutes les entrées de l'index textuel (UI) sous forme de dictionnaire (clé -> texte).
        /// </summary>
        public Dictionary<string, string> GetAllUiText()
        {
            return m_textEntries.ToDictionary(x => x.Key, x => x.Value.Text);
        }

        /// <summary>
        /// Retourne un identifiant numérique libre (supérieur à tous les identifiants existants).
        /// Utile pour ajouter de nouvelles traductions sans conflit.
        /// </summary>
        public int FindFreeId()
        {
            return m_entries.Keys.Max() + 1;
        }

        /// <summary>
        /// Sauvegarde le fichier D2I à son emplacement d'origine.
        /// </summary>
        public void Save()
        {
            Save(m_uri);
        }

        /// <summary>
        /// Sauvegarde le fichier D2I à un chemin spécifié.
        /// La structure du fichier est :
        ///   [int : position de l'index] [données texte] [index principal] [index UI] [index de tri]
        /// Les données texte sont écrites en premier, et l'index est ajouté à la fin (les offsets pointent vers les données).
        /// </summary>
        public void Save(string uri)
        {
            using (var contentWriter = new BigEndianWriter(new StreamWriter(uri).BaseStream))
            {
                var headerWriter = new BigEndianWriter();
                // On réserve 4 octets au début pour écrire la position de l'index plus tard
                contentWriter.Seek(4, SeekOrigin.Begin);

                // === Table 1 : index des textes numériques ===
                foreach (var index in m_entries.Where(x => x.Value.Text != null))
                {
                    headerWriter.WriteInt(index.Key);              // Clé numérique
                    headerWriter.WriteBoolean(index.Value.UseUndiactricalText); // A-t-on une version sans accents ?

                    headerWriter.WriteInt((int)contentWriter.Position); // Position des données dans le flux de contenu
                    contentWriter.WriteUTF(index.Value.Text);      // Écriture du texte principal

                    if (index.Value.UseUndiactricalText)
                    {
                        // Écriture de la version sans accents juste après
                        headerWriter.WriteInt((int)contentWriter.Position);
                        contentWriter.WriteUTF(index.Value.UnDiactricialText);
                    }
                }

                var indexLen = (int)headerWriter.Position;

                // === Table 2 : index des textes UI (clés textuelles) ===
                headerWriter.WriteInt(0); // Placeholder pour la taille de cette table (on l'écrira plus tard)

                foreach (var index in m_textEntries.Where(x => x.Value.Text != null))
                {
                    headerWriter.WriteUTF(index.Key);              // Clé textuelle
                    headerWriter.WriteInt((int)contentWriter.Position);
                    contentWriter.WriteUTF(index.Value.Text);
                }

                var textIndexLen = headerWriter.Position - indexLen - 4;
                var searchTablePos = headerWriter.Position;

                // Retour en arrière pour écrire la vraie taille de la table 2
                headerWriter.Seek(indexLen);
                headerWriter.WriteInt((int)textIndexLen);
                headerWriter.Seek((int)searchTablePos);

                // === Table 3 : index de tri alphabétique ===
                headerWriter.WriteInt(0); // Placeholder pour la taille
                // On trie les entrées alphabétiquement par leur texte pour faciliter la recherche
                var sortedEntries = m_entries.Values.OrderBy(x => x.Text);
                foreach (var entry in sortedEntries)
                {
                    headerWriter.WriteInt(entry.Key);
                }

                var searchTableLen = headerWriter.Position - searchTablePos - 4;
                headerWriter.Seek((int)searchTablePos);
                headerWriter.WriteInt((int)searchTableLen); // Écriture de la vraie taille de la table 3

                var indexPos = (int)contentWriter.Position;

                // Écriture de l'index complet à la fin du fichier de contenu
                byte[] indexData = headerWriter.Data;
                contentWriter.WriteInt(indexLen);
                contentWriter.WriteBytes(indexData);

                // Écriture de la position de l'index tout au début du fichier
                contentWriter.Seek(0, SeekOrigin.Begin);
                contentWriter.WriteInt(indexPos);
            }
        }
    }

    /// <summary>
    /// Entrée générique d'un fichier D2I, paramétrée par le type de clé (int ou string).
    /// Stocke le texte principal et optionnellement une version sans accents pour la recherche.
    /// </summary>
    /// <typeparam name="T">Type de la clé : int pour les textes numériques, string pour les textes UI.</typeparam>
    public class D2IEntry<T>
    {
        /// <summary>
        /// Crée une entrée simple sans version diacritique.
        /// </summary>
        public D2IEntry(T key, string text)
        {
            Key = key;
            Text = text;
        }

        /// <summary>
        /// Crée une entrée avec sa version sans accents (version diacritique).
        /// </summary>
        public D2IEntry(T key, string text, string undiactricalText)
        {
            Key = key;
            Text = text;
            UnDiactricialText = undiactricalText;
            UseUndiactricalText = true; // On signale qu'une version normalisée est disponible
        }

        /// <summary>
        /// Clé de l'entrée (entier ou chaîne de caractères selon le contexte).
        /// </summary>
        public T Key
        {
            get;
            set;
        }

        /// <summary>
        /// Indique si cette entrée possède une version sans accents ni majuscules.
        /// </summary>
        public bool UseUndiactricalText
        {
            get;
            set;
        }

        /// <summary>
        /// Version du texte sans accents et en minuscules, utilisée pour la recherche textuelle.
        /// Exemple : "Épée d'acier" devient "epee d'acier".
        /// </summary>
        public string UnDiactricialText
        {
            get;
            set;
        }

        /// <summary>
        /// Texte principal tel qu'il doit être affiché dans le jeu.
        /// </summary>
        public string Text
        {
            get;
            set;
        }
    }
}
