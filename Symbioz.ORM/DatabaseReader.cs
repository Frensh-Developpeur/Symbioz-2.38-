using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Core;

namespace Symbioz.ORM
{
    /// <summary>
    /// Classe générique responsable de lire les données depuis une table SQL et de les convertir en objets C#.
    /// Le paramètre de type T représente le type de la table à lire (ex : DatabaseReader&lt;AccountRecord&gt;).
    /// T doit implémenter l'interface ITable, ce qui garantit qu'il s'agit bien d'une table ORM.
    /// Cette classe utilise la réflexion (System.Reflection) pour lire automatiquement
    /// les champs d'un objet C# et les remplir avec les données SQL correspondantes.
    /// </summary>
    public class DatabaseReader<T>
       where T : ITable
    {
        // FIELDS

        /// <summary>Caractère séparateur utilisé pour les listes stockées en SQL (ex : "1,2,3").</summary>
        private const char LIST_SPLITER = ',';

        /// <summary>Caractère séparateur utilisé pour les dictionnaires stockés en SQL (ex : "1,val1;2,val2").</summary>
        private const char DICTIONARY_SPLITER = ';';

        /// <summary>Liste des objets lus depuis la base de données, après conversion en type T.</summary>
        private List<T> m_elements;

        /// <summary>Lecteur MySQL utilisé pour parcourir les résultats d'une requête SQL.</summary>
        private MySqlDataReader m_reader;

        /// <summary>
        /// Tableau des champs (FieldInfo) de la classe T, excluant les champs [Ignore] et statiques.
        /// L'ordre est important : il doit correspondre à l'ordre des colonnes dans la table SQL.
        /// </summary>
        private FieldInfo[] m_fields;

        /// <summary>Méthodes de la classe T acceptant un string en paramètre (utilisées pour la désérialisation).</summary>
        private MethodInfo[] m_methods;

        /// <summary>Nom de la table SQL, extrait de l'attribut [Table] de la classe T.</summary>
        private string m_tableName;

        // PROPERTIES

        /// <summary>Retourne le nom de la table SQL associée au type T.</summary>
        public string TableName { get { return this.m_tableName; } }

        /// <summary>Retourne la liste des éléments lus depuis la base de données.</summary>
        public List<T> Elements { get { return this.m_elements; } }

        // CONSTRUCTORS

        /// <summary>
        /// Constructeur du DatabaseReader. Initialise la liste d'éléments et appelle Initialize()
        /// pour préparer la lecture (récupération des champs, du nom de table, etc.).
        /// </summary>
        public DatabaseReader()
        {
            this.m_elements = new List<T>();
            this.Initialize();
        }

        // METHODS

        /// <summary>
        /// Initialise le reader en inspectant le type T via la réflexion.
        /// Récupère la liste des champs à lire (en excluant les [Ignore] et les statiques),
        /// les méthodes de désérialisation, et le nom de la table depuis l'attribut [Table].
        /// Lance une exception si l'attribut [Table] est absent sur la classe T.
        /// </summary>
        private void Initialize()
        {
            // Récupère tous les champs publics de T, sauf ceux marqués [Ignore] et les champs statiques.
            // OrderBy(MetadataToken) garantit l'ordre de déclaration dans le code source.
            this.m_fields = typeof(T).GetFields().Where(field =>
                field.GetCustomAttribute(typeof(IgnoreAttribute), false) == null &&
                !field.IsStatic).OrderBy(x => x.MetadataToken).ToArray();

            // Récupère les méthodes statiques de T qui prennent un seul string en paramètre.
            // Ces méthodes sont utilisées pour désérialiser des valeurs complexes depuis SQL.
            this.m_methods = typeof(T).GetMethods().Where(method => method.IsStatic &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == typeof(string)).ToArray();

            // Vérifie que la classe T possède bien l'attribut [Table]. Sinon, exception.
            if (typeof(T).GetCustomAttribute(typeof(TableAttribute)) == null)
                throw new Exception(string.Empty);

            // Récupère le nom de la table depuis l'attribut [Table].
            this.m_tableName = (typeof(T).GetCustomAttribute(typeof(TableAttribute)) as TableAttribute).tableName;
        }

        /// <summary>
        /// Exécute une requête SQL SELECT et remplit la liste m_elements avec les objets créés.
        /// Pour chaque ligne retournée par MySQL, un objet T est instancié via Activator.CreateInstance
        /// avec les valeurs lues comme paramètres du constructeur.
        /// </summary>
        /// <param name="connection">Connexion MySQL active.</param>
        /// <param name="parameter">Requête SQL complète à exécuter (ex : "SELECT * FROM `Accounts` WHERE 1").</param>
        private void ReadTable(MySqlConnection connection, string parameter)
        {
            try
            {
                // Crée et exécute la commande SQL.
                var command = new MySqlCommand(parameter, connection);
                this.m_reader = command.ExecuteReader();

                // Parcourt toutes les lignes retournées par la requête.
                while (this.m_reader.Read())
                {
                    // Crée un tableau d'objets pour stocker les valeurs de cette ligne.
                    var obj = new object[this.m_fields.Length];
                    for (var i = 0; i < this.m_fields.Length; i++)
                        obj[i] = this.m_reader[i]; // Lit la valeur de la colonne i.

                    // Vérifie et convertit les types si nécessaire (ex : string -> List<int>).
                    this.VerifyFieldsType(obj);

                    // Crée une instance de T en passant les valeurs comme arguments du constructeur.
                    T item = (T)Activator.CreateInstance(typeof(T), obj);
                    this.m_elements.Add(item);
                }
                this.m_reader.Close();
            }
            catch (Exception ex)
            {
                Logger.Write<DatabaseReader<T>>(ex.ToString(), ConsoleColor.DarkRed);
                this.m_reader.Close();
            }
        }

        /// <summary>
        /// Recherche et retourne le champ marqué [Primary] dans la classe T.
        /// Il doit y avoir exactement un seul champ [Primary] par table.
        /// Lance une exception si aucun ou plusieurs champs primaires sont trouvés.
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
        /// Lit toutes les lignes de la table (SELECT * FROM table WHERE 1).
        /// "WHERE 1" est une astuce SQL qui retourne toujours vrai, donc lit tout.
        /// </summary>
        /// <param name="connection">Connexion MySQL active.</param>
        public void Read(MySqlConnection connection)
        {
            this.ReadTable(connection, string.Format("SELECT * FROM `{0}` WHERE 1", this.m_tableName));
        }

        /// <summary>
        /// Lit les lignes de la table qui correspondent à une condition SQL donnée.
        /// </summary>
        /// <param name="connection">Connexion MySQL active.</param>
        /// <param name="condition">Condition SQL à placer après WHERE (ex : "Id = 5").</param>
        public void Read(MySqlConnection connection, string condition)
        {
            this.ReadTable(connection, string.Format("SELECT * FROM `{0}` WHERE {1}", this.m_tableName, condition));
        }

        /// <summary>
        /// Retourne le nombre de lignes correspondant à une condition SQL dans la table.
        /// Utilise SELECT COUNT(*) pour compter sans charger toutes les données.
        /// </summary>
        /// <param name="connection">Connexion MySQL active.</param>
        /// <param name="condition">Condition SQL (ex : "AccountId = 3").</param>
        /// <returns>Nombre de lignes correspondantes (long).</returns>
        public long Count(MySqlConnection connection, string condition)
        {
            MySqlCommand cmd = new MySqlCommand(string.Format("SELECT COUNT(*) FROM `{0}` WHERE {1}", this.m_tableName, condition), connection);
            // ExecuteScalar() retourne la première colonne de la première ligne : ici le COUNT.
            return (long)cmd.ExecuteScalar();
        }

        /// <summary>
        /// Vérifie et convertit les valeurs brutes lues depuis MySQL vers les types C# attendus.
        /// MySQL retourne souvent des types simples (string, int), mais les champs C# peuvent être
        /// des types complexes : List&lt;T&gt;, Dictionary&lt;K,V&gt;, objets XML, etc.
        /// Cette méthode gère toutes ces conversions de manière automatique.
        /// </summary>
        /// <param name="obj">Tableau des valeurs brutes lues depuis une ligne SQL.</param>
        private void VerifyFieldsType(object[] obj)
        {
            for (var i = 0; i < this.m_fields.Length; i++)
            {
                // Si le type de la valeur lue correspond déjà au type du champ C#, pas besoin de conversion.
                if (obj[i].GetType() == this.m_fields[i].FieldType)
                    continue;

                XmlAttribute xmlAttribute;
                MethodInfo method = null;

                // Cas des types génériques : List<T> ou Dictionary<K,V>
                if (this.m_fields[i].FieldType.IsGenericType)
                {
                    // Récupère les paramètres de type générique (ex : <int> pour List<int>).
                    var parameters = this.m_fields[i].FieldType.GetGenericArguments();

                    switch (parameters.Length)
                    {
                        case 1: // C'est une List<T> (un seul paramètre générique).

                            // Découpe la chaîne SQL par des virgules (ex : "1,2,3" -> ["1","2","3"]).
                            var elements = (obj[i].ToString()).Split(new char[] { LIST_SPLITER }, StringSplitOptions.RemoveEmptyEntries);

                            // Crée une instance de List<T> dynamiquement via réflexion.
                            var newList = Activator.CreateInstance(typeof(List<>).MakeGenericType(parameters));
                            method = newList.GetType().GetMethod("Add");

                            // Vérifie si le champ est marqué [Xml] : si oui, on désérialise depuis du XML.
                            xmlAttribute = this.m_fields[i].GetCustomAttribute<XmlAttribute>();
                            if (xmlAttribute != null)
                            {
                                var xmlObject = obj[i].ToString().XMLDeserialize(this.m_fields[i].FieldType);
                                obj[i] = Convert.ChangeType(xmlObject, this.m_fields[i].FieldType);
                                continue;
                            }

                            // Vérifie si le type des éléments de la liste possède une méthode Deserialize.
                            var desezializeMethod = parameters[0].GetMethod("Deserialize");
                            if (desezializeMethod != null)
                            {
                                // Utilise la méthode Deserialize pour convertir chaque élément de la liste.
                                elements = (obj[i] as string).Split(new char[] { LIST_SPLITER }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var element in elements)
                                    method.Invoke(newList, new object[] { desezializeMethod.Invoke(null, new object[] { element }) });
                            }
                            else
                            {
                                // Sinon, conversion classique via Convert.ChangeType (ex : string -> int).
                                foreach (var element in elements)
                                    method.Invoke(newList, new object[] { Convert.ChangeType(element, parameters[0]) });
                            }

                            obj[i] = newList;
                            continue;

                        case 2: // C'est un Dictionary<K,V> (deux paramètres génériques).
                            // Découpe la chaîne SQL par des points-virgules (ex : "1,val1;2,val2").
                            elements = (obj[i] as string).Split(new char[] { DICTIONARY_SPLITER }, StringSplitOptions.RemoveEmptyEntries);

                            // Crée une instance de Dictionary<K,V> dynamiquement.
                            var newDictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(parameters));
                            method = newDictionary.GetType().GetMethod("Add");

                            foreach (var pair in elements)
                            {
                                // Chaque paire est "clé,valeur" (séparée par une virgule).
                                string[] split = pair.Split(',');
                                object key = Convert.ChangeType(split[0], parameters[0]);
                                object value = Convert.ChangeType(split[1], parameters[1]);
                                method.Invoke(newDictionary, new object[2] { key, value });
                            }
                            obj[i] = newDictionary;
                            continue;
                    }
                }

                // Vérifie si le champ est marqué [Xml] pour une désérialisation XML simple (hors Liste).
                xmlAttribute = this.m_fields[i].GetCustomAttribute<XmlAttribute>();
                if (xmlAttribute != null)
                {
                    var xmlObject = obj[i].ToString().XMLDeserialize(this.m_fields[i].FieldType);
                    obj[i] = Convert.ChangeType(xmlObject, this.m_fields[i].FieldType);
                    continue;
                }

                // Vérifie si le type possède une méthode statique Deserialize(string) personnalisée.
                method = this.m_fields[i].FieldType.GetMethod("Deserialize");
                if (method != null)
                {
                    obj[i] = method.Invoke(null, new object[] { obj[i] });
                    continue;
                }

                // Dernier recours : conversion standard via Convert.ChangeType (ex : long -> int).
                try { obj[i] = Convert.ChangeType(obj[i], this.m_fields[i].FieldType); }
                catch
                {
                    string exception = string.Format("Unknown constructor for '{0}', ({1}) if its an XmlField, FieldType must got empty constructor.", this.m_fields[i].FieldType.Name, this.m_fields[i].Name);
                    Console.WriteLine(exception);
                    throw new Exception(exception);
                }
            }
        }

        /// <summary>
        /// Méthode statique pratique pour lire des éléments avec une condition SQL,
        /// en utilisant automatiquement la connexion gérée par DatabaseManager.
        /// </summary>
        /// <param name="condition">Condition SQL (ex : "Username = 'Jean'").</param>
        /// <returns>Liste des objets T correspondant à la condition.</returns>
        public static List<T> Read(string condition)
        {
            DatabaseReader<T> reader = new DatabaseReader<T>();
            // UseProvider() retourne la connexion MySQL active (gérée par DatabaseManager).
            reader.ReadTable(DatabaseManager.GetInstance().UseProvider(), string.Format("SELECT * FROM `{0}` WHERE {1}", reader.m_tableName, condition));
            return reader.Elements;
        }

        /// <summary>
        /// Lit et retourne le premier élément correspondant à la condition SQL donnée.
        /// Retourne la valeur par défaut du type T (null pour les classes) si aucun élément n'est trouvé.
        /// </summary>
        /// <param name="condition">Condition SQL (ex : "Id = 42").</param>
        /// <returns>Premier élément trouvé, ou default(T) si aucun résultat.</returns>
        public static T ReadFirst(string condition)
        {
            List<T> elements = Read(condition);
            if (elements.Count > 0)
                return elements.First();
            else
                return default(T); // Retourne null pour les types référence.
        }

        /// <summary>
        /// Compte le nombre de lignes correspondant à une condition SQL, sans charger les données.
        /// Version statique utilisant la connexion gérée par DatabaseManager.
        /// </summary>
        /// <param name="condition">Condition SQL (ex : "AccountId = 5").</param>
        /// <returns>Nombre de lignes (long).</returns>
        public static long Count(string condition)
        {
            return new DatabaseReader<T>().Count(DatabaseManager.GetInstance().UseProvider(), condition);
        }
    }
}
