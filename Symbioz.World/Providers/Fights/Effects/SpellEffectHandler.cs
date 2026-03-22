using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects
{
    // Classe de base abstraite pour tous les handlers d'effets de sorts
    // Chaque effet (dégâts feu, soin, buff de force, vulnérabilité...) hérite de cette classe
    // et implémente la méthode Apply() avec sa logique spécifique
    public abstract class SpellEffectHandler
    {
        // Le combattant qui a lancé le sort
        protected Fighter Source
        {
            get;
            private set;
        }

        // Accès rapide au combat en cours via le Source
        protected Fight Fight
        {
            get
            {
                return Source.Fight;
            }
        }

        // Les données du niveau du sort (coût PA, portée, effets en BDD...)
        protected SpellLevelRecord SpellLevel
        {
            get;
            private set;
        }

        // L'ID du sort (ex: 691 pour Vulnérabilité)
        protected ushort SpellId
        {
            get;
            private set;
        }

        // Les données de cet effet spécifique (EffectEnum, DiceMin, Duration, Zone...)
        protected EffectInstance Effect
        {
            get;
            private set;
        }

        // Les cibles calculées par SpellEffectsManager selon la zone de l'effet
        private Fighter[] BaseTargets
        {
            get;
            set;
        }

        // La cellule où le sort a été lancé
        protected MapPoint CastPoint
        {
            get;
            private set;
        }

        // True si le sort a fait un coup critique
        protected bool Critical
        {
            get;
            private set;
        }

        // Constructeur : reçoit toutes les infos nécessaires à l'exécution de l'effet
        public SpellEffectHandler(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect, Fighter[] targets, MapPoint castPoint, bool critical)
        {
            this.Source = source;
            this.SpellLevel = spellLevel;
            this.SpellId = spellLevel.SpellId;
            this.Effect = effect;
            this.BaseTargets = targets;
            this.CastPoint = castPoint;
            this.Critical = critical;
        }

        // Méthode abstraite à implémenter dans chaque handler
        // C'est ici que la logique du sort est codée (infliger dégâts, poser un buff, téléporter...)
        public abstract bool Apply(Fighter[] targets);

        // Appelé par SpellEffectsProvider après la création du handler
        // Gère le cas où l'effet est différé (Delay > 0 en BDD)
        public void Execute()
        {
            if (Effect.Delay > 0)
            {
                // Si l'effet a un délai (ex: piège qui se déclenche 1 tour plus tard),
                // on pose un TriggerBuff sur le lanceur qui appellera Apply() quand le délai expire
                this.AddTriggerBuff(Source, FightDispellableEnum.REALLY_NOT_DISPELLABLE, TriggerType.BUFF_DELAY_ENDED, delegate (TriggerBuff buff, TriggerType trigger, object token)
                    {
                        Apply(BaseTargets);
                        return false;
                    }, Effect.Delay);
            }
            else
            {
                // Pas de délai → on applique l'effet immédiatement
                Apply(BaseTargets);
            }
        }

        // ── Helpers pour poser des buffs ─────────────────────────────────────────
        // Ces méthodes sont utilisées dans Apply() des handlers pour appliquer des effets sur les cibles

        // Ajoute un buff de statistique (ex: +50 Force, -3 PA...)
        // value = la valeur du bonus/malus, caracteritic = la stat modifiée
        public StatBuff AddStatBuff(Fighter target, short value, Characteristic caracteritic, FightDispellableEnum dispelable)
        {
            int id = target.BuffIdProvider.Pop();
            StatBuff statBuff = new StatBuff(id, target, this.Source, this.SpellLevel, this.Effect, this.SpellId, value, caracteritic, this.Critical, dispelable);
            target.AddAndApplyBuff(statBuff, true);
            return statBuff;
        }

        // Surcharge avec un actionId personnalisé (pour l'affichage côté client)
        public StatBuff AddStatBuff(Fighter target, short value, Characteristic caracteritic, FightDispellableEnum dispelable, short customActionId)
        {
            int id = target.BuffIdProvider.Pop();
            StatBuff statBuff = new StatBuff(id, target, this.Source, this.SpellLevel, this.Effect, this.SpellId, value, caracteritic, this.Critical, dispelable, customActionId);
            target.AddAndApplyBuff(statBuff, true);
            return statBuff;
        }

        // Ajoute un TriggerBuff : un buff qui se déclenche quand un événement survient
        // trigger = l'événement déclencheur (BEFORE_ATTACKED, TURN_BEGIN, etc.)
        // applyTrigger = le callback à appeler quand l'événement survient
        // Note : delay = -1 signifie "pas de délai" (actif immédiatement)
        public TriggerBuff AddTriggerBuff(Fighter target, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuff.TriggerBuffApplyHandler applyTrigger)
        {
            int id = target.BuffIdProvider.Pop();
            TriggerBuff triggerBuff = new TriggerBuff(id, target, this.Source, this.SpellLevel, this.Effect, this.SpellId, this.Critical, dispelable, trigger, applyTrigger, -1);
            target.AddAndApplyBuff(triggerBuff, true);
            return triggerBuff;
        }
        // Version avec délai explicite — utilisée pour les effets différés (ex: piège, glyphe)
        // Le buff ne se déclenche qu'après N tours
        protected TriggerBuff AddTriggerBuff(Fighter target, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuff.TriggerBuffApplyHandler applyTrigger, short delay)
        {
            int id = target.BuffIdProvider.Pop();
            TriggerBuff triggerBuff = new TriggerBuff(id, target, this.Source, this.SpellLevel, this.Effect, this.SpellId, this.Critical, dispelable, trigger, applyTrigger, delay);
            target.AddAndApplyBuff(triggerBuff, true);
            return triggerBuff;
        }

        // Version avec un callback de suppression (RemoveTrigger appelé quand le buff expire/est dissipé)
        public TriggerBuff AddTriggerBuff(Fighter target, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuff.TriggerBuffApplyHandler applyTrigger, Symbioz.World.Providers.Fights.Buffs.TriggerBuff.TriggerBuffRemoveHandler removeTrigger)
        {
            int id = target.BuffIdProvider.Pop();
            TriggerBuff triggerBuff = new TriggerBuff(id, target, this.Source, this.SpellLevel, this.Effect, this.SpellId, this.Critical, dispelable, trigger, applyTrigger, removeTrigger, -1);
            target.AddAndApplyBuff(triggerBuff, true);
            return triggerBuff;
        }

        // Ajoute un buff de vitalité (modifie les HP max de la cible)
        public VitalityBuff AddVitalityBuff(Fighter target, FightDispellableEnum dispelable, short num)
        {
            int id = target.BuffIdProvider.Pop();
            VitalityBuff buff = new VitalityBuff(id, target, Source, this.SpellLevel, Effect, this.SpellId, num, this.Critical, dispelable);
            target.AddAndApplyBuff(buff);
            return buff;
        }

        // Ajoute un bouclier (absorbe une partie des dégâts reçus)
        public ShieldBuff AddShieldBuff(Fighter target, FightDispellableEnum dispelable, short num)
        {
            int id = target.BuffIdProvider.Pop();
            ShieldBuff buff = new ShieldBuff(id, target, Source, SpellLevel, Effect, SpellId, num, Critical, dispelable);
            target.AddAndApplyBuff(buff);
            return buff;
        }

        // Ajoute un buff de look (change l'apparence du combattant)
        public LookBuff AddLookBuff(Fighter target, FightDispellableEnum dispelable)
        {
            int id = target.BuffIdProvider.Pop();
            LookBuff buff = new LookBuff(id, target, Source, this.SpellLevel, Effect, this.SpellId, Critical, dispelable);
            target.AddAndApplyBuff(buff);
            return buff;
        }

        // Ajoute un buff d'état (ex: état "empoisonné", "ralenti"...)
        public StateBuff AddStateBuff(Fighter target, SpellStateRecord stateRecord, FightDispellableEnum dispelable)
        {
            int id = target.BuffIdProvider.Pop();
            StateBuff buff = new StateBuff(id, target, Source, this.SpellLevel, Effect, this.SpellId, Critical, dispelable, stateRecord);
            target.AddAndApplyBuff(buff);
            return buff;
        }

        // Surcharge avec une durée personnalisée (overrride la durée de la BDD)
        public StateBuff AddStateBuff(Fighter target, SpellStateRecord stateRecord, short duration, FightDispellableEnum dispelable)
        {
            int id = target.BuffIdProvider.Pop();
            StateBuff buff = new StateBuff(id, target, Source, this.SpellLevel, Effect, this.SpellId, Critical, dispelable, stateRecord)
            {
                Duration = duration
            };
            target.AddAndApplyBuff(buff);
            return buff;
        }
    }
}
