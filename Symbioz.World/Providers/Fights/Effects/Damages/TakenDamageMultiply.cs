using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Damages;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Damages
{
    // Cet attribut dit au SpellEffectsProvider que cette classe gère l'effet Effect_TakenDamageMultiply (ID 1163)
    // Exemple : sort Vulnérabilité du Pandawa (sort 691) — la cible reçoit plus de dégâts pendant X tours
    [SpellEffectHandler(EffectsEnum.Effect_TakenDamageMultiply)]
    public class TakenDamageMultiply : SpellEffectHandler
    {
        // Le multiplicateur de dégâts calculé depuis la BDD
        // Ex: DiceMin = 109 → Ratio = 1.09 → la cible reçoit 9% de dégâts supplémentaires
        public double Ratio
        {
            get;
            private set;
        }

        // Constructeur appelé automatiquement par SpellEffectsProvider via Activator.CreateInstance
        // Il reçoit toutes les infos du sort : qui lance, quel sort, quel effet, quelles cibles, où, critique ?
        public TakenDamageMultiply(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
             Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {
            // On lit la valeur DiceMin dans la BDD (ex: 109) et on la divise par 100 pour avoir un ratio (1.09)
            this.Ratio = ((double)Effect.DiceMin / (double)100);
        }

        // Apply() est appelé par Execute() dans SpellEffectHandler
        // C'est ici qu'on pose le buff sur chaque cible touchée
        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                // On ajoute un TriggerBuff sur la cible :
                // - DISPELLABLE = le buff peut être dissipé
                // - BEFORE_ATTACKED = le callback BeforeAttacked sera appelé juste AVANT que la cible reçoive des dégâts
                this.AddTriggerBuff(target, FightDispellableEnum.DISPELLABLE, TriggerType.BEFORE_ATTACKED, BeforeAttacked);
            }
            return true;
        }

        // Callback déclenché juste avant que la cible reçoive des dégâts (TriggerType.BEFORE_ATTACKED)
        // token contient l'objet Damage avec le montant de dégâts en cours
        private bool BeforeAttacked(TriggerBuff buff, TriggerType trigger, object token)
        {
            // On récupère l'objet Damage passé en token
            Damage damages = (Damage)token;

            // On multiplie les dégâts par le ratio (ex: 100 dégâts * 1.09 = 109 dégâts)
            double num = (double)damages.Delta * Ratio;

            // On modifie directement l'objet Damage — les dégâts finaux seront augmentés
            damages.Delta = (short)num;

            // Retourner false = on ne consume pas le buff (il reste actif pour les prochains tours)
            return false;
        }
    }
}
