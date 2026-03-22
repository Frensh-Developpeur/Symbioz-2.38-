using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain
{
    /// <summary>
    /// Attribut permettant d'associer une méthode de sélection de cible à une catégorie de sort.
    /// Utilisé par l'IA des monstres pour choisir la bonne cible selon le type de sort qu'elle veut lancer
    /// (sort agressif → cible un ennemi, soin → cible un allié, etc.).
    /// </summary>
    public class TargetSelector : Attribute
    {
        /// <summary>
        /// Catégorie de sort pour laquelle cette sélection de cible s'applique.
        /// </summary>
        public SpellCategoryEnum SpellCategory
        {
            get;set;
        }
        public TargetSelector(SpellCategoryEnum category)
        {
            this.SpellCategory = category;
        }
    }
}
