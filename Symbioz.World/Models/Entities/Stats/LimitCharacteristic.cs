using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Caractéristique avec une valeur maximale (plafond).
    /// Sert de base pour PA (ApCharacteristic), PM (MpCharacteristic) et résistances (ResistanceCharacteristic).
    ///
    /// - Limit : valeur maximale configurable (ex. 12 PA, 6 PM, 50% résistance)
    /// - ContextLimit : si true, le plafond s'applique aussi à TotalInContext() (résistances en combat)
    ///                  si false, le plafond ne s'applique qu'à Total() (PA/PM : les buffs de contexte peuvent dépasser)
    /// </summary>
    public abstract class LimitCharacteristic : Characteristic
    {
        // Valeur maximale de la caractéristique (configurée par WorldConfiguration)
        [YAXDontSerialize]
        public abstract short Limit
        {
            get;
        }

        // Indique si le plafond s'applique aussi en contexte de combat (true pour les résistances)
        [YAXDontSerialize]
        public abstract bool ContextLimit
        {
            get;
        }

        // Retourne le total plafonné à Limit (utilisé hors combat ou comme max de référence)
        public override short Total()
        {
            short total = base.Total();
            return total > Limit ? Limit : total;
        }

        // Retourne le total en contexte de combat, plafonné seulement si ContextLimit est vrai
        public override short TotalInContext()
        {
            if (ContextLimit)
            {
                short total = base.TotalInContext();
                return total > Limit ? Limit : total;
            }
            else
            {
                return base.TotalInContext();
            }
        }
    }
}
