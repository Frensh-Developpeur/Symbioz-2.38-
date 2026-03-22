using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui indique qu'une table peut être réinitialisée (remise à zéro).
    /// Lorsqu'un serveur World demande un reset de sa base de données via la transition Auth,
    /// les tables marquées [Resettable] peuvent être vidées et recréées automatiquement.
    /// Cela est utile lors de mises à jour majeures ou de resets de saison de jeu.
    /// </summary>
    public class ResettableAttribute : Attribute
    {
        // Aucun paramètre : la présence de [Resettable] sur une classe suffit à l'indiquer comme réinitialisable.
    }
}
