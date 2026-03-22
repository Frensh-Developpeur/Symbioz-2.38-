using Symbioz.Core;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain.Actions
{
    /// <summary>
    /// Action de l'IA : lancer des sorts.
    /// Analyse les catégories de sorts disponibles (Agressive, Heal, Buff, Summon...),
    /// puis pour chaque catégorie par ordre de priorité, tente de lancer le sort
    /// en choisissant la meilleure cellule cible via EnvironmentAnalyser.
    /// </summary>
    [Brain(ActionType.CastSpell)]
    public class CastSpellAction : BrainAction
    {
        // Dictionnaire priorité → catégorie de sort (calculé lors de l'analyse)
        private Dictionary<int, SpellCategoryEnum> Categories
        {
            get;
            set;
        }
        public CastSpellAction(BrainFighter fighter)
            : base(fighter)
        {

        }
        // Analyse : récupère les catégories de sorts prioritaires pour ce monstre
        public override void Analyse()
        {
            this.Categories = EnvironmentAnalyser.Instance.GetSpellsCategories(Fighter);
        }
        // Exécute les sorts par ordre de priorité croissante :
        // Pour chaque catégorie, cherche les sorts disponibles, mélange aléatoirement,
        // et tente de lancer chaque sort si le monstre a assez de PA et qu'une cible est trouvée
        public override void Execute()
        {
            if (!Fighter.Alive)
                return;

            // Tri par clé (priorité) croissante : priorité 0 avant priorité 5
            foreach (var category in Categories.OrderByDescending(x => x.Key).Reverse())
            {
                // Récupère les sorts de la catégorie au niveau maximum
                var levels = Fighter.Template.SpellRecords.FindAll(x => x.CategoryEnum == category.Value).ConvertAll<SpellLevelRecord>(x => x.GetLastLevel());

                // Mélange aléatoire pour éviter un comportement prévisible
                foreach (var level in levels.Shuffle())
                {
                    if (Fighter.Stats.ActionPoints.TotalInContext() >= level.ApCost)
                    {
                        if (Fighter.Fight.Ended)
                            return;

                        // Demande à l'EnvironmentAnalyser la meilleure cellule cible
                        short cellId = EnvironmentAnalyser.Instance.GetTargetedCell(Fighter, category.Value, level);

                        if (cellId != -1)
                        {
                            var spell = SpellRecord.GetSpellRecord(level.SpellId);
                            if (spell != null)
                                Fighter.CastSpell(spell, spell.GetLastLevelGrade(), cellId);
                        }
                        else
                            break; // Aucune cible valide → arrête cette catégorie
                    }
                }

            }
        }
    }
}
