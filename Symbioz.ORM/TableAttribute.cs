using System;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut C# qui associe une classe à une table SQL dans la base de données.
    /// En C#, les attributs s'écrivent entre crochets au-dessus d'une classe ou d'un champ.
    /// Exemple d'utilisation : [Table("Accounts")] sur la classe AccountRecord.
    /// Le système ORM lit cet attribut pour savoir dans quelle table lire/écrire les données.
    /// </summary>
    public class TableAttribute : Attribute
    {
        /// <summary>Nom de la table SQL correspondante dans la base de données.</summary>
        public string tableName;

        /// <summary>
        /// Si true, la table est chargée automatiquement au démarrage du serveur.
        /// Si false, la table n'est pas chargée automatiquement (utile pour les tables très volumineuses).
        /// </summary>
        public bool catchAll;

        /// <summary>
        /// Ordre de chargement de la table au démarrage.
        /// Certaines tables doivent être chargées avant d'autres (dépendances).
        /// -1 signifie qu'aucun ordre particulier n'est imposé.
        /// </summary>
        public short readingOrder;

        /// <summary>
        /// Constructeur de l'attribut Table.
        /// </summary>
        /// <param name="tableName">Nom exact de la table dans la base de données SQL.</param>
        /// <param name="catchAll">Si true (par défaut), la table est chargée automatiquement au démarrage.</param>
        /// <param name="readingOrder">Priorité de chargement (-1 = pas de priorité spéciale).</param>
        public TableAttribute(string tableName, bool catchAll = true, short readingOrder = -1)
        {
            this.tableName = tableName;
            this.catchAll = catchAll;
            this.readingOrder = readingOrder;
        }
    }
}
