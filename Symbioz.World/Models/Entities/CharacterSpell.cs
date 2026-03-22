using Symbioz.Protocol.Types;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities
{
    /// <summary>
    /// Sort possédé par un personnage joueur, sérialisé en XML dans CharacterRecord.
    ///
    /// Chaque personnage possède une liste de CharacterSpell (sort + grade actuel).
    /// Un sort peut être monté de grade (1 à 6) en dépensant des points de sort.
    ///
    /// Template est chargé paresseusement (lazy loading) depuis SpellRecord au premier accès.
    /// GetBoostCost() calcule le coût en points de sort pour passer d'un grade à un autre.
    /// </summary>
    public class CharacterSpell
    {
        public CharacterSpell(ushort spellId, sbyte grade)
        {
            this.SpellId = spellId;
            this.Grade = grade;
        }
        public CharacterSpell()
        {
        }

        // Identifiant du sort (référence vers SpellRecord)
        public ushort SpellId
        {
            get;
            private set;
        }

        // Grade actuel du sort (1 = grade de base, 6 = grade maximum)
        public sbyte Grade
        {
            get;
            private set;
        }

        // Cache interne du template (chargé lors du premier accès à Template)
        [YAXDontSerialize]
        private SpellRecord m_template;

        // Template du sort (chargé depuis SpellRecord au premier accès, puis mis en cache)
        [YAXDontSerialize]
        public SpellRecord Template
        {
            get
            {
                if (m_template == null)
                {
                    m_template = SpellRecord.GetSpellRecord((ushort)SpellId);
                    return m_template;
                }
                else
                {
                    return m_template;
                }
            }
        }

        // Met à jour le grade du sort (appelé lors de la montée en grade)
        public void SetGrade(sbyte grade)
        {
            this.Grade = grade;
        }

        // Sérialise le sort pour le protocole client (barre de sorts)
        public SpellItem GetSpellItem()
        {
            return new SpellItem(SpellId, Grade);
        }

        // Calcule le coût en points de sort pour monter du grade actualspellgrade au grade newgrade.
        // Le coût de chaque niveau est égal au numéro du niveau : grade 1→2 coûte 1, 2→3 coûte 2, etc.
        public static ushort GetBoostCost(sbyte actualspellgrade, sbyte newgrade)
        {
            ushort cost = 0;
            for (sbyte i = actualspellgrade; i < newgrade; i++)
            {
                cost += (ushort)i;
            }
            return (ushort)(cost);
        }
    }
}
