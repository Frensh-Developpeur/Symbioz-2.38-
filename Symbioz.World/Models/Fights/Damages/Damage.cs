using Symbioz.Core;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Damages
{
    /// <summary>
    /// Représente un calcul de dégâts infligés d'une source vers une cible.
    /// Contient toute la logique de calcul :
    ///   - réduction de dégâts (armure globale)
    ///   - résistances élémentaires (terre, feu, eau, air, neutre)
    ///   - bonus/malus coup critique
    ///   - multiplicateur final de dégâts
    /// Le Jet contient les valeurs min/max/actuelle des dégâts bruts.
    /// </summary>
    public class Damage
    {
        // Combattant qui inflige les dégâts
        public Fighter Source
        {
            get;
            private set;
        }
        // Combattant qui reçoit les dégâts
        public Fighter Target
        {
            get;
            private set;
        }
        // Valeur actuelle des dégâts (raccourci vers Jet.Delta)
        public short Delta
        {
            get
            {
                return Jet.Delta;
            }
            set
            {
                Jet.Delta = value;
            }
        }
        // Type élémentaire des dégâts (Terre, Feu, Eau, Air, Neutre, Direct)
        public EffectElementType ElementType
        {
            get;
            private set;
        }
        // Effet de sort associé à ce dégât (peut être null pour les dégâts directs)
        public EffectInstance Effect
        {
            get;
            private set;
        }
        private AsyncRandom Randomizer
        {
            get;
            set;
        }
        // Jet de dés contenant les valeurs min/max/actuelle
        private Jet Jet
        {
            get;
            set;
        }
        // true si ce dégât provient d'un coup critique
        private bool Critical
        {
            get;
            set;
        }
        public Damage(Fighter source, Fighter target, Jet jet, EffectElementType elementType, EffectInstance effect, bool critical)
        {
            this.Source = source;
            this.Target = target;
            this.Jet = jet;
            this.Effect = effect;
            this.ElementType = elementType;
            this.Randomizer = new AsyncRandom();
            this.Critical = critical;
        }
        public Damage(Fighter source, Fighter target, short delta, EffectElementType elementType = EffectElementType.Direct)
        {
            this.Source = source;
            this.Target = target;
            this.Jet = new Jet(delta, delta, delta);
            this.Effect = null;
            this.ElementType = elementType;
            this.Randomizer = new AsyncRandom();
            this.Critical = false;
        }
        /// <summary>
        /// Applique la réduction de dégâts globale de la cible (armure fixe).
        /// Ne peut pas réduire les dégâts en dessous de la valeur d'érosion minimale
        /// pour garantir qu'une partie des dégâts passe toujours.
        /// </summary>
        public void CalculateDamageReduction(ushort erosionDelta)
        {
            short reduction = Target.Stats.GlobalDamageReduction;

            if (reduction > 0)
            {
                // On s'assure que la réduction ne descend pas les dégâts en dessous de l'érosion
                if (Delta - reduction < erosionDelta)
                {
                    reduction = (short)(Delta - erosionDelta);
                }
                if (reduction > 0)
                {
                    Delta -= reduction;
                    Target.OnDamageReduced(Target.Stats.GlobalDamageReduction);
                }
            }
        }
        /// <summary>
        /// Applique les résistances élémentaires de la cible aux dégâts.
        /// Choisit les résistances PvP ou PvM selon le type de combat.
        /// Applique d'abord les bonus/malus de coup critique,
        /// puis calcule les dégâts après résistance en % et réduction fixe par élément.
        /// Empêche les dégâts de passer en négatif (plancher à 0).
        /// </summary>
        public void CalculateDamageResistance()
        {
            bool pvp = Source.Fight.PvP;
            int resistPercent = 0;
            int elementReduction = 0;

            // Sélectionne les résistances selon l'élément des dégâts
            switch (ElementType)
            {
                case EffectElementType.Earth:
                    resistPercent = GetResistanceValue(Target.Stats.EarthResistPercent, Target.Stats.PvPEarthResistPercent);
                    elementReduction = GetResistanceValue(Target.Stats.EarthReduction, Target.Stats.PvPEarthReduction);
                    break;
                case EffectElementType.Air:
                    resistPercent = GetResistanceValue(Target.Stats.AirResistPercent, Target.Stats.PvPAirResistPercent);
                    elementReduction = GetResistanceValue(Target.Stats.AirReduction, Target.Stats.PvPAirReduction);
                    break;
                case EffectElementType.Water:
                    resistPercent = GetResistanceValue(Target.Stats.WaterResistPercent, Target.Stats.PvPWaterResistPercent);
                    elementReduction = GetResistanceValue(Target.Stats.WaterReduction, Target.Stats.PvPWaterReduction);
                    break;
                case EffectElementType.Fire:
                    resistPercent = GetResistanceValue(Target.Stats.FireResistPercent, Target.Stats.PvPFireResistPercent);
                    elementReduction = GetResistanceValue(Target.Stats.FireReduction, Target.Stats.PvPFireReduction);
                    break;
                case EffectElementType.Neutral:
                    resistPercent = GetResistanceValue(Target.Stats.NeutralResistPercent, Target.Stats.PvPNeutralResistPercent);
                    elementReduction = GetResistanceValue(Target.Stats.NeutralReduction, Target.Stats.PvPNeutralReduction);
                    break;
                case EffectElementType.Direct:
                    break;
            }

            if (Critical) // Avant ou apres le calcul des resistances? on tente avant dans le doute.
            {
                Delta -= Target.Stats.CriticalDamageReduction.TotalInContext();
                Delta += Source.Stats.CriticalDamageBonus.TotalInContext();
            }

            this.Jet.DeltaMin = GetDeltaWithResists(resistPercent, elementReduction, Jet.DeltaMin);
            this.Jet.DeltaMax = GetDeltaWithResists(resistPercent, elementReduction, Jet.DeltaMax);
            this.Delta = GetDeltaWithResists(resistPercent, elementReduction, Delta);

            this.Jet.DeltaMin = (short)(this.Jet.DeltaMin < 0 ? 0 : Jet.DeltaMin);
            this.Jet.DeltaMax = (short)(this.Jet.DeltaMax < 0 ? 0 : Jet.DeltaMax);
            this.Jet.Delta = (short)(this.Jet.Delta < 0 ? 0 : Jet.Delta);

        }
        private short GetDeltaWithResists(int resistPercent, int reduction, short delta)
        {
            return (short)((1.0 - (double)resistPercent / 100.0) * ((double)delta - (double)reduction));
        }
        private int GetResistanceValue(Characteristic resistCharacteristic, Characteristic resistPvPCharacteristic)
        {
            return resistCharacteristic.TotalInContext() + (Source.Fight.PvP ? resistPvPCharacteristic.TotalInContext() : 0);
        }

        public void ApplyFinalBoost()
        {
            this.Delta = (short)((double)this.Delta * Source.Stats.FinalDamageCoefficient);
        }
    }
}
