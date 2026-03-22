using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Spells
{
    /// <summary>
    /// État de sort (buff/debuff) applicable à un combattant.
    /// Un état peut bloquer certaines actions (lancer un sort, combattre, bouger...)
    /// ou conférer des immunités (invulnérable aux corps-à-corps, aux soins, etc.).
    /// Les états sont référencés par StatesRequired et StatesForbidden dans SpellLevelRecord.
    /// </summary>
    [Table("SpellsStates")]
    public class SpellStateRecord : ITable
    {
        // Liste de tous les états de sorts chargés en mémoire au démarrage
        public static List<SpellStateRecord> SpellsStates = new List<SpellStateRecord>();

        [Primary]
        public short Id;        // Identifiant unique de l'état

        public string Name;     // Nom de l'état (ex: "Immobilisé", "Invulnérable")

        public bool PreventsSpellCast;      // Si true, le personnage ne peut pas lancer de sorts

        public bool PreventsFight;          // Si true, le personnage ne peut pas engager de combat

        public bool IsSilent;               // Si true, l'état est invisible côté client (pas d'animation)

        public bool CantBeMoved;            // Si true, le personnage ne peut pas être déplacé par des sorts

        public bool CantBePushed;           // Si true, le personnage ne peut pas être poussé

        public bool CantDealDamage;         // Si true, le personnage ne peut pas infliger de dégâts

        public bool Invulnerable;           // Si true, le personnage est invulnérable à tous les dégâts

        public bool CantSwitchPosition;     // Si true, le personnage ne peut pas changer de position

        public bool Incurable;              // Si true, le personnage ne peut pas être soigné

        public bool InvulnerableMelee;      // Si true, invulnérable aux dégâts de corps-à-corps uniquement

        public bool InvulnerableRange;      // Si true, invulnérable aux dégâts à distance uniquement

        public SpellStateRecord(short id, string name, bool preventspellCast, bool preventFight, bool isSilent, bool cantBeMoved, bool cantBePushed, bool cantDeadDamage, bool invulnerable,
            bool cantSwitchPosition, bool incurable, bool invulnerableMelee, bool invulnerableRange)
        {
            this.Id = id;
            this.Name = name;
            this.PreventsSpellCast = preventspellCast;
            this.PreventsFight = preventFight;
            this.IsSilent = isSilent;
            this.CantBeMoved = cantBeMoved;
            this.CantBePushed = cantBePushed;
            this.CantDealDamage = cantDeadDamage;
            this.Invulnerable = invulnerable;
            this.CantSwitchPosition = cantSwitchPosition;
            this.Incurable = incurable;
            this.InvulnerableMelee = invulnerableMelee;
            this.InvulnerableRange = invulnerableRange;
        }

        public static SpellStateRecord GetState(int id)
        {
            return SpellsStates.Find(x => x.Id == id);
        }
    }
}
