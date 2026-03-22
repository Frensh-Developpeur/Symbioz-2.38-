using Symbioz.World.Models.Fights.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain.Actions
{
    /// <summary>
    /// Action de l'IA : fuir en direction de l'allié le plus faible.
    /// Utilisée quand le monstre est en danger (vie basse).
    /// Le monstre se déplace vers le chemin qui mène à son allié le plus faible,
    /// ce qui peut l'amener à se regrouper avec les autres membres de son équipe.
    /// </summary>
    [Brain(ActionType.Flee)]
    public class FleeAction : BrainAction
    {
        public FleeAction(BrainFighter fighter)
            : base(fighter)
        {

        }
        // Aucune analyse nécessaire pour la fuite (la cible est calculée directement à l'exécution)
        public override void Analyse()
        {
        }

        // Se déplace vers l'allié le plus faible en points de vie (regroupement défensif)
        public override void Execute()
        {
            var path = Fighter.Team.LowerFighter().FindPathTo(Fighter).ToList();
            if (path.Count > 0)
                Fighter.Move(path);
        }
    }
}
