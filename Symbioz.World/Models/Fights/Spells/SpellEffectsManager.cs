using Symbioz.Core.DesignPattern;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Providers.Fights;
using Symbioz.World.Providers.Fights.Effects;
using Symbioz.World.Records.Spells;
using Symbioz.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Records.Items;
using Symbioz.Protocol.Selfmade.Enums;
using static Symbioz.World.Models.Fights.Fighters.Fighter;

namespace Symbioz.World.Models.Fights.Spells
{
    /// <summary>
    /// Gestionnaire central des effets de sorts en combat.
    /// Reçoit un sort lancé, détermine les cibles touchées dans la zone d'effet,
    /// sélectionne les effets actifs (normal ou critique, effets à probabilité),
    /// et délègue le traitement de chaque effet à SpellEffectsProvider.
    /// Fonctionne en singleton (une seule instance partagée).
    /// </summary>
    public class SpellEffectsManager : Singleton<SpellEffectsManager>
    {
        /// <summary>
        /// Traite tous les effets d'un sort lancé (version principale avec niveau de sort complet).
        /// Détermine la zone d'effet de chaque effet, trouve les cibles dans cette zone,
        /// puis appelle HandleEffect pour chaque effet.
        /// </summary>
        public void HandleEffects(Fighter fighter, SpellLevelRecord level, MapPoint castPoint, bool criticalHit, bool applyEffectTrigger = true)
        {
            // Sélectionne les effets normaux ou critiques selon le résultat du lancer
            EffectInstance[] effects = SelectEffects(fighter, criticalHit ? level.CriticalEffects : level.Effects, level, castPoint);

            foreach (var effect in effects)
            {
                Zone zone = effect.GetZone(fighter.Point.OrientationTo(castPoint));

                Fighter[] targets = GetAffectedFighters(fighter, zone, castPoint, effect.TargetMask).Distinct().ToArray();
                var cells = zone.GetCells(castPoint.CellId, fighter.Fight.Map);
                HandleEffect(fighter, level, castPoint, criticalHit, effect, targets);

            }
        }
        public void HandleEffects(Fighter fighter, EffectInstance[] effects, SpellLevelRecord level, MapPoint castPoint,
            string rawZone, string targetMask, bool criticalHit, bool applyEffectTrigger = true)
        {
            EffectInstance[] selectedEffects = SelectEffects(fighter, effects.ToList(), level, castPoint);

            foreach (var effect in selectedEffects)
            {
                Zone zone = new Zone(rawZone[0], byte.Parse(rawZone[1].ToString()), fighter.Point.OrientationTo(castPoint));

                Fighter[] targets = GetAffectedFighters(fighter, zone, castPoint, targetMask).Distinct().ToArray();

                HandleEffect(fighter, level, castPoint, criticalHit, effect, targets);
            }
        }
        public void HandleEffects(Fighter fighter, EffectInstance[] effects, SpellLevelRecord level, MapPoint castPoint,
            string rawZone, bool criticalHit, bool applyEffectTrigger = true)
        {
            EffectInstance[] selectedEffects = SelectEffects(fighter, effects.ToList(), level, castPoint);

            foreach (var effect in selectedEffects)
            {
                Zone zone = new Zone(rawZone[0], byte.Parse(rawZone[1].ToString()), fighter.Point.OrientationTo(castPoint));

                Fighter[] targets = GetAffectedFighters(fighter, zone, castPoint, effect.TargetMask).Distinct().ToArray();

                HandleEffect(fighter, level, castPoint, criticalHit, effect, targets);
            }
        }
        public void HandleEffects(Fighter fighter, EffectInstance[] effects, SpellLevelRecord level, MapPoint castPoint,
          bool criticalHit, bool applyEffectTrigger = true)
        {
            EffectInstance[] selectedEffects = SelectEffects(fighter, effects.ToList(), level, castPoint);

            foreach (var effect in selectedEffects)
            {
                Zone zone = effect.GetZone(fighter.Point.OrientationTo(castPoint));

                Fighter[] targets = GetAffectedFighters(fighter, zone, castPoint, effect.TargetMask).Distinct().ToArray();

                HandleEffect(fighter, level, castPoint, criticalHit, effect, targets);
            }
        }
        public void HandleEffect(Fighter fighter, SpellLevelRecord level, MapPoint castPoint, bool criticalHit, EffectInstance effect, Fighter[] targets)
        {
            if (effect.TriggerTypes != TriggerType.AFTER_DEATH && effect.TriggerTypes != TriggerType.TURN_END) // The only trigger handled for the moment (complicated to handle with Symbioz fight architecture)
            {
                SpellEffectsProvider.Handle(fighter, level, effect, targets, castPoint, criticalHit);
            }
        }
      
       
        /// <summary>
        /// Sélectionne les effets à appliquer parmi la liste complète :
        /// - Les effets non-aléatoires (Random == 0) sont tous inclus.
        /// - Parmi les effets aléatoires (Random != 0), un seul est tiré au sort.
        /// Cela permet d'avoir des sorts avec "soit cet effet, soit cet autre effet".
        /// </summary>
        private EffectInstance[] SelectEffects(Fighter source, List<EffectInstance> effects, SpellLevelRecord level, MapPoint castPoint)
        {
            List<EffectInstance> results = new List<EffectInstance>();

            // Ajoute tous les effets garantis (sans probabilité)
            results.AddRange(effects.FindAll(x => x.Random == 0));

            // Parmi les effets aléatoires, en tire un seul au hasard
            List<EffectInstance> randomEffects = effects.FindAll(x => x.Random != 0);

            if (randomEffects.Count > 0)
                results.Add(randomEffects.Random());

            return results.ToArray();
        }
        /// <summary>
        /// Détermine quels combattants sont affectés par un sort dans une zone donnée.
        /// 1. Calcule les cellules de la zone d'effet.
        /// 2. Filtre les combattants selon le masque de cibles (alliés, ennemis, soi-même...).
        /// 3. Applique des sélections personnalisées supplémentaires (ex: seulement le plus proche).
        /// </summary>
        public Fighter[] GetAffectedFighters(Fighter fighter, Zone zone, MapPoint castPoint, string targetMask)
        {
            // Récupère toutes les cellules de la zone d'effet
            short[] cells = zone.GetCells(castPoint.CellId, fighter.Fight.Map);

            List<Fighter> targets = new List<Fighter>();
            List<Fighter> filtreds = new List<Fighter>();

            // Récupère la liste de tous les combattants autorisés selon le masque de cibles
            foreach (var mask in targetMask.Split(TargetMaskSelector.TARGET_MASK_SPLITTER))
            {
                filtreds.AddRange(TargetMaskProvider.Handle(fighter, mask));
            }

            // Ne garde que les combattants présents sur une cellule de la zone ET autorisés
            foreach (var cell in cells)
            {
                Fighter target = fighter.Fight.GetFighter(cell);

                if (target != null && filtreds.Contains(target))
                    targets.Add(target);
            }

            // Applique d'éventuels filtres supplémentaires (ex: seulement le combattant le plus proche)
            return TargetMaskSelector.Custom(fighter, targetMask, TargetMaskSelector.Select(fighter, targets, targetMask), castPoint).ToArray();
        }
    }
}
