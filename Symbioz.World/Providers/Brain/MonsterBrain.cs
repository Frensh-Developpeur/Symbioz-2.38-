using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Providers.Brain.Actions;
using Symbioz.World.Providers.Brain.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain
{
    /// <summary>
    /// Représente l'intelligence artificielle d'un monstre en combat.
    /// Chaque monstre possède son propre cerveau (MonsterBrain) qui décide
    /// des actions à effectuer lors de son tour.
    ///
    /// Certains monstres ont un comportement personnalisé (Behavior), défini par un nom
    /// dans leur template (ex: "Agressive", "Pusher", "DragonPig"...).
    /// Les autres utilisent le comportement générique basé sur l'EnvironmentAnalyser.
    ///
    /// Déroulement d'un tour de monstre :
    ///   1. EnvironmentAnalyser détermine les types d'actions prioritaires (attaque, déplacement, fuite...)
    ///   2. BrainProvider instancie les actions correspondantes
    ///   3. Chaque action est analysée (calcul de la meilleure cible/position)
    ///   4. Chaque action est exécutée (si le monstre est encore vivant et c'est son tour)
    /// </summary>
    public class MonsterBrain
    {
        // Comportement personnalisé optionnel (certains monstres spéciaux ont leur propre IA)
        private Behavior Behavior
        {
            get;
            set;
        }
        // True si ce monstre a un comportement personnalisé défini
        public bool HasBehavior
        {
            get
            {
                return Behavior != null;
            }
        }
        // Référence au fighter associé à ce cerveau
        public BrainFighter Fighter
        {
            get;
            private set;
        }

        // Accès typé au comportement personnalisé (pour les comportements spéciaux)
        public T GetBehavior<T>() where T : Behavior
        {
            return (T)Behavior;
        }

        /// <summary>
        /// Crée le cerveau du monstre. Si le template du monstre définit un comportement
        /// personnalisé (BehaviorName), il est chargé via le BehaviorManager.
        /// </summary>
        public MonsterBrain(BrainFighter fighter)
        {
            this.Fighter = fighter;

            if (fighter.Template.BehaviorName != string.Empty && fighter.Template.BehaviorName != null)
                this.Behavior = BehaviorManager.GetBehavior(fighter.Template.BehaviorName, Fighter);
        }

        /// <summary>
        /// Joue le tour du monstre :
        /// 1. Détermine les types d'actions à effectuer (via l'analyse de l'environnement)
        /// 2. Crée les instances d'actions correspondantes
        /// 3. Analyse chaque action (trouve la meilleure cible/position)
        /// 4. Exécute les actions (tant que le monstre est vivant et c'est son tour)
        /// </summary>
        public void Play()
        {
            // Analyse la situation et détermine les types d'actions prioritaires
            var actionTypes = EnvironmentAnalyser.Instance.GetSortedActions(Fighter);

            var actions = new List<BrainAction>();

            // Instancie les actions correspondantes aux types déterminés
            foreach (var action in actionTypes)
            {
                actions.Add(BrainProvider.GetAction(Fighter, action));
            }

            // Phase d'analyse : chaque action calcule sa meilleure exécution
            foreach (var action in actions)
            {
                action.Analyse();
            }

            // Phase d'exécution : joue les actions dans l'ordre
            foreach (var action in actions)
            {
                // Vérifie que le monstre est encore vivant et que c'est toujours son tour
                if (Fighter.Alive && Fighter.IsFighterTurn)
                {
                    action.Execute();
                }
                else
                    break; // Arrête si le monstre est mort ou si son tour est terminé
            }
        }
    }

}
