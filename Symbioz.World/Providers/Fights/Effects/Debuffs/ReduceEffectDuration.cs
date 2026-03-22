using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Debuffs
{
    /// <summary>
    /// Réduit la durée de tous les buffs dissipables d'une cible de DiceMin tours.
    /// Si un buff tombe à 0 tours (ou moins) et qu'il n'est pas infini (Duration != -1),
    /// il est immédiatement supprimé. Envoie un message au client pour mettre à jour l'affichage.
    /// Ne réduit pas les buffs encore en délai (GetDelay() > 0).
    /// </summary>
    [SpellEffectHandler(Protocol.Selfmade.Enums.EffectsEnum.Effect_ReduceEffectsDuration)]
    public class ReduceEffectDuration : SpellEffectHandler
    {
        public ReduceEffectDuration(Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
             Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, spellLevel, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            foreach (var target in targets)
            {
                foreach (var buff in target.GetDispelableBuffs())
                {
                    // Ignore les buffs encore en attente de déclenchement (ex: piège non activé)
                    if (buff.GetDelay() == 0)
                    {
                        buff.Duration -= (short)Effect.DiceMin;

                        // Si le buff est épuisé et pas infini, on le supprime immédiatement
                        if (buff.Duration <= 0 && buff.Effect.Duration != -1)
                        {
                            target.RemoveAndDispellBuff(buff);
                        }
                    }
                }
                // Notifie le client de la réduction de durée (affichage des tours restants)
                Fight.Send(new GameActionFightModifyEffectsDurationMessage(0, Source.Id, target.Id,
                   (short)-Effect.DiceMin));
            }
            return true;
        }
    }
}
