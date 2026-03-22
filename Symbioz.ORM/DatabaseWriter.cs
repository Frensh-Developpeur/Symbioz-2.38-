using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using Symbioz.Core;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Symbioz.ORM
{
    /// <summary>
    /// Classe générique responsable d'écrire, mettre à jour et supprimer des données dans une table SQL.
    /// Comme DatabaseReader&lt;T&gt;, elle utilise la réflexion pour construire automatiquement
    /// les requêtes SQL (INSERT, UPDATE, DELETE) à partir des champs de la classe T.
    /// T doit implémenter ITable et posséder un champ marqué [Primary].
    /// </summary>
    public class DatabaseWriter<T>
        where T : ITable
    {
        // FIELDS

        /// <summary>
        /// Nombre maximum d'éléments insérés en une seule requête INSERT.
        /// MySQL accepte plusieurs lignes dans un seul INSERT (INSERT INTO ... VALUES (...), (...), ...).
        /// On limite à 250 pour éviter des requêtes trop longues.
        /// </summary>
        private const short MAX_ADDING_LINES = 250;

        /// <summary>Template SQL pour l'insertion de plusieurs lignes en une seule requête.</summary>
        private const string ADD_ELEMENTS = "INSERT INTO `{0}` VALUES\n{1}";

        /// <summary>Template SQL pour la mise à jour d'une ligne identifiée par sa clé primaire.</summary>
        private const string UPDATE_ELEMENTS = "UPDATE `{0}` SET {1} WHERE `{2}` = {3}";

        /// <summary>Template SQL pour la suppression d'une ligne identifiée par sa clé primaire.</summary>
        private const string REMOVE_ELEMENTS = "DELETE FROM `{0}` WHERE `{1}` = {2}";

        /// <summary>Séparateur pour sérialiser les listes en SQL (ex : List&lt;int&gt; -> "1,2,3").</summary>
        private const string LIST_SPLITTER = ",";

        /// <summary>Séparateur pour sérialiser les dictionnaires en SQL (ex : "1,val1;2,val2").</summary>
        private const string DICTIONARY_SPLITTER = ";";

        /// <summary>Nom de la table SQL, extrait de l'attribut [Table].</summary>
        private string m_tableName;

        /// <summary>Commande MySQL courante (réutilisée pour chaque opération).</summary>
        private MySqlCommand m_command;

        /// <summary>Champs à inclure selon l'opération (Add = tous les champs, Update = champs [Update] seulement).</summary>
        private List<FieldInfo> m_fields;

        /// <summary>Méthodes de sérialisation marquées [Translation] en mode écriture.</summary>
        private List<MethodInfo> m_methods;

        // CONSTRUCTORS

        /// <summary>
        /// Constructeur principal : prépare le writer et exécute l'opération demandée (Add/Update/Remove).
        /// </summary>
        /// <param name="action">Type d'opération SQL : Add (INSERT), Update (UPDATE), ou Remove (DELETE).</param>
        /// <param name="elements">Tableau des éléments ITable à traiter.</param>
        public DatabaseWriter(DatabaseAction action, params ITable[] elements)
        {
            this.Initialize(action);

            switch (action)
            {
                case DatabaseAction.Add:
                    this.AddElements(elements);
                    return;

                case DatabaseAction.Update:
                    this.UpdateElements(elements);
                    return;

                case DatabaseAction.Remove:
                    this.DeleteElements(elements);
                    return;
            }
        }

        /// <summary>
        /// Initialise le writer selon l'action : récupère les champs appropriés,
        /// les méthodes de sérialisation, et le nom de la table.
        /// </summary>
        /// <param name="action">Type d'opération SQL.</param>
        private void Initialize(DatabaseAction action)
        {
            // Pour un INSERT, on prend tous les champs (sauf [Ignore]).
            if (action == DatabaseAction.Add)
                this.m_fields = GetAddFields(typeof(T));

            // Pour un UPDATE, on ne prend que les champs marqués [Update].
            if (action == DatabaseAction.Update)
                this.m_fields = GetUpdateFields(typeof(T));

            // Récupère les méthodes de sérialisation (marquées [Translation] en mode écriture).
            this.m_methods = typeof(T).GetMethods().Where(method => method.GetCustomAttribute(typeof(TranslationAttribute), false) != null &&
                !(method.GetCustomAttribute(typeof(TranslationAttribute), false) as TranslationAttribute).readingMode).ToList();

            // Récupère le nom de la table depuis l'attribut [Table].
            this.m_tableName = (typeof(T).GetCustomAttribute(typeof(TableAttribute)) as TableAttribute).tableName;

            // Pour les opérations non-Add, on vérifie qu'une clé primaire existe (nécessaire pour WHERE).
            if (action != DatabaseAction.Add)
                this.GetPrimaryField();
        }

        /// <summary>
        /// Construit et exécute les requêtes INSERT pour ajouter plusieurs éléments en base de données.
        /// Les éléments sont regroupés par lots de MAX_ADDING_LINES pour optimiser les performances.
        /// </summary>
        /// <param name="elements">Éléments à insérer.</param>
        private void AddElements(ITable[] elements)
        {
            var values = new List<string>();
            var str = string.Empty;

            // Découpage en lots de MAX_ADDING_LINES éléments.
            for (var i = 0; i < elements.Length / MAX_ADDING_LINES + 1; i++)
            {
                str = string.Empty;

                for (var j = i * MAX_ADDING_LINES; j < (i + 1) * MAX_ADDING_LINES; j++)
                {
                    // Ajoute une virgule entre les lignes VALUES.
                    if (str != string.Empty && elements.Length > j)
                        str += ",\n";

                    if (elements.Length <= j)
                        break;

                    // Crée la représentation SQL d'une ligne : (val1, val2, val3, ...).
                    str += string.Format("({0})", this.CreateElement(elements[j]));
                }

                if (str != string.Empty)
                {
                    // Ajoute un point-virgule pour terminer la requête SQL.
                    values.Add(string.Format("{0};", str));
                }
            }

            // Exécute chaque lot d'INSERT.
            foreach (var element in values)
            {
                var command = string.Format(ADD_ELEMENTS, this.m_tableName, element);
                this.m_command = new MySqlCommand(command, DatabaseManager.GetInstance().UseProvider());
                try
                {
                    this.m_command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Write("Unable to add element to database (" + m_tableName + ") " +
                        ex.Message, ConsoleColor.DarkRed);
                }
            }
        }

        /// <summary>
        /// Construit et exécute les requêtes UPDATE pour chaque élément fourni.
        /// Seuls les champs marqués [Update] sont inclus dans la requête SET.
        /// Le verrou (lock) garantit qu'aucune autre opération ne modifie l'élément pendant l'UPDATE.
        /// </summary>
        /// <param name="elements">Éléments à mettre à jour.</param>
        private void UpdateElements(ITable[] elements)
        {
            foreach (var element in elements)
            {
                lock (element) // Verrou pour éviter les modifications concurrentes.
                {
                    // Construit la partie SET de la requête : "champ1 = val1, champ2 = val2, ..."
                    var values = this.m_fields.ConvertAll<string>(field => string.Format("{0} = {1}", field.Name, this.GetFieldValue(field, element)));
                    var command = string.Format(UPDATE_ELEMENTS, this.m_tableName, string.Join(", ", values), this.GetPrimaryField().Name, this.GetPrimaryField().GetValue(element));

                    this.m_command = new MySqlCommand(command, DatabaseManager.GetInstance().UseProvider());
                    this.m_command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Construit et exécute les requêtes DELETE pour chaque élément fourni.
        /// Utilise la clé primaire ([Primary]) pour cibler la ligne à supprimer (WHERE Id = valeur).
        /// </summary>
        /// <param name="elements">Éléments à supprimer.</param>
        private void DeleteElements(ITable[] elements)
        {
            foreach (var element in elements)
            {
                lock (element) // Verrou pour éviter les suppressions concurrentes.
                {
                    var command = string.Format(REMOVE_ELEMENTS, this.m_tableName, this.GetPrimaryField().Name, this.GetPrimaryField().GetValue(element));
                    this.m_command = new MySqlCommand(command, DatabaseManager.GetInstance().UseProvider());
                    this.m_command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Construit la représentation SQL d'un élément pour une requête INSERT.
        /// Retourne une chaîne "val1, val2, val3, ..." correspondant aux colonnes de la table.
        /// </summary>
        /// <param name="element">L'objet ITable à sérialiser en SQL.</param>
        /// <returns>Chaîne de valeurs séparées par des virgules.</returns>
        private string CreateElement(ITable element)
        {
            // Convertit chaque champ en sa représentation SQL (entourée de guillemets simples).
            var values = this.m_fields.ConvertAll<string>(field => this.GetFieldValue(field, element));
            return string.Join(", ", values);
        }

        /// <summary>
        /// Retourne la valeur SQL d'un champ pour un élément donné.
        /// Gère tous les cas de conversion : DateTime, types avec Translation, XML, List, Dictionary, etc.
        /// La valeur retournée est toujours une chaîne entourée de guillemets simples SQL : 'valeur'.
        /// </summary>
        /// <param name="field">Informations sur le champ à sérialiser.</param>
        /// <param name="element">L'objet contenant le champ.</param>
        /// <returns>Valeur SQL formatée (ex : "'Jean'", "'42'", "'2024-01-15 12:00:00'").</returns>
        private string GetFieldValue(FieldInfo field, ITable element)
        {
            var value = field.GetValue(element);

            if (field.GetCustomAttribute(typeof(TranslationAttribute), false) != null)
            {
                // Champ avec [Translation] : appelle ToString() pour sérialiser.
                var method = field.FieldType.GetMethod("ToString");
                value = method.Invoke(field.GetValue(element), new object[] { });
            }
            else if (field.FieldType == typeof(DateTime))
            {
                // Les DateTime sont formatées en ISO 8601 pour MySQL.
                value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (this.m_methods.FirstOrDefault(x => x.IsStatic && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == field.FieldType) != null)
            {
                // Il existe une méthode statique de sérialisation pour ce type.
                var method = this.m_methods.FirstOrDefault(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == field.FieldType);
                value = method.Invoke(null, new object[] { field.GetValue(element) });
            }
            else if (field.FieldType.GetMethod("Deserialize") != null)
            {
                // Le type possède sa propre méthode de sérialisation/désérialisation.
                value = field.FieldType.GetMethod("ToString").Invoke(field.GetValue(element), new object[] { });
            }
            else if (field.GetCustomAttribute(typeof(XmlAttribute)) != null)
            {
                // Champ XML : sérialisation via XMLSerialize().
                value = field.GetValue(element).XMLSerialize();
            }
            else
            {
                // Types génériques (List ou Dictionary) : sérialisation en chaîne de caractères.
                if (field.FieldType.IsGenericType)
                {
                    var arguments = field.FieldType.GetGenericArguments();

                    switch (arguments.Length)
                    {
                        case 1: // List<T> : on joint les éléments avec LIST_SPLITTER (",").
                            var values = (IList)field.GetValue(element);

                            if (field.GetCustomAttribute(typeof(XmlAttribute)) != null)
                            {
                                // Liste d'objets XML : chaque élément est sérialisé en XML.
                                var newValues = new List<string>();
                                foreach (var ele in values)
                                    newValues.Add(ele.XMLSerialize());
                                value = string.Join(LIST_SPLITTER, newValues);
                                break;
                            }

                            if (arguments[0].GetMethod("Deserialize") != null)
                            {
                                // Éléments avec méthode Deserialize : utilise ToString() pour sérialiser.
                                var method = arguments[0].GetMethod("ToString");
                                var newValues = new List<string>();
                                foreach (var ele in values)
                                    newValues.Add((string)method.Invoke(ele, new object[] { }));
                                value = string.Join(LIST_SPLITTER, newValues);
                                break;
                            }

                            // Cas simple : jointure directe des éléments en chaîne.
                            value = "";
                            var array = new List<string>();
                            foreach (var ele in values)
                                array.Add(ele.ToString());
                            value = string.Join(LIST_SPLITTER, array);
                            break;

                        case 2: // Dictionary<K,V> : chaque paire "clé,valeur" séparée par DICTIONARY_SPLITTER (";").
                            var list = new List<string>();
                            IDictionary dic = (IDictionary)value;
                            foreach (DictionaryEntry entry in dic)
                                list.Add(string.Format("{0},{1}", entry.Key, entry.Value));
                            value = string.Join(DICTIONARY_SPLITTER, list);
                            break;
                    }
                }
            }

            // Échappe les guillemets simples (apostrophes) pour éviter les erreurs SQL (injection).
            value = value.ToString().Replace("'", "''");
            // Entoure la valeur de guillemets simples pour la syntaxe SQL.
            return string.Format("'{0}'", value);
        }

        /// <summary>
        /// Recherche et retourne le champ marqué [Primary] dans la classe T.
        /// Nécessaire pour les requêtes UPDATE et DELETE (clause WHERE).
        /// Lance une exception s'il y a zéro ou plusieurs champs primaires.
        /// </summary>
        private FieldInfo GetPrimaryField()
        {
            var fields = typeof(T).GetFields().Where(field => field.GetCustomAttribute(typeof(PrimaryAttribute), false) != null);

            if (fields.Count() != 1)
            {
                if (fields.Count() == 0)
                    throw new Exception(string.Format("The Table '{0}' hasn't got a primary field", typeof(T).FullName));

                if (fields.Count() > 1)
                    throw new Exception(string.Format("The Table '{0}' has too much primary fields", typeof(T).FullName));
            }
            return fields.First();
        }

        /// <summary>
        /// Retourne la liste des champs à inclure dans une requête UPDATE.
        /// Seuls les champs non-statiques, non-[Ignore] et marqués [Update] sont inclus.
        /// </summary>
        /// <param name="type">Type de la classe table à inspecter.</param>
        public static List<FieldInfo> GetUpdateFields(Type type)
        {
            return type.GetFields().Where(field =>
                !field.IsStatic &&
                field.GetCustomAttribute(typeof(IgnoreAttribute), false) == null &&
                field.GetCustomAttribute(typeof(UpdateAttribute), false) != null)
                .OrderBy(x => x.MetadataToken).ToList();
        }

        /// <summary>
        /// Retourne la liste des champs à inclure dans une requête INSERT.
        /// Tous les champs non-statiques et non-[Ignore] sont inclus.
        /// </summary>
        /// <param name="type">Type de la classe table à inspecter.</param>
        public static List<FieldInfo> GetAddFields(Type type)
        {
            return type.GetFields().Where(field =>
                !field.IsStatic &&
                field.GetCustomAttribute(typeof(IgnoreAttribute), false) == null)
                .OrderBy(x => x.MetadataToken).ToList();
        }

        /// <summary>
        /// Met à jour un seul élément immédiatement en base de données (sans passer par le SaveTask).
        /// Utile pour les mises à jour urgentes qui ne peuvent pas attendre le prochain cycle de sauvegarde.
        /// </summary>
        /// <param name="item">L'élément T à mettre à jour.</param>
        public static void InstantUpdate(T item)
        {
            new DatabaseWriter<T>(DatabaseAction.Update, new ITable[] { item });
        }

        /// <summary>
        /// Met à jour plusieurs éléments immédiatement en base de données.
        /// </summary>
        /// <param name="items">Énumération des éléments à mettre à jour.</param>
        public static void InstantUpdate(IEnumerable<T> items)
        {
            new DatabaseWriter<T>(DatabaseAction.Update, items.ToArray() as ITable[]);
        }

        /// <summary>
        /// Insère un seul élément immédiatement en base de données.
        /// </summary>
        /// <param name="item">L'élément T à insérer.</param>
        public static void InstantInsert(T item)
        {
            new DatabaseWriter<T>(DatabaseAction.Add, new ITable[] { item });
        }

        /// <summary>
        /// Insère plusieurs éléments immédiatement en base de données.
        /// </summary>
        /// <param name="items">Énumération des éléments à insérer.</param>
        public static void InstantInsert(IEnumerable<T> items)
        {
            new DatabaseWriter<T>(DatabaseAction.Add, items.ToArray() as ITable[]);
        }

        /// <summary>
        /// Supprime un seul élément immédiatement de la base de données.
        /// </summary>
        /// <param name="item">L'élément T à supprimer.</param>
        public static void InstantRemove(T item)
        {
            new DatabaseWriter<T>(DatabaseAction.Remove, new ITable[] { item });
        }

        /// <summary>
        /// Supprime plusieurs éléments immédiatement de la base de données.
        /// </summary>
        /// <param name="items">Énumération des éléments à supprimer.</param>
        public static void InstantRemove(IEnumerable<T> items)
        {
            new DatabaseWriter<T>(DatabaseAction.Remove, items.ToArray() as ITable[]);
        }

        /// <summary>
        /// Crée la table SQL correspondant au type T si elle n'existe pas déjà.
        /// Délègue la création au DatabaseManager.
        /// </summary>
        public static void CreateTable()
        {
            DatabaseManager.GetInstance().CreateTable(typeof(T));
        }
    }

    /// <summary>
    /// Enumération des types d'opérations SQL disponibles pour le DatabaseWriter.
    /// Add = INSERT INTO, Update = UPDATE SET, Remove = DELETE FROM.
    /// </summary>
    public enum DatabaseAction
    {
        /// <summary>Insère un nouvel enregistrement dans la table (INSERT INTO).</summary>
        Add,

        /// <summary>Met à jour un enregistrement existant (UPDATE SET ... WHERE).</summary>
        Update,

        /// <summary>Supprime un enregistrement de la table (DELETE FROM ... WHERE).</summary>
        Remove
    }
}
