using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Buffs
{
    /// <summary>
    /// Classe de base pour tous les buffs/debuffs appliqués sur un combattant.
    /// Un buff est un effet temporaire qui dure N tours (Duration) et peut être dissipé.
    /// Chaque buff a un ID unique, une cible (Target), un lanceur (Caster),
    /// le sort et l'effet source, une durée, et un indicateur de dissipabilité (Dispelable).
    ///
    /// À chaque fin de tour, DecrementDuration() est appelé. Quand la durée atteint 0,
    /// le buff est dissipé via Dispell().
    /// Les sous-classes concrètes implémentent Apply() (activation) et Dispell() (retrait).
    /// </summary>
    public abstract class Buff
    {
        // Identifiant unique de ce buff dans la liste des buffs de la cible
        public int Id
        {
            get;
            private set;
        }
        // Combattant sur lequel ce buff est appliqué
        public Fighter Target
        {
            get;
            private set;
        }
        // Combattant qui a lancé le sort créant ce buff
        public Fighter Caster
        {
            get;
            private set;
        }
        // Données de l'effet de sort à l'origine de ce buff (valeurs, durée, élément...)
        public EffectInstance Effect
        {
            get;
            private set;
        }
        // ID du sort qui a créé ce buff (pour la dissipation par sort)
        public ushort SpellId
        {
            get;
            private set;
        }
        // Niveau du sort qui a créé ce buff
        public SpellLevelRecord Level
        {
            get;
            private set;
        }
        // Durée restante en tours (−1 = durée infinie)
        public short Duration
        {
            get;
            set;
        }
        // True si le buff a été déclenché en coup critique
        public bool Critical
        {
            get;
            private set;
        }
        // Indique si ce buff peut être dissipé (DISPELLABLE, NON_DISPELLABLE...)
        public FightDispellableEnum Dispelable
        {
            get;
            set;
        }
        // ID d'action personnalisé pour l'affichage client (null = utilise l'EffectId)
        public short? CustomActionId
        {
            get;
            private set;
        }
        // Multiplicateur d'efficacité du buff (1.0 = normal, utilisé pour certains calculs)
        public double Efficiency
        {
            get;
            set;
        }
        // True si le buff est actif (pas de délai d'activation en cours)
        public bool Active
        {
            get
            {
                return GetDelay() <= 0;
            }
        }
        protected Buff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, bool critical, FightDispellableEnum dispelable)
        {
            this.Id = id;
            this.Target = target;
            this.Caster = caster;
            this.Effect = effect;
            this.SpellId = spellId;
            this.Critical = critical;
            this.Level = level;
            this.Dispelable = dispelable;
            this.Duration = (short)this.Effect.Duration;
            this.Efficiency = 1.0;
        }
        protected Buff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, bool critical, FightDispellableEnum dispelable, short customActionId)
        {
            this.Id = id;
            this.Target = target;
            this.Caster = caster;
            this.Effect = effect;
            this.SpellId = spellId;
            this.Critical = critical;
            this.Level = level;
            this.Dispelable = dispelable;
            this.CustomActionId = new short?(customActionId);
            this.Duration = (short)this.Effect.Duration;
            this.Efficiency = 1.0;
        }
        // Décrémente la durée d'un tour ; retourne true si le buff doit être supprimé
        // Duration == -1 signifie durée infinie (ne décrémente pas)
        public bool DecrementDuration()
        {
            return this.Duration != -1 && (this.Duration -= 1) <= 0;
        }

        // Retourne le délai avant activation (0 = actif immédiatement)
        public virtual short GetDelay()
        {
            return 0;
        }
        // Retourne true si c'est un TriggerBuff (activé à un événement spécifique)
        public virtual bool IsTrigger()
        {
            return false;
        }

        // Applique l'effet du buff sur la cible (modifie les stats, l'état, etc.)
        public abstract void Apply();
        // Retire l'effet du buff de la cible (annule les modifications de Apply)
        public abstract void Dispell();

        // Retourne l'ID de l'action envoyée au client (CustomActionId si défini, sinon EffectId)
        public short GetActionId()
        {
            short result;
            if (this.CustomActionId.HasValue)
            {
                result = this.CustomActionId.Value;
            }
            else
            {
                result = (short)this.Effect.EffectId;
            }
            return result;
        }
        // Retourne la représentation protocole de ce buff pour la synchronisation client
        public abstract AbstractFightDispellableEffect GetAbstractFightDispellableEffect();
    }
}
