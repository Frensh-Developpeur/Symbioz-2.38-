using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Points d'Action (PA) : caractéristique spécialisée avec plafond configurable.
    /// Le plafond (WorldConfiguration.ApLimit, typiquement 12 PA) s'applique au total de base,
    /// mais ContextLimit = false signifie que les buffs de contexte en combat peuvent dépasser ce plafond.
    /// </summary>
    public class ApCharacteristic : LimitCharacteristic
    {
        // Plafond des PA de base (défini dans la configuration du serveur, ex. 12)
        [YAXDontSerialize]
        public override short Limit
        {
            get
            {
                return WorldConfiguration.Instance.ApLimit;
            }
        }

        // false : les buffs en combat (Context) peuvent dépasser le plafond de base
        [YAXDontSerialize]
        public override bool ContextLimit
        {
            get
            {
                return false;
            }
        }

        // Crée une nouvelle instance avec une valeur de base donnée et tous les autres champs à 0
        public static new ApCharacteristic New(short @base)
        {
            return new ApCharacteristic()
            {
                Base = @base,
                Additional = 0,
                Context = 0,
                Objects = 0
            };
        }

        // Copie profonde de la caractéristique (utilisée lors de l'entrée en combat)
        public override Characteristic Clone()
        {
            return new ApCharacteristic()
            {
                Additional = Additional,
                Base = Base,
                Context = Context,
                Objects = Objects
            };
        }
    }
}
