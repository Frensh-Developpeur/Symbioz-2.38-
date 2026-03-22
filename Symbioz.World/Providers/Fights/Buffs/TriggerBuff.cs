using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
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
    // Un TriggerBuff est un buff qui ne fait rien immédiatement
    // mais qui se déclenche automatiquement quand un événement précis survient en combat
    // Exemples :
    //   - BEFORE_ATTACKED  → se déclenche juste avant que la cible reçoit des dégâts (ex: Vulnérabilité)
    //   - TURN_BEGIN       → se déclenche au début du tour de la cible (ex: poison)
    //   - BUFF_DELAY_ENDED → se déclenche quand le délai expire (ex: piège)
    public class TriggerBuff : Buff
    {
        // Type de callback pour l'application du buff
        // Paramètres : le buff lui-même, le type de trigger, un token (ex: l'objet Damage)
        // Retourne true si le buff doit être consommé/supprimé après déclenchement
        public delegate bool TriggerBuffApplyHandler(TriggerBuff buff, TriggerType trigger, object token);

        // Type de callback pour la suppression du buff (quand il expire ou est dissipé)
        public delegate void TriggerBuffRemoveHandler(TriggerBuff buff);

        // L'événement qui déclenche ce buff (BEFORE_ATTACKED, TURN_BEGIN, etc.)
        public TriggerType Trigger
        {
            get;
            private set;
        }

        // Le callback à appeler quand le trigger se produit
        public TriggerBuffApplyHandler ApplyTrigger
        {
            get;
            private set;
        }

        // Callback optionnel appelé quand le buff est supprimé/dissipé
        public TriggerBuffRemoveHandler RemoveTrigger
        {
            get;
            private set;
        }

        // Le délai en tours avant que le buff devienne actif
        // -1 = actif immédiatement, 0 = actif ce tour, N = actif dans N tours
        public short Delay
        {
            get;
            private set;
        }

        // Constructeur de base : sans callback de suppression
        public TriggerBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, bool critical, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuffApplyHandler applyTrigger, short delay)
            : base(id, target, caster, level, effect, spellId, critical, dispelable)
        {
            this.Trigger = trigger;
            this.ApplyTrigger = applyTrigger;
            this.Delay = delay;
        }

        // Constructeur avec callback de suppression (RemoveTrigger)
        public TriggerBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, bool critical, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuffApplyHandler applyTrigger, TriggerBuffRemoveHandler removeTrigger, short delay)
            : base(id, target, caster, level, effect, spellId, critical, dispelable)
        {
            this.Trigger = trigger;
            this.ApplyTrigger = applyTrigger;
            this.RemoveTrigger = removeTrigger;
            this.Delay = delay;
        }

        // Constructeur avec actionId personnalisé (pour l'affichage de l'icône côté client)
        public TriggerBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, bool critical, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuffApplyHandler applyTrigger, short delay, short customActionId)
            : base(id, target, caster, level, effect, spellId, critical, dispelable, customActionId)
        {
            this.Trigger = trigger;
            this.ApplyTrigger = applyTrigger;
            this.Delay = delay;
        }

        // Constructeur complet avec suppression + actionId personnalisé
        public TriggerBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, bool critical, FightDispellableEnum dispelable, TriggerType trigger, TriggerBuffApplyHandler applyTrigger, TriggerBuffRemoveHandler removeTrigger, short delay, short customActionId)
            : base(id, target, caster, level, effect, spellId, critical, dispelable, customActionId)
        {
            this.Trigger = trigger;
            this.ApplyTrigger = applyTrigger;
            this.RemoveTrigger = removeTrigger;
            this.Delay = delay;
        }

        // Décrémente le délai d'un tour — retourne true quand le délai atteint 0 (buff actif)
        public bool DecrementDelay()
        {
            return (this.Delay -= 1) == 0;
        }

        // Retourne true si ce buff a un délai actif (pas encore déclenché)
        public bool IsDelayed()
        {
            return this.Effect.Delay > 0;
        }

        // Indique que c'est bien un TriggerBuff (utilisé pour les vérifications de type)
        public override bool IsTrigger()
        {
            return true;
        }

        // Retourne le délai restant (utilisé pour savoir si le buff est actif ou pas encore)
        public override short GetDelay()
        {
            return this.Delay;
        }

        // Apply() sans trigger ni token — appelé si le buff a TriggerType.ON_CAST
        // Dans la plupart des cas, le buff se déclenche via Apply(trigger, token) ci-dessous
        public override void Apply()
        {
            if (this.ApplyTrigger != null)
            {
                this.ApplyTrigger(this, TriggerType.UNKNOWN, null);
            }
        }

        // Apply() avec le type de trigger — appelé par Fighter.TriggerBuffs()
        // Retourne true si le buff doit être consommé (supprimé après déclenchement)
        public bool Apply(TriggerType trigger)
        {
            if (this.ApplyTrigger != null)
            {
                return this.ApplyTrigger(this, trigger, null);
            }
            return false;
        }

        // Apply() avec trigger + token — version complète, utilisée pour BEFORE_ATTACKED
        // token contient les données utiles (ex: l'objet Damage pour modifier les dégâts)
        public bool Apply(TriggerType trigger, object token)
        {
            if (this.ApplyTrigger != null)
            {
                return this.ApplyTrigger(this, trigger, token);
            }
            return false;
        }

        // Appelé quand le buff est dissipé ou expire
        // Exécute le callback de suppression si défini
        public override void Dispell()
        {
            if (this.RemoveTrigger != null)
            {
                this.RemoveTrigger(this);
            }
        }

        // Construit le message réseau à envoyer au client pour qu'il affiche l'icône du buff dans la barre de buffs.
        //
        // POURQUOI FightTemporaryBoostEffect et pas FightTriggeredEffect ?
        //   Il existe deux types de messages pour décrire un buff :
        //     - FightTriggeredEffect  (TypeId=210) : prévu pour les buffs déclenchés sur condition
        //     - FightTemporaryBoostEffect (TypeId=209) : prévu pour les boosts de stats temporaires
        //   Le client Dofus 2.38 ne sait pas afficher FightTriggeredEffect dans la barre de buffs.
        //   Il reçoit le message mais n'affiche rien. On utilise donc FightTemporaryBoostEffect
        //   même pour les TriggerBuffs, uniquement pour que le client affiche l'icône correctement.
        //   La mécanique côté serveur (déclenchement sur AFTER_ATTACKED, etc.) n'est pas affectée.
        //
        // POURQUOI effectId=0 et parentBoostUid=0 ?
        //   Le champ parentBoostUid sert à dire au client "ce buff est un sous-effet d'un autre buff".
        //   Si parentBoostUid != 0, le client cherche le buff parent correspondant.
        //   S'il ne le trouve pas (ce qui est toujours le cas ici), il n'affiche pas l'icône.
        //   En mettant 0, on dit au client "ce buff est indépendant, affiche-le normalement".
        //
        // RÈGLE : si tu crées un nouveau TriggerBuff et qu'il n'affiche pas d'icône,
        //         vérifie que GetAbstractFightDispellableEffect() suit ce même pattern.
        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect((uint)base.Id, base.Target.Id, (short)base.Duration, (sbyte)Dispelable, (ushort)base.SpellId, 0, 0, (short)Effect.DiceMin);
        }
    }
}
