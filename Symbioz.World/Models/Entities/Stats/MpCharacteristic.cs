using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Points de Mouvement (PM) : identique à ApCharacteristic mais pour les déplacements.
    /// Le plafond (WorldConfiguration.MpLimit, typiquement 6 PM) s'applique au total de base.
    /// ContextLimit = false : les buffs de combat peuvent temporairement dépasser ce plafond.
    /// </summary>
    public class MpCharacteristic : LimitCharacteristic
    {
        // Plafond des PM de base (défini dans la configuration du serveur, ex. 6)
        [YAXDontSerialize]
        public override short Limit
        {
            get
            {
                return WorldConfiguration.Instance.MpLimit;
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
        public static MpCharacteristic New(short @base)
        {
            return new MpCharacteristic()
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
            return new MpCharacteristic()
            {
                Additional = Additional,
                Base = Base,
                Context = Context,
                Objects = Objects
            };
        }
    }
}
