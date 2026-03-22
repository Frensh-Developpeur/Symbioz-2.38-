using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using Symbioz;
using Symbioz.Core;
using System.Threading;

namespace Symbioz.ORM
{
    /// <summary>
    /// Gestionnaire principal de la base de données MySQL.
    /// Fonctionne comme un ORM (Object-Relational Mapping) simplifié :
    /// il lit automatiquement les données MySQL et les charge en mémoire dans les "Records".
    ///
    /// Concepts clés :
    ///   - ITable      : interface que doit implémenter chaque classe de données (Record)
    ///   - [Table]     : attribut qui indique le nom de la table MySQL correspondante
    ///   - [Primary]   : attribut qui marque le champ clé primaire
    ///   - [Ignore]    : attribut pour ne pas sauvegarder un champ en BDD
    ///   - DatabaseReader&lt;T&gt; : lit les données d'une table MySQL et retourne une liste
    ///   - DatabaseWriter&lt;T&gt; : écrit/sauvegarde les données en MySQL
    /// </summary>
    public class DatabaseManager
    {
        Logger logger = new Logger();

        // Template SQL pour créer une table si elle n'existe pas encore
        private const string CREATE_TABLE = "CREATE TABLE if not exists {0} ({1})";

        // Instance singleton (accessible via GetInstance())
        private static DatabaseManager _self;

        // La connexion MySQL active
        internal MySqlConnection m_provider;

        // Liste des méthodes [RemoveWhereId] à appeler lors de la suppression d'un élément par ID
        private List<MethodInfo> m_removeMethods = new List<MethodInfo>();

        // Assembly contenant tous les Records (classes de données, ex: CharacterRecord, ItemRecord)
        public Assembly RecordsAssembly;

        /// <summary>
        /// Constructeur principal : crée la connexion MySQL et prépare le chargement des tables.
        /// </summary>
        public DatabaseManager(Assembly recordsAssembly, string host, string database, string user, string password)
        {
            if (_self == null)
                _self = this;

            // Chaîne de connexion MySQL standard
            this.m_provider = new MySqlConnection(string.Format("Server={0};UserId={1};Password={2};Database={3}", host, user, password, database));
            this.RecordsAssembly = recordsAssembly;
            this.LoadRemoveMethods();
        }

        /// <summary>
        /// Constructeur avec port personnalisé (utile si MySQL n'est pas sur le port 3306 par défaut)
        /// </summary>
        public DatabaseManager(Assembly recordsAssembly, string host, string port, string database, string user, string password)
        {
            if (_self == null)
                _self = this;

            this.m_provider = new MySqlConnection(string.Format("Server={0};Port={1};UserId={2};Password={3};Database={4}", host, port, user, password, database));
            this.RecordsAssembly = recordsAssembly;
            this.LoadRemoveMethods();
        }

        /// <summary>
        /// Retourne la connexion MySQL active.
        /// Si elle est déconnectée (Ping() échoue), la rouvre automatiquement.
        /// </summary>
        public MySqlConnection UseProvider()
        {
            return UseProvider(m_provider);
        }

        private MySqlConnection UseProvider(MySqlConnection connection)
        {
            if (!connection.Ping()) // Ping = vérifie si la connexion est encore active
            {
                connection.Close();
                connection.Open(); // Reconnexion automatique
            }

            return connection;
        }

        /// <summary>
        /// Pré-charge les méthodes [RemoveWhereId] des tables [Resettable].
        /// Ces méthodes servent à supprimer les entrées liées à un ID donné (ex: déconnexion joueur).
        /// </summary>
        private void LoadRemoveMethods()
        {
            var tablesTypes = RecordsAssembly.GetTypes().Where(x => x.GetInterface("ITable") != null).Where(x => x.GetCustomAttribute(typeof(ResettableAttribute)) != null);

            foreach (var table in tablesTypes)
            {
                var tableName = table.GetCustomAttribute<TableAttribute>().tableName;
                var method = table.GetMethods().FirstOrDefault(x => x.GetCustomAttribute<RemoveWhereIdAttribute>() != null);

                if (method != null)
                {
                    m_removeMethods.Add(method);
                }
            }
        }

        /// <summary>
        /// Charge toutes les tables en mémoire au démarrage du serveur.
        /// Pour chaque classe implémentant ITable :
        ///   1. Lit toutes les lignes de la table MySQL correspondante
        ///   2. Les stocke dans le champ statique List&lt;T&gt; de la classe Record
        ///
        /// Respecte l'ordre de chargement défini par [Table(readingOrder=X)]
        /// (nécessaire quand certaines tables dépendent d'autres).
        /// </summary>
        public void LoadTables()
        {
            var tables = RecordsAssembly.GetTypes().Where(x => x.GetInterface("ITable") != null).ToArray();
            var orderedTables = new Type[tables.Length]; // Tables dans l'ordre de chargement
            var dontCatch = new List<Type>();            // Tables à ne pas charger automatiquement

            // Étape 1 : classe les tables selon leur readingOrder défini dans [Table]
            foreach (var table in tables)
            {
                var attribute = (TableAttribute)table.GetCustomAttribute(typeof(TableAttribute), false);
                if (attribute == null)
                {
                    logger.Color2(string.Format("Warning : the table type '{0}' hasn't got an attribute called 'TableAttribute'", table.Name));
                    continue;
                }

                if (attribute.catchAll)
                {
                    if (attribute.readingOrder >= 0)
                        orderedTables[attribute.readingOrder] = table; // Place à la position définie
                }
                else
                    dontCatch.Add(table); // Cette table ne sera pas chargée automatiquement
            }

            // Étape 2 : place les tables sans ordre défini à la fin du tableau
            foreach (var table in tables)
            {
                if (orderedTables.Contains(table) || dontCatch.Contains(table))
                    continue;

                for (var i = tables.Length - 1; i >= 0; i--)
                {
                    if (orderedTables[i] == null)
                    {
                        orderedTables[i] = table;
                        break;
                    }
                }
            }

            // Étape 3 : charge chaque table dans la mémoire
            foreach (var type in orderedTables)
            {
                if (type == null)
                    continue;

                // Crée un DatabaseReader<T> générique pour ce type de Record
                var reader = Activator.CreateInstance(typeof(DatabaseReader<>).MakeGenericType(type));
                var tableName = (string)reader.GetType().GetProperty("TableName").GetValue(reader);

                logger.Gray("Loading " + tableName + " ...");

                // Lit toutes les lignes de la table MySQL
                var method = reader.GetType().GetMethods().FirstOrDefault(x => x.Name == "Read" && x.GetParameters().Length == 1);
                method.Invoke(reader, new object[] { this.UseProvider() });

                // Récupère les éléments lus (liste de Records)
                var elements = reader.GetType().GetProperty("Elements").GetValue(reader);

                // Cherche le champ statique List<T> dans la classe Record et y ajoute les éléments
                var field = type.GetFields().FirstOrDefault(x => x.IsStatic && x.FieldType.IsGenericType && x.FieldType.GetGenericArguments()[0] == type);
                if (field != null)
                {
                    field.FieldType.GetMethod("AddRange").Invoke(field.GetValue(null), new object[] { elements });
                }
            }
        }

        // Ferme la connexion MySQL (appeler à l'arrêt du serveur)
        public void CloseProvider()
        {
            this.m_provider.Close();
        }

        // Retourne l'instance unique du DatabaseManager (pattern Singleton manuel)
        public static DatabaseManager GetInstance()
        {
            return _self;
        }

        /// <summary>
        /// Vide (DELETE *) toutes les tables marquées [Resettable] dans l'assembly donné.
        /// Utilisé pour réinitialiser les données temporaires (ex: combats en cours) au redémarrage.
        /// </summary>
        public void ResetTables(Assembly assembly)
        {
            var tables = assembly.GetTypes().Where(x => x.GetInterface("ITable") != null).Where(x => x.GetCustomAttribute(typeof(ResettableAttribute)) != null);

            foreach (var table in tables)
            {
                TableAttribute attribute = table.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;
                Delete(attribute.tableName);
            }
        }

        // Supprime toutes les lignes d'une table MySQL (DELETE FROM tableName)
        private void Delete(string tableName)
        {
            Query(string.Format("DELETE from {0}", tableName), UseProvider());
        }

        /// <summary>
        /// Recharge une table spécifique depuis la BDD (sans redémarrer le serveur).
        /// Utile pour rafraîchir des données qui ont changé en base pendant le jeu.
        /// </summary>
        public void Reload<T>() where T : ITable
        {
            DatabaseReader<T> reader = new DatabaseReader<T>();
            reader.Read(UseProvider());
            FieldInfo field = SaveTask.GetCache(typeof(T));
            field.FieldType.GetMethod("Clear").Invoke(field.GetValue(null), null); // Vide la liste en mémoire
            field.SetValue(null, reader.Elements);                                  // Remplace par les nouvelles données
        }

        /// <summary>
        /// Exécute une requête SQL qui ne retourne pas de données (INSERT, UPDATE, DELETE).
        /// Logue l'erreur si la requête échoue, sans planter le serveur.
        /// </summary>
        public void Query(string query, MySqlConnection connection)
        {
            MySqlCommand cmd = new MySqlCommand(query, connection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                logger.Error("Unable to query (" + query + ")");
            }
        }

        /// <summary>
        /// Effectue une sauvegarde complète de la base de données vers un fichier SQL.
        /// Utilise MySqlBackup.NET pour exporter toutes les tables.
        /// </summary>
        public void Backup(string fileName)
        {
            MySqlConnection connection = (MySqlConnection)UseProvider().Clone();

            using (MySqlCommand cmd = new MySqlCommand())
            {
                using (MySqlBackup mb = new MySqlBackup(cmd))
                {
                    cmd.Connection = connection;
                    connection.Open();
                    mb.ExportToFile(fileName); // Génère un fichier .sql complet
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Supprime tous les enregistrements liés à un ID donné dans toutes les tables [Resettable].
        /// Ex: quand un personnage se déconnecte, supprime ses combats, groupes, etc. en BDD
        /// ET retire les objets correspondants des listes en mémoire.
        /// </summary>
        public void RemoveWhereIdMethod(long fieldValue)
        {
            Dictionary<Type, List<ITable>> tableTypes = new Dictionary<Type, List<ITable>>();

            foreach (var method in m_removeMethods)
            {
                // Appelle la méthode [RemoveWhereId] de chaque table pour trouver les éléments à supprimer
                var list = (method.Invoke(null, new object[] { fieldValue }) as IList).Cast<ITable>().ToList();
                tableTypes.Add(method.DeclaringType, list);
            }

            foreach (var key in tableTypes.Keys)
            {
                if (tableTypes[key].Count > 0)
                    tableTypes[key].RemoveInstantElements(key); // Supprime en BDD et en mémoire
                logger.White("Removed from " + key.Name);
            }
        }

        /// <summary>
        /// Crée une table MySQL depuis un type C# (si elle n'existe pas déjà).
        /// Génère automatiquement les colonnes à partir des champs publics du type,
        /// en ignorant ceux marqués [Ignore] et ceux qui sont statiques.
        /// Le champ marqué [Primary] devient la clé primaire (INT).
        /// </summary>
        public void CreateTable(Type type)
        {
            string tableName = type.GetCustomAttribute<TableAttribute>().tableName;
            FieldInfo primaryField = type.GetFields().FirstOrDefault(x => x.GetCustomAttribute<PrimaryAttribute>() != null);

            string str = string.Empty;

            foreach (var field in type.GetFields().ToList().FindAll(x => x.GetCustomAttribute<IgnoreAttribute>() == null).FindAll(x => !x.IsStatic))
            {
                string fieldType = "mediumtext"; // Type SQL par défaut pour tous les champs

                if (primaryField == field)
                {
                    fieldType = "int (40)"; // La clé primaire est un entier
                }
                str += field.Name + " " + fieldType + ",";
            }

            if (primaryField != null)
                str += "PRIMARY KEY (" + primaryField.Name + ")";
            else
                str = str.Remove(str.Length - 1, 1); // Retire la dernière virgule si pas de PK

            this.Query(string.Format(CREATE_TABLE, tableName, str), UseProvider());
        }

        // Surcharge : crée la table depuis une instance (plutôt que depuis le Type directement)
        public void CreateTable(ITable table)
        {
            CreateTable(table.GetType());
        }

        /// <summary>
        /// Crée un DatabaseWriter générique pour sauvegarder des éléments en BDD.
        /// L'action peut être Insert, Update ou Delete selon DatabaseAction.
        /// </summary>
        public void WriterInstance(Type type, DatabaseAction action, ITable[] elements)
        {
            Activator.CreateInstance(typeof(DatabaseWriter<>).MakeGenericType(type), action, elements);
        }

    }
}
