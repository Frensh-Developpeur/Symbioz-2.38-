using System;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui indique qu'un champ doit être inclus dans les requêtes UPDATE.
    /// Par défaut, lors d'un UPDATE, seuls les champs marqués [Update] sont mis à jour en base de données.
    /// Cela permet d'éviter de réécrire des champs immuables (comme l'identifiant ou le mot de passe)
    /// à chaque mise à jour, ce qui est plus efficace et plus sûr.
    /// Exemple : [Update] public bool Banned; — le champ Banned sera mis à jour lors d'un UpdateElement().
    /// </summary>
    public class UpdateAttribute : Attribute
    {
        /// <summary>
        /// Constructeur vide : la simple présence de [Update] sur un champ suffit à l'inclure dans les UPDATE.
        /// </summary>
        public UpdateAttribute()
        { }
    }
}
