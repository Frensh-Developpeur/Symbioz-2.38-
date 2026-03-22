using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Marks
{
    /// <summary>
    /// Interface implémentée par les marques qui ont une durée limitée (en tours).
    /// Par exemple, un Glyphe peut durer 3 tours avant de disparaître.
    /// À chaque fin de tour, le système appelle DecrementDuration() sur chaque IDurationMark.
    /// Si la méthode retourne true, la marque est retirée du combat.
    /// </summary>
    public interface IDurationMark
    {
        // Décrémente la durée restante d'un tour et retourne true si la marque doit être supprimée
        bool DecrementDuration();

        short Duration
        {
            get;
            set;
        }
    }
}
