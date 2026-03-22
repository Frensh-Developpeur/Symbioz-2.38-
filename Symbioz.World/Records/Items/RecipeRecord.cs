using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Recette de craft pour un métier (forgeron, tailleur, alchimiste, etc.).
    /// Définit quel objet peut être fabriqué (ResultId), par quel métier (JobId/SkillId),
    /// et avec quels ingrédients (IngredientIds/Quantities).
    ///
    /// À la construction, Ingredients est construit comme un Dictionary pour un accès rapide :
    /// Ingredients[GIdIngredient] = quantité requise.
    /// Result (ItemRecord) est chargé depuis ItemRecord.Items au moment du constructeur.
    /// </summary>
    [Table("Recipes", true, 8)]
    public class RecipeRecord : ITable
    {
        // Liste de toutes les recettes chargées en mémoire au démarrage
        public static List<RecipeRecord> Recipes = new List<RecipeRecord>();

        [Primary]
        public ushort ResultId;     // GId de l'objet fabriqué (référence vers ItemRecord)

        [Ignore]
        public ItemRecord Result;   // Template de l'objet résultant (chargé en mémoire, non persisté)

        public string ResultName;   // Nom de l'objet fabriqué (dupliqué pour affichage rapide)

        public ushort ResultTypeId; // Type de l'objet fabriqué

        public ushort ResultLevel;  // Niveau requis pour fabriquer cet objet

        public List<ushort> IngredientIds;  // IDs des ingrédients nécessaires (GId dans ItemRecord)

        public List<uint> Quantities;       // Quantités requises pour chaque ingrédient (même ordre)

        public sbyte JobId;         // ID du métier requis pour utiliser cette recette

        public uint SkillId;        // ID du skill spécifique du métier (ex: "Forger une épée")

        [Ignore]
        // Dictionnaire ingrédient → quantité (construit depuis IngredientIds/Quantities, non persisté)
        public Dictionary<ushort, uint> Ingredients = new Dictionary<ushort, uint>();

        public RecipeRecord(ushort resultId, string resultName, ushort resultTypeId,
            ushort resultLevel, List<ushort> ingredientIds, List<uint> quantities,
            sbyte jobId, uint skillId)
        {
            this.ResultId = resultId;
            this.ResultName = resultName;
            this.ResultTypeId = resultTypeId;
            this.ResultLevel = resultLevel;
            this.IngredientIds = ingredientIds;
            this.Quantities = quantities;
            this.JobId = jobId;
            this.SkillId = skillId;

            for (int i = 0; i < IngredientIds.Count(); i++)
            {
                Ingredients.Add(IngredientIds[i], Quantities[i]);
            }
            this.Result = ItemRecord.GetItem(resultId);
        }

        public static RecipeRecord GetRecipe(ushort gid)
        {
            return Recipes.Find(x => x.ResultId == gid);
        }
    }
}
