using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui marque une méthode devant être exécutée automatiquement avant la sauvegarde d'un enregistrement.
    /// Avant que l'ORM n'écrive les données en base de données, il recherche les méthodes
    /// portant cet attribut sur la classe et les appelle.
    /// Cela permet d'effectuer des traitements préparatoires : nettoyage de données,
    /// calculs de champs dérivés, validation, etc.
    /// Exemple : une méthode [BeforeSaving] pourrait recalculer un hash ou vider un cache temporaire.
    /// </summary>
    public class BeforeSavingAttribute : Attribute
    {
        // Aucun paramètre : la présence de [BeforeSaving] sur une méthode suffit
        // pour qu'elle soit appelée automatiquement avant chaque sauvegarde.
    }
}
