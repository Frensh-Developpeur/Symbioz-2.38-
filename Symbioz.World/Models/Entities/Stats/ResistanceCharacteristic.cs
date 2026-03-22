using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Résistance élémentaire en pourcentage (ex. résistance Feu, Eau, Air, Terre, Neutre).
    /// Plafonnée à 50% (RESISTANCE_PERCENTAGE_LIMIT), même en contexte de combat.
    /// ContextLimit = true : les buffs de combat ne peuvent pas faire dépasser 50%.
    /// </summary>
    public class ResistanceCharacteristic : LimitCharacteristic
    {
        // Plafond absolu de résistance en % (50% dans Dofus 2.x)
        public const short RESISTANCE_PERCENTAGE_LIMIT = 50;

        // Retourne le plafond de 50% défini par la constante
        [YAXDontSerialize]
        public override short Limit
        {
            get
            {
                return RESISTANCE_PERCENTAGE_LIMIT;
            }
        }

        // true : même les buffs de contexte en combat sont plafonnés à 50%
        [YAXDontSerialize]
        public override bool ContextLimit
        {
            get
            {
                return true;
            }
        }

        // Crée une résistance avec la valeur de base donnée
        public static new ResistanceCharacteristic New(short @base)
        {
            return new ResistanceCharacteristic()
            {
                Base = @base,
                Additional = 0,
                Context = 0,
                Objects = 0
            };
        }

        // Crée une résistance à 0% (utilisée pour initialiser les monstres et personnages)
        public static ResistanceCharacteristic Zero()
        {
            return New(0);
        }

        // Copie profonde de la résistance (utilisée lors de l'entrée en combat)
        public override Characteristic Clone()
        {
            return new ResistanceCharacteristic()
            {
                Additional = Additional,
                Base = Base,
                Context = Context,
                Objects = Objects
            };
        }
    }
}
