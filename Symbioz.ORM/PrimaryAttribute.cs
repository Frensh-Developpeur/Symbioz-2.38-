using System;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui marque un champ comme étant la clé primaire de la table SQL.
    /// La clé primaire est l'identifiant unique de chaque ligne dans une table.
    /// Il ne peut y avoir qu'un seul champ [Primary] par classe ITable.
    /// Exemple d'utilisation : [Primary] public int Id;
    /// Le système ORM utilise ce champ pour les requêtes UPDATE et DELETE (WHERE Id = ...).
    /// </summary>
    public class PrimaryAttribute : Attribute
    {
        /// <summary>
        /// Constructeur vide : cet attribut n'a besoin d'aucun paramètre.
        /// Il sert uniquement de marqueur sur le champ clé primaire.
        /// </summary>
        public PrimaryAttribute()
        { }
    }
}
