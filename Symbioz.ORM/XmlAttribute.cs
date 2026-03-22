using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui indique qu'un champ doit être sérialisé/désérialisé en format XML.
    /// Plutôt que de stocker un objet complexe sous forme de texte brut en base de données,
    /// [Xml] permet de le convertir automatiquement en XML pour le stockage,
    /// puis de le reconstruire depuis le XML lors de la lecture.
    /// Utile pour stocker des objets imbriqués, des listes d'objets complexes, etc.
    /// Exemple : [Xml] public List<MonObjetComplexe> MaListe;
    /// </summary>
    public class XmlAttribute : Attribute
    {
        // Aucun paramètre nécessaire : la simple présence de [Xml] sur un champ
        // indique à l'ORM d'utiliser la sérialisation XML pour ce champ.
    }
}
