using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Fights.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Damages
{
    /// <summary>
    /// Représente un "jet de dés" pour un sort ou une attaque.
    /// Contient les valeurs minimale, maximale et actuelle (résultat du tirage aléatoire)
    /// des dégâts bruts avant application des résistances.
    /// Ces trois valeurs sont affichées dans les logs de combat côté client.
    /// </summary>
    public class Jet
    {
        // Dégâts minimum possibles avec ce sort (affiché dans l'interface)
        public short DeltaMin
        {
            get;
            set;
        }
        // Dégâts maximum possibles avec ce sort (affiché dans l'interface)
        public short DeltaMax
        {
            get;
            set;
        }
        // Valeur réellement tirée aléatoirement entre DeltaMin et DeltaMax
        public short Delta
        {
            get;
            set;
        }
        public Jet(short deltaMin, short deltaMax, short delta)
        {
            this.DeltaMin = deltaMin;
            this.DeltaMax = deltaMax;
            this.Delta = delta;
        }
        // Crée une copie indépendante de ce jet (pour éviter les modifications non voulues)
        public Jet Clone()
        {
            return new Jet(DeltaMin, DeltaMax, Delta);
        }

    }
}
