using Symbioz.Core.DesignPattern;
using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Items;
using Symbioz.World.Network;
using Symbioz.World.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Records.Monsters;

namespace Symbioz.World.Providers.Items
{
    /// <summary>
    /// Gère les effets des équipements sur les statistiques d'un personnage.
    /// Chaque méthode est marquée [ItemEffect(EffectsEnum.XXX)] — quand un joueur équipe
    /// ou déséquipe un item, AddEffects/RemoveEffects est appelé automatiquement.
    /// "delta" = la valeur de l'effet venant de la base de données (ex: +50 Force → delta = 50).
    /// Les effets modifient toujours le champ "Objects" de la stat (= bonus équipements).
    /// </summary>
    public class ItemEffectsProvider
    {
        // Effets volontairement ignorés (pas de handler prévu)
        public static EffectsEnum[] UnhandledEffects = new EffectsEnum[]
        {
            EffectsEnum.Effect_PetFeedQuantity,
            EffectsEnum.Effect_410, // Effet inconnu, aucune logique identifiée
            EffectsEnum.Effect_412, // Effet inconnu, aucune logique identifiée
        };

        // Dictionnaire construit au démarrage : associe chaque [ItemEffect] à sa méthode
        public static readonly Dictionary<ItemEffectAttribute, MethodInfo> Handlers = typeof(ItemEffectsProvider).MethodsWhereAttributes<ItemEffectAttribute>();

        // Appelé quand un joueur EQUIPE un item → applique tous ses effets (+delta)
        public static void AddEffects(Character character, List<Effect> effects)
        {
            foreach (EffectInteger effect in effects.FindAll(x => x is EffectInteger))
            {
                Handle(character, effect.EffectEnum, (short)effect.Value);
            }
        }

        // Appelé quand un joueur DESEQUIPE un item → annule tous ses effets (-delta)
        public static void RemoveEffects(Character character, List<Effect> effects)
        {
            foreach (EffectInteger effect in effects.FindAll(x => x is EffectInteger))
            {
                Handle(character, effect.EffectEnum, (short)(-effect.Value));
            }
        }

        // Trouve et exécute le handler correspondant à l'effet.
        // Si aucun handler n'existe et que le joueur est Fondateur → message d'avertissement.
        private static void Handle(Character character, EffectsEnum effectEnum, short value)
        {
            if (!UnhandledEffects.Contains(effectEnum))
            {
                var handler = Handlers.FirstOrDefault(x => x.Key.Effect == effectEnum);
                if (handler.Value != null)
                {
                    handler.Value.Invoke(null, new object[] { character, value });
                }
                else if (character.Client.Account.Role == ServerRoleEnum.Fondator)
                {
                    character.Reply(effectEnum + " is not handled.");
                }
            }
        }

        // ── Points d'Action / Points de Mouvement ────────────────────────────────

        // Bonus de PA via équipement (id 111 = variante du protocole)
        [ItemEffect(EffectsEnum.Effect_AddAP_111)]
        public static void AddAp111(Character character, short delta)
        {
            character.Record.Stats.ActionPoints.Objects += delta;
        }

        // Bonus de PM via équipement (id 128 = variante du protocole)
        [ItemEffect(EffectsEnum.Effect_AddMP_128)]
        public static void AddMp128(Character character, short delta)
        {
            character.Record.Stats.MovementPoints.Objects += delta;
        }

        // Bonus de PM via équipement (variante standard)
        [ItemEffect(EffectsEnum.Effect_AddMP)]
        public static void AddMp(Character character, short delta)
        {
            character.Record.Stats.MovementPoints.Objects += delta;
        }

        // ── Force ────────────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddStrength)]
        public static void AddStrength(Character character, short delta)
        {
            character.Record.Stats.Strength.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubStrength)]
        public static void SubStrength(Character character, short delta)
        {
            character.Record.Stats.Strength.Objects -= delta;
        }

        // ── Chance ───────────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddChance)]
        public static void AddChance(Character character, short delta)
        {
            character.Record.Stats.Chance.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubChance)]
        public static void SubChance(Character character, short delta)
        {
            character.Record.Stats.Chance.Objects -= delta;
        }

        // ── Intelligence ─────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddIntelligence)]
        public static void AddIntelligence(Character character, short delta)
        {
            character.Record.Stats.Intelligence.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubIntelligence)]
        public static void SubIntelligence(Character character, short delta)
        {
            character.Record.Stats.Intelligence.Objects -= delta;
        }

        // ── Agilité ──────────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddAgility)]
        public static void AddAgility(Character character, short delta)
        {
            character.Record.Stats.Agility.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubAgility)]
        public static void SubAgility(Character character, short delta)
        {
            character.Record.Stats.Agility.Objects -= delta;
        }

        // ── Sagesse ──────────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddWisdom)]
        public static void AddWisdom(Character character, short delta)
        {
            character.Record.Stats.Wisdom.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubWisdom)]
        public static void SubWisdom(Character character, short delta)
        {
            character.Record.Stats.Wisdom.Objects -= delta;
        }

        // ── Vitalité ─────────────────────────────────────────────────────────────

        // Ajoute de la vitalité ET met à jour les HP actuels et max
        [ItemEffect(EffectsEnum.Effect_AddVitality)]
        public static void AddVitality(Character character, short delta)
        {
            character.Record.Stats.Vitality.Objects += delta;
            character.Record.Stats.LifePoints += delta;
            character.Record.Stats.MaxLifePoints += delta;
        }

        // Alias de AddVitality (même effet, id différent dans le protocole)
        [ItemEffect(EffectsEnum.Effect_AddHealth)]
        public static void AddHealth(Character character, short delta)
        {
            AddVitality(character, delta);
        }

        // Retire de la vitalité ET met à jour les HP actuels et max
        [ItemEffect(EffectsEnum.Effect_SubVitality)]
        public static void SubVitality(Character character, short delta)
        {
            character.Record.Stats.Vitality.Objects -= delta;
            character.Record.Stats.LifePoints -= delta;
            character.Record.Stats.MaxLifePoints -= delta;
        }

        // ── Esquive / Tacle ──────────────────────────────────────────────────────
        // Esquive (TackleEvade) : permet de s'éloigner d'un ennemi sans perdre de PA/PM.
        // Tacle (TackleBlock) : empêche un ennemi de s'éloigner sans perdre de PA/PM.

        [ItemEffect(EffectsEnum.Effect_AddDodge)]
        public static void AddDodge(Character character, short delta)
        {
            character.Record.Stats.TackleEvade.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubDodge)]
        public static void SubDodge(Character character, short delta)
        {
            character.Record.Stats.TackleEvade.Objects -= delta;
        }
        [ItemEffect(EffectsEnum.Effect_AddLock)]
        public static void AddLock(Character character, short delta)
        {
            character.Record.Stats.TackleBlock.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubLock)]
        public static void SubLock(Character character, short delta)
        {
            character.Record.Stats.TackleBlock.Objects -= delta;
        }

        // ── Résistance perte PA/PM ───────────────────────────────────────────────
        // Réduit la probabilité de perdre des PA/PM lors d'un tacle ennemi.

        [ItemEffect(EffectsEnum.Effect_IncreaseAPAvoid)]
        public static void IncreaseAPAvoid(Character character, short delta)
        {
            character.Record.Stats.DodgePAProbability.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubDodgeAPProbability)]
        public static void SubDodgeAPProbability(Character character, short delta)
        {
            character.Record.Stats.DodgePAProbability.Objects -= delta;
        }
        [ItemEffect(EffectsEnum.Effect_IncreaseMPAvoid)]
        public static void IncreaseMPAvoid(Character character, short delta)
        {
            character.Record.Stats.DodgePMProbability.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubDodgeMPProbability)]
        public static void SubDodgeMPProbability(Character character, short delta)
        {
            character.Record.Stats.DodgePMProbability.Objects -= delta;
        }

        // ── Initiative ───────────────────────────────────────────────────────────
        // Détermine l'ordre de jeu au début d'un combat

        [ItemEffect(EffectsEnum.Effect_AddInitiative)]
        public static void AddInitiative(Character character, short delta)
        {
            character.Record.Stats.Initiative.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubInitiative)]
        public static void SubInitiative(Character character, short delta)
        {
            character.Record.Stats.Initiative.Objects -= delta;
        }

        // ── Portée ───────────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddRange)]
        public static void AddRange(Character character, short delta)
        {
            character.Record.Stats.Range.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubRange)]
        public static void SubRange(Character character, short delta)
        {
            character.Record.Stats.Range.Objects -= delta;
        }

        // ── Coup Critique ────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddCriticalHit)]
        public static void AddCriticalHit(Character character, short delta)
        {
            character.Record.Stats.CriticalHit.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubCriticalHit)]
        public static void SubCriticalHit(Character character, short delta)
        {
            character.Record.Stats.CriticalHit.Objects -= delta;
        }

        // ── Dommages Bonus ───────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddDamageBonus)]
        public static void AddDamagesBonus(Character character, short delta)
        {
            character.Record.Stats.AllDamagesBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubDamageBonus)]
        public static void SubDamagesBonus(Character character, short delta)
        {
            character.Record.Stats.AllDamagesBonus.Objects -= delta;
        }

        // ── Dommages Pièges (Roublard) ───────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddTrapBonus)]
        public static void AddTrapBonus(Character character, short delta)
        {
            character.Record.Stats.TrapBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_AddTrapBonusPercent)]
        public static void AddTrapBonusPercent(Character character, short delta)
        {
            character.Record.Stats.TrapBonusPercent.Objects += delta;
        }

        // Bonus dommages en % (variante id 138 du protocole)
        [ItemEffect(EffectsEnum.Effect_IncreaseDamage_138)]
        public static void IncreaseDamage138(Character character, short delta)
        {
            character.Record.Stats.DamagesBonusPercent.Objects += delta;
        }

        // ── Prospection ──────────────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddProspecting)]
        public static void AddProspecting(Character character, short delta)
        {
            character.Record.Stats.Prospecting.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubProspecting)]
        public static void SubProspecting(Character character, short delta)
        {
            character.Record.Stats.Prospecting.Objects -= delta;
        }

        // ── Limite d'invocations ─────────────────────────────────────────────────

        [ItemEffect(EffectsEnum.Effect_AddSummonLimit)]
        public static void AddSummonLimit(Character character, short delta)
        {
            character.Record.Stats.SummonableCreaturesBoost.Objects += delta;
        }



        // ── Résistances élémentaires % (PvM + PvP) ──────────────────────────────
        // Ces résistances réduisent les dégâts reçus d'un élément donné, en pourcentage.
        // La version "Pvp" ne s'applique qu'en combat JcJ.

        // Résistance Neutre %
        [ItemEffect(EffectsEnum.Effect_AddNeutralResistPercent)]
        public static void AddNeutralResistPercent(Character character, short delta)
        {
            character.Record.Stats.NeutralResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubNeutralResistPercent)]
        public static void SubNeutralResistPercent(Character character, short delta)
        {
            character.Record.Stats.NeutralResistPercent.Objects -= delta;
        }
        // Résistance Neutre % (PvP uniquement)
        [ItemEffect(EffectsEnum.Effect_AddPvpNeutralResistPercent)]
        public static void AddPvpNeutralResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPNeutralResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPvpNeutralResistPercent)]
        public static void SubPvpNeutralResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPNeutralResistPercent.Objects -= delta;
        }

        // Résistance Air %
        [ItemEffect(EffectsEnum.Effect_AddAirResistPercent)]
        public static void AddAirResistPercent(Character character, short delta)
        {
            character.Record.Stats.AirResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubAirResistPercent)]
        public static void SubAirResistPercent(Character character, short delta)
        {
            character.Record.Stats.AirResistPercent.Objects -= delta;
        }
        // Résistance Air % (PvP uniquement)
        [ItemEffect(EffectsEnum.Effect_AddPvpAirResistPercent)]
        public static void AddPvpAirResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPAirResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPvpAirResistPercent)]
        public static void SubPvpAirResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPAirResistPercent.Objects -= delta;
        }

        // Résistance Eau %
        [ItemEffect(EffectsEnum.Effect_AddWaterResistPercent)]
        public static void AddWaterResistPercent(Character character, short delta)
        {
            character.Record.Stats.WaterResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubWaterResistPercent)]
        public static void SubWaterResistPercent(Character character, short delta)
        {
            character.Record.Stats.WaterResistPercent.Objects -= delta;
        }
        // Résistance Eau % (PvP uniquement)
        [ItemEffect(EffectsEnum.Effect_AddPvpWaterResistPercent)]
        public static void AddPvpWaterResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPWaterResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPvpWaterResistPercent)]
        public static void SubPvpWaterResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPWaterResistPercent.Objects -= delta;
        }

        // Résistance Feu %
        [ItemEffect(EffectsEnum.Effect_AddFireResistPercent)]
        public static void AddFireResistPercent(Character character, short delta)
        {
            character.Record.Stats.FireResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubFireResistPercent)]
        public static void SubFireResistPercent(Character character, short delta)
        {
            character.Record.Stats.FireResistPercent.Objects -= delta;
        }
        // Résistance Feu % (PvP uniquement)
        [ItemEffect(EffectsEnum.Effect_AddPvpFireResistPercent)]
        public static void AddPvpFireResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPFireResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPvpFireResistPercent)]
        public static void SubPvpFireResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPFireResistPercent.Objects -= delta;
        }

        // Résistance Terre %
        [ItemEffect(EffectsEnum.Effect_AddEarthResistPercent)]
        public static void AddEarthResistPercent(Character character, short delta)
        {
            character.Record.Stats.EarthResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubEarthResistPercent)]
        public static void SubEarthResistPercent(Character character, short delta)
        {
            character.Record.Stats.EarthResistPercent.Objects -= delta;
        }
        // Résistance Terre % (PvP uniquement)
        [ItemEffect(EffectsEnum.Effect_AddPvpEarthResistPercent)]
        public static void AddPvpEarthResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPEarthResistPercent.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPvpEarthResistPercent)]
        public static void SubPvpEarthResistPercent(Character character, short delta)
        {
            character.Record.Stats.PvPEarthResistPercent.Objects -= delta;
        }

        // ── Résistances fixes (valeur brute, pas en %) ───────────────────────────
        // Réduisent les dégâts d'un élément d'une valeur fixe (ex: -10 dégâts feu reçus).
        // Moins courants que les % mais présents sur certains équipements haut niveau.

        [ItemEffect(EffectsEnum.Effect_AddEarthElementReduction)]
        public static void EarthElementReduction(Character character, short delta)
        {
            character.Record.Stats.EarthReduction.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_AddFireElementReduction)]
        public static void FireElementReduction(Character character, short delta)
        {
            character.Record.Stats.FireReduction.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_AddWaterElementReduction)]
        public static void WaterElementReduction(Character character, short delta)
        {
            character.Record.Stats.WaterReduction.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_AddAirElementReduction)]
        public static void AirElementReduction(Character character, short delta)
        {
            character.Record.Stats.AirReduction.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_AddNeutralElementReduction)]
        public static void NeutralElementReduction(Character character, short delta)
        {
            character.Record.Stats.NeutralReduction.Objects += delta;
        }

        // ── Dommages de poussée ──────────────────────────────────────────────────
        // Augmente/réduit les dégâts infligés ou reçus lors d'une poussée (sort ou contact).

        [ItemEffect(EffectsEnum.Effect_AddPushDamageBonus)]
        public static void AddPushDamageBonus(Character character, short delta)
        {
            character.Record.Stats.PushDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPushDamageBonus)]
        public static void SubPushDamageBonus(Character character, short delta)
        {
            character.Record.Stats.PushDamageBonus.Objects -= delta;
        }
        // Réduction des dégâts reçus par poussée
        [ItemEffect(EffectsEnum.Effect_AddPushDamageReduction)]
        public static void AddPushDamageReduction(Character character, short delta)
        {
            character.Record.Stats.PushDamageReduction.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubPushDamageReduction)]
        public static void SubPushDamageReduction(Character character, short delta)
        {
            character.Record.Stats.PushDamageReduction.Objects -= delta;
        }

        // ── Dommages Critiques ───────────────────────────────────────────────────
        // Bonus/réduction sur les dégâts infligés ou reçus lors d'un coup critique.

        [ItemEffect(EffectsEnum.Effect_AddCriticalDamageBonus)]
        public static void AddCriticalDamageBonus(Character character, short delta)
        {
            character.Record.Stats.CriticalDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubCriticalDamageBonus)]
        public static void SubCriticalDamageBonus(Character character, short delta)
        {
            character.Record.Stats.CriticalDamageBonus.Objects -= delta;
        }
        // Réduction des dégâts reçus sur un coup critique
        [ItemEffect(EffectsEnum.Effect_AddCriticalDamageReduction)]
        public static void AddCriticalDamageReduction(Character character, short delta)
        {
            character.Record.Stats.CriticalDamageReduction.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubCriticalDamageReduction)]
        public static void SubCriticalDamageReduction(Character character, short delta)
        {
            character.Record.Stats.CriticalDamageReduction.Objects -= delta;
        }

        // ── Dommages élémentaires bonus ──────────────────────────────────────────
        // Ajoute une valeur fixe aux dégâts infligés avec l'élément correspondant.
        // S'applique EN PLUS du multiplicateur de caractéristique (Force/Intelligence/etc.).

        // Eau
        [ItemEffect(EffectsEnum.Effect_AddWaterDamageBonus)]
        public static void WaterDamageBonus(Character character, short delta)
        {
            character.Record.Stats.WaterDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubWaterDamageBonus)]
        public static void SubWaterDamageBonus(Character character, short delta)
        {
            character.Record.Stats.WaterDamageBonus.Objects -= delta;
        }
        // Air
        [ItemEffect(EffectsEnum.Effect_AddAirDamageBonus)]
        public static void AirDamageBonus(Character character, short delta)
        {
            character.Record.Stats.AirDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubAirDamageBonus)]
        public static void SubAirDamageBonus(Character character, short delta)
        {
            character.Record.Stats.AirDamageBonus.Objects -= delta;
        }
        // Terre
        [ItemEffect(EffectsEnum.Effect_AddEarthDamageBonus)]
        public static void EarthDamageBonus(Character character, short delta)
        {
            character.Record.Stats.EarthDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubEarthDamageBonus)]
        public static void SubEarthDamageBonus(Character character, short delta)
        {
            character.Record.Stats.EarthDamageBonus.Objects -= delta;
        }
        // Neutre
        [ItemEffect(EffectsEnum.Effect_AddNeutralDamageBonus)]
        public static void NeutralDamageBonus(Character character, short delta)
        {
            character.Record.Stats.NeutralDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubNeutralDamageBonus)]
        public static void SubNeutralDamageBonus(Character character, short delta)
        {
            character.Record.Stats.NeutralDamageBonus.Objects -= delta;
        }
        // Feu
        [ItemEffect(EffectsEnum.Effect_AddFireDamageBonus)]
        public static void FireDamageBonus(Character character, short delta)
        {
            character.Record.Stats.FireDamageBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubFireDamageBonus)]
        public static void SubFireDamageBonus(Character character, short delta)
        {
            character.Record.Stats.FireDamageBonus.Objects -= delta;
        }

        // ── Bonus de soin ────────────────────────────────────────────────────────
        // Augmente les HP rendus par les sorts de soin du personnage.

        [ItemEffect(EffectsEnum.Effect_AddHealBonus)]
        public static void HealBonus(Character character, short delta)
        {
            character.Record.Stats.HealBonus.Objects += delta;
        }
        [ItemEffect(EffectsEnum.Effect_SubHealBonus)]
        public static void SubHealBonus(Character character, short delta)
        {
            character.Record.Stats.HealBonus.Objects -= delta;
        }

        // ── Effets spéciaux ──────────────────────────────────────────────────────

        // Emote : delta > 0 → apprend l'émote, delta < 0 → la retire.
        // Utilisé par les costumes ou cadeaux qui débloquent des animations.
        [ItemEffect(EffectsEnum.Effect_Emote)]
        public static void Emote(Character character, short delta)
        {
            if (delta > 0)
                character.LearnEmote((byte)delta);
            else
                character.RemoveEmote((byte)Math.Abs(delta));
        }

        // Titre : effet non encore implémenté (à faire).
        [ItemEffect(EffectsEnum.Effect_Title)]
        public static void Title(Character character, short delta)
        {
            // Todo
        }

        // Familier visible : delta > 0 → ajoute un monstre qui suit le joueur,
        // delta < 0 → le retire. Utilisé par les familiers équipés.
        [ItemEffect(EffectsEnum.Effect_Followed)]
        public static void Followed(Character character, short delta)
        {
            var monsterTemplate = MonsterRecord.GetMonster((ushort)Math.Abs(delta));

            if (delta > 0)
            {
                character.AddFollower(monsterTemplate.Look);
            }
            else
            {
                character.RemoveFollower(monsterTemplate.Look);
            }
        }



    }
}
