using Symbioz.Core;
using Symbioz.Core.DesignPattern;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Providers.Brain.Actions;
using Symbioz.World.Providers.Brain.Behaviors;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain
{
    /// <summary>
    /// Analyseur d'environnement de combat pour l'IA des monstres.
    /// Singleton utilisé par MonsterBrain pour déterminer :
    ///   1. GetSortedActions() : quels types d'actions effectuer et dans quel ordre
    ///   2. GetSpellsCategories() : quelles catégories de sorts lancer (avec leur priorité)
    ///   3. GetTargetedCell() : quelle cellule cibler pour chaque sort
    ///
    /// Les méthodes de sélection de cible sont enregistrées via l'attribut [TargetSelector]
    /// et invoquées dynamiquement par réflexion selon la catégorie du sort.
    /// Si le monstre a un comportement personnalisé (Behavior), il est consulté en priorité.
    /// </summary>
    public class EnvironmentAnalyser : Singleton<EnvironmentAnalyser>
    {
        // Dictionnaire attribut TargetSelector → méthode de sélection de cible (chargé par réflexion)
        Dictionary<TargetSelector, MethodInfo> Handlers;

        [StartupInvoke("Environment Analyser", StartupInvokePriority.Eighth)]
        public void Initialize()
        {
            Handlers = this.GetType().MethodsWhereAttributes<TargetSelector>();
        }
        // Seuil de vie (en %) en dessous duquel le monstre est considéré en danger
        public const sbyte WarningLifePercentage = 15;

        // Retourne la liste ordonnée des types d'actions à effectuer ce tour
        // Si le monstre a un Behavior personnalisé, celui-ci définit ses propres actions
        public List<ActionType> GetSortedActions(BrainFighter fighter)
        {
            List<ActionType> actions = new List<ActionType>();
            if (fighter.Brain.HasBehavior)
            {
                var behaviorActions = fighter.Brain.GetBehavior<Behavior>().GetSortedActions();

                if (behaviorActions != null)
                {
                    return behaviorActions.ToList();
                }
            }

            actions.Add(ActionType.CastSpell);
            actions.Add(ActionType.MoveToEnemy);
            actions.Add(ActionType.CastSpell);
            actions.Add(ActionType.Flee);
            actions.Add(ActionType.CastSpell);

            return actions;
        }
        /// <summary>
        /// Retourne les catégories de sorts à utiliser ce tour, indexées par priorité (clé = priorité).
        /// Une priorité plus basse = le sort est lancé en premier.
        /// Logique par défaut :
        ///   - Priorité -1 : ressusciter un allié mort
        ///   - Priorité 0 : invoquer une créature (si slot disponible)
        ///   - Priorité 1 : téléportation (si pas d'ennemi adjacent)
        ///   - Priorité 2 : attaque (si vie > 15%), ou priorité 5 (si en danger)
        ///   - Priorité 3 : soin
        ///   - Priorité 4 : buff
        ///   - Priorité 6 : sorts inconnus/divers
        /// </summary>
        public Dictionary<int, SpellCategoryEnum> GetSpellsCategories(BrainFighter fighter)
        {

            Dictionary<int, SpellCategoryEnum> categories;
            if (fighter.Brain.HasBehavior)
            {
                categories = fighter.Brain.GetBehavior<Behavior>().GetSpellsCategories();

                if (categories != null)
                {
                    return categories;
                }
            }

            categories = new Dictionary<int, SpellCategoryEnum>();

            if (fighter.Team.LastDead() != null)
            {
                categories.Add(-1, SpellCategoryEnum.ReviveDeath);
            }

            if (fighter.CanSummon)
                categories.Add(0, SpellCategoryEnum.Summon);

            // Téléportation uniquement s'il n'y a pas d'ennemi adjacent (pour se rapprocher)
            if (!fighter.IsThereEnemy(fighter.Point.GetNearPoints()))
            {
                categories.Add(1, SpellCategoryEnum.Teleport);
            }

            // Si le monstre est en danger (vie < 15%), l'attaque est déprioritisée (priorité 5 au lieu de 2)
            if (fighter.Stats.LifePercentage > WarningLifePercentage)
            {
                categories.Add(2, SpellCategoryEnum.Agressive);
            }
            else
            {
                categories.Add(5, SpellCategoryEnum.Agressive);
            }

            categories.Add(3, SpellCategoryEnum.Heal);
            categories.Add(4, SpellCategoryEnum.Buff);

            categories.Add(6, SpellCategoryEnum.Unknown);
            return categories;
        }
        // Retourne la cellule cible pour un sort donné et une catégorie.
        // Délègue au Behavior personnalisé si présent, sinon utilise le handler [TargetSelector] correspondant.
        public short GetTargetedCell(BrainFighter fighter, SpellCategoryEnum category, SpellLevelRecord level)
        {
            if (fighter.Brain.HasBehavior)
            {
                var cellId = fighter.Brain.GetBehavior<Behavior>().GetTargetCellForSpell(level.SpellId);

                if (cellId != null)
                {
                    return cellId.Value;
                }
            }
            // Cherche dans les handlers enregistrés via [TargetSelector] le handler pour cette catégorie
            var handler = Handlers.FirstOrDefault(x => x.Key.SpellCategory == category);

            if (handler.Value != null)
            {
                return (short)handler.Value.Invoke(this, new object[] { fighter, level });
            }
            else
                return fighter.CellId; // Par défaut : cibler sa propre cellule
        }

        // Cible agressive : sélectionne l'ennemi visible le plus proche avec le moins de PV
        [TargetSelector(SpellCategoryEnum.Agressive)]
        public short AgressiveTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            if (fighter.Brain.HasBehavior)
            {
                var agressiveCell = fighter.Brain.GetBehavior<Behavior>().GetAgressiveCell();

                if (agressiveCell != -1)
                {
                    return agressiveCell;
                }
            }

            if (level.MaxRange > 0)
            {
                // Cherche l'ennemi visible le plus proche (dernière position dans la liste triée par distance)
                var targets = fighter.OposedTeam().CloserFighters(fighter);
                var target = targets.LastOrDefault(x => x.Stats.InvisibilityState == Protocol.Enums.GameActionFightInvisibilityStateEnum.VISIBLE);

                if (target != null)
                    return target.CellId;
                else
                    // Aucun ennemi visible → cible une cellule aléatoire dans la zone de lancer
                    return Array.FindAll(fighter.GetCastZone(fighter.CellId, level), x => fighter.Fight.Map.WalkableDuringFight((ushort)x)).Random();
            }
            else
            {
                return fighter.CellId; // Sort sans portée → se cibler soi-même
            }
        }

        // Cible de buff : l'allié avec le moins de % de vie (pour le soigner/booster en priorité)
        [TargetSelector(SpellCategoryEnum.Buff)]
        public short BuffTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            if (fighter.Brain.HasBehavior)
            {
                var buffCell = fighter.Brain.GetBehavior<Behavior>().GetBuffCell();

                if (buffCell != -1)
                {
                    return buffCell;
                }
            }

            if (level.MaxRange > 0)
            {
                var target = fighter.Team.LowerFighterPercentage();
                return target != null ? target.CellId : fighter.CellId;
            }
            else
            {
                return fighter.CellId;
            }
        }

        // Cible de téléportation : une cellule libre adjacente à l'ennemi le plus faible
        [TargetSelector(SpellCategoryEnum.Teleport)]
        public short TeleportTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            if (fighter.Brain.HasBehavior)
            {
                var teleportCell = fighter.Brain.GetBehavior<Behavior>().GetTeleportCell();

                if (teleportCell != -1)
                {
                    return teleportCell;
                }
            }

            var target = fighter.OposedTeam().LowerFighter();





            if (target != null)
            {
                var points = target.Point.GetNearPoints();

                if (points.Count() > 0)
                {
                    // Cherche une cellule libre adjacente à l'ennemi pour se téléporter dessus
                    var pt = points.FirstOrDefault(x => fighter.Fight.IsCellFree(x.CellId));

                    if (pt != null)
                    {
                        return pt.CellId;
                    }
                }

            }
            return -1; // Pas de cible valide pour la téléportation
        }
        // Cible d'invocation : la cellule libre la plus proche du monstre
        [TargetSelector(SpellCategoryEnum.Summon)]
        public short SummonTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            return fighter.NearFreeCell();

        }
        // Cible de résurrection : la première cellule libre adjacente (pour placer l'allié ressuscité)
        [TargetSelector(SpellCategoryEnum.ReviveDeath)]
        public short ReviveDeathTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            return fighter.Point.GetNearPoints().FirstOrDefault(x => fighter.Fight.IsCellFree(x.CellId)).CellId;
        }
        // Cible de soin : l'allié avec le moins de % de vie
        [TargetSelector(SpellCategoryEnum.Heal)]
        public short HealTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            return fighter.Team.LowerFighterPercentage().CellId;
        }
        // Sorts inconnus : utilise la même logique que les sorts agressifs
        [TargetSelector(SpellCategoryEnum.Unknown)]
        public short UnkownTarget(BrainFighter fighter, SpellLevelRecord level)
        {
            return AgressiveTarget(fighter, level);
        }
    }
}
