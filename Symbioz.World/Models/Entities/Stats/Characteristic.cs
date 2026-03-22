using Symbioz.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Représente une statistique d'un personnage ou d'un combattant.
    /// Une caractéristique est décomposée en 4 composantes indépendantes :
    ///   - Base : points distribués par le joueur (via les points de stats)
    ///   - Additional : bonus de classe, de parchemin
    ///   - Objects : bonus des équipements (calculé depuis l'inventaire)
    ///   - Context : bonus temporaires en combat (buffs, debuffs, malédictions)
    ///
    /// Total() = Base + Additional + Objects (valeur hors combat)
    /// TotalInContext() = Total() + Context (valeur en combat)
    ///
    /// Context n'est pas sérialisé en BDD (YAXDontSerialize) car il est réinitialisé à 0 hors combat.
    /// </summary>
    public class Characteristic
    {
        // Points attribués par le joueur (base permanente)
        public virtual short Base
        {
            get;
            set;
        }

        // Bonus additionnels (parchemins, bonus de classe)
        public virtual short Additional
        {
            get;
            set;
        }

        // Bonus provenant des équipements (recalculé à chaque changement d'équipement)
        public virtual short Objects
        {
            get;
            set;
        }

        // Bonus temporaires en combat (buffs/debuffs) — non persisté en BDD
        [YAXDontSerialize]
        public virtual short Context
        {
            get;
            set;
        }

        public virtual Characteristic Clone()
        {
            return new Characteristic()
            {
                Additional = Additional,
                Base = Base,
                Context = Context,
                Objects = Objects
            };
        }
        public static Characteristic Zero()
        {
            return New(0);
        }
        public static Characteristic New(short @base)
        {
            return new Characteristic()
            {
                Base = @base,
                Additional = 0,
                Context = 0,
                Objects = 0
            };
        }
        public CharacterBaseCharacteristic GetBaseCharacteristic()
        {
            return new CharacterBaseCharacteristic(Base, Additional, Objects, Context, Context);
        }
        // Total hors combat : base + additional + objects (ignoré Context)
        public virtual short Total()
        {
            return (short)(Base + Additional + Objects);
        }
        // Total en combat : inclut les buffs/debuffs temporaires (Context)
        public virtual short TotalInContext()
        {
            return (short)(Total() + Context);
        }
    }
}
