using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui indique que la suppression d'un enregistrement doit se faire via une clause WHERE sur l'Id.
    /// Par défaut, les suppressions utilisent la clé primaire ([Primary]) pour cibler la ligne à effacer.
    /// Cet attribut peut modifier ce comportement pour utiliser un champ Id spécifique
    /// dans la requête DELETE (DELETE FROM table WHERE Id = valeur).
    /// Utile pour les tables dont la logique de suppression diffère de la clé primaire standard.
    /// </summary>
    public class RemoveWhereIdAttribute : Attribute
    {
        // Aucun paramètre : la présence de [RemoveWhereId] sur une classe ou un champ
        // indique à l'ORM d'utiliser ce champ comme critère de suppression.
    }
}
