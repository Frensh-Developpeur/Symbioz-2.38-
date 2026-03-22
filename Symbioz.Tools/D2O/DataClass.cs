using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Tools.D2O
{
    /// <summary>
    /// Représentation générique d'un objet de données lu depuis un fichier D2O.
    /// Au lieu d'utiliser un type C# fortement typé, DataClass stocke les champs
    /// dans un dictionnaire dynamique (nom_du_champ -> valeur).
    /// C'est une alternative à l'approche réflexive de GameDataClassDefinition :
    /// ici on ne crée pas d'instance d'une vraie classe C#, on stocke juste les données brutes.
    /// </summary>
    public class DataClass
    {
        /// <summary>
        /// Accès à un champ par son nom (indexeur).
        /// Exemple : monObjet["id"] retourne la valeur du champ "id".
        /// </summary>
        /// <param name="Name">Nom du champ à lire.</param>
        /// <returns>La valeur du champ, ou null si le champ n'existe pas.</returns>
        public object this[string Name]
        {
            get
            {
                if (!this.Fields.ContainsKey(Name))
                {
                    return null;
                }
                return this.Fields[Name];
            }
        }

        /// <summary>
        /// Dictionnaire de tous les champs de cet objet D2O.
        /// Les clés sont les noms des champs (string), les valeurs peuvent être de tout type
        /// (int, bool, string, List, ou un autre DataClass imbriqué).
        /// </summary>
        public Dictionary<string, object> Fields = new Dictionary<string, object>();

        /// <summary>
        /// Nom de la classe D2O dont est issu cet objet (ex: "Spell", "Item", "MapPosition").
        /// </summary>
        public string Name;
    }


}
