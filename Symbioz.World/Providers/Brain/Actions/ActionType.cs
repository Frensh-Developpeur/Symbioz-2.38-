using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain.Actions
{
    /// <summary>
    /// Enumération des actions disponibles pour l'IA d'un monstre pendant son tour.
    /// Le comportement (Behavior) retourne un tableau trié de ces actions pour définir sa stratégie :
    /// par exemple un monstre Agressif va d'abord se déplacer vers un ennemi, puis lancer un sort.
    /// </summary>
    public enum ActionType
    {
        /// <summary>Se déplacer vers un allié (pour le soigner ou le défendre).</summary>
        MoveToAlly,
        /// <summary>Se déplacer vers un ennemi (pour l'attaquer au corps à corps).</summary>
        MoveToEnemy,
        /// <summary>Lancer un sort sur la meilleure cible disponible.</summary>
        CastSpell,
        /// <summary>Fuir le combat (s'éloigner des ennemis).</summary>
        Flee,
    }
}
