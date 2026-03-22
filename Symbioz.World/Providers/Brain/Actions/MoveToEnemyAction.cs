using Symbioz.World.Models.Fights.Fighters;
using System;
using System.Collections.Generic;
using Symbioz.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain.Actions
{
    /// <summary>
    /// Action de l'IA : se déplacer vers l'ennemi le plus faible (en points de vie).
    /// Cible uniquement les combattants non-invoqués pour éviter de viser des totems.
    /// Si le monstre est déjà sur la même cellule que la cible, il se déplace vers
    /// une cellule adjacente libre pour libérer la cellule occupée.
    /// </summary>
    [Brain(ActionType.MoveToEnemy)]
    public class MoveToEnemyAction : BrainAction
    {
        // Ennemi cible sélectionné lors de l'analyse (le plus faible en PV)
        private Fighter Target
        {
            get;
            set;
        }

        public MoveToEnemyAction(BrainFighter fighter)
            : base(fighter)
        {

        }
        // Analyse : sélectionne l'ennemi non-invoqué avec le moins de points de vie
        public override void Analyse()
        {
            List<Fighter> fighters = Fighter.OposedTeam().GetFighters().FindAll(x => !x.IsSummon);
            Target = fighters.Count == 0 ? null : fighters.Aggregate((f1, f2) => f1.Stats.CurrentLifePoints < f2.Stats.CurrentLifePoints ? f1 : f2);
        }

        // Exécute le déplacement vers la cible
        public override void Execute()
        {
            if (Target == null)
                return;
            var path = Target.FindPathTo(Fighter);

            // Si déjà sur la même cellule que la cible, se décaler vers une cellule adjacente
            if (Fighter.CellId == Target.CellId)
            {
                var points = Target.Point.GetNearPoints();

                var point = points.FirstOrDefault(x => Fighter.Fight.IsCellFree(x.CellId));

                if (point != null)
                    Fighter.Move(new List<short>() { Fighter.CellId, point.CellId });
                return;
            }
            if (path.Count() > 0)
                Fighter.Move(path.ToList());

        }
    }
}
