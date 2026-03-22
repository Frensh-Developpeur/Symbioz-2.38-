using Symbioz.World.Models.Fights.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain.Actions
{
    /// <summary>
    /// Classe de base pour toutes les actions de l'IA d'un monstre.
    /// Une action se déroule en deux phases distinctes :
    ///   1. Analyse() : calcule la meilleure cible ou position pour l'action
    ///   2. Execute() : effectue l'action (lancer un sort, se déplacer, fuir...)
    ///
    /// Les sous-classes concrètes sont enregistrées automatiquement par BrainProvider
    /// via l'attribut [Brain(ActionType.X)] et instanciées par réflexion à chaque tour.
    /// </summary>
    public abstract class BrainAction
    {
        // Le monstre qui effectue cette action
        public BrainFighter Fighter
        {
            get;
            set;
        }

        public BrainAction(BrainFighter fighter)
        {
            this.Fighter = fighter;
        }
        /// <summary>
        /// Phase d'analyse : calcule la meilleure cible/cellule pour l'action.
        /// Cette méthode est appelée AVANT Execute() pour préparer les données.
        /// </summary>
        public abstract void Analyse();
        /// <summary>
        /// Phase d'exécution : effectue l'action préparée lors de l'analyse.
        /// N'est appelée que si le monstre est encore vivant et c'est son tour.
        /// </summary>
        public abstract void Execute();

    }
}
