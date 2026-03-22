using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Portée supplémentaire des sorts : caractéristique avec un plafond de +6 cases.
    /// ContextLimit = true : les buffs de combat sont aussi plafonnés.
    /// Cette stat s'ajoute à la portée de base d'un sort quand "portée modifiable" est activé.
    /// </summary>
    public class RangeCharacteristic : LimitCharacteristic
    {
        // Portée supplémentaire maximale accordée par cette caractéristique (+6 cases)
        public const short RANGE_LIMIT = 6;

        // Plafond de +6 cases de portée
        [YAXDontSerialize]
        public override short Limit
        {
            get
            {
                return RANGE_LIMIT;
            }
        }

        // true : même les buffs temporaires de portée sont plafonnés à +6 en combat
        [YAXDontSerialize]
        public override bool ContextLimit
        {
            get
            {
                return true;
            }
        }

        public static RangeCharacteristic New(short @base)
        {
            return new RangeCharacteristic()
            {
                Base = @base,
                Additional = 0,
                Context = 0,
                Objects = 0
            };
        }

        public static RangeCharacteristic Zero()
        {
            return New(0);
        }

        public override Characteristic Clone()
        {
            return new RangeCharacteristic()
            {
                Additional = Additional,
                Base = Base,
                Context = Context,
                Objects = Objects
            };
        }
    }
}
