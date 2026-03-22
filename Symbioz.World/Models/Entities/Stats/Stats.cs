using Symbioz.Core.DesignPattern.StartupEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Records;
using Symbioz.Protocol.Types;
using Symbioz.Protocol.Selfmade.Enums;
using System.Reflection;
using YAXLib;
using Symbioz.World.Records.Breeds;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Providers;

namespace Symbioz.World.Models.Entities.Stats
{
    /// <summary>
    /// Statistiques complètes d'un personnage ou d'un combattant.
    /// Contient toutes les caractéristiques du jeu (PA, PM, résistances, dégâts, éléments...).
    ///
    /// Stats sert de modèle persistant (sauvegardé en base de données pour les personnages).
    /// FighterStats en hérite pour les stats temporaires en combat (HP courants, bouclier, érosion...).
    ///
    /// Chaque caractéristique est un objet Characteristic avec 4 parties : Base, Additional, Objects, Context.
    /// </summary>
    public class Stats
    {
        // PA de départ pour un nouveau personnage (6 PA par défaut dans Dofus 2.x)
        public const short StartActionPoints = 6;

        // PM de départ pour un nouveau personnage (3 PM par défaut dans Dofus 2.x)
        public const ushort StartMovementPoints = 3;

        // Points de vie actuels (hors combat, représente les HP de la fiche personnage)
        public int LifePoints
        {
            get;
            set;
        }

        // Points de vie maximum de base (sans érosion ni buffs temporaires)
        public int MaxLifePoints
        {
            get;
            set;
        }

        // Énergie maximum (niveau × 100, utilisée pour les fantômes dans Dofus)
        public ushort MaxEnergyPoints
        {
            get;
            set;
        }

        // Énergie actuelle du personnage
        public ushort Energy
        {
            get;
            set;
        }

        // Initiative totale calculée via FormulasProvider (détermine l'ordre de jeu en combat)
        public uint TotalInitiative
        {
            get
            {
                return FormulasProvider.Instance.TotalInitiative(this);
            }
        }
        public Characteristic Initiative
        {
            get;
            set;
        }

        public Characteristic Prospecting
        {
            get;
            set;
        }

        public ApCharacteristic ActionPoints
        {
            get;
            set;
        }

        public MpCharacteristic MovementPoints
        {
            get;
            set;
        }

        public RangeCharacteristic Range
        {
            get;
            set;
        }

        public Characteristic SummonableCreaturesBoost
        {
            get;
            set;
        }

        public Characteristic Reflect
        {
            get;
            set;
        }

        public Characteristic CriticalHit
        {
            get;
            set;
        }

        public ushort CriticalHitWeapon
        {
            get;
            set;
        }

        public Characteristic HealBonus
        {
            get;
            set;
        }

        public Characteristic AllDamagesBonus
        {
            get;
            set;
        }

        public Characteristic DamagesBonusPercent
        {
            get;
            set;
        }

        public Characteristic WeaponDamagesBonusPercent
        {

            get;
            set;
        }

        public Characteristic TrapBonus
        {
            get;
            set;
        }

        public Characteristic TrapBonusPercent
        {
            get;
            set;
        }

        public Characteristic GlyphBonusPercent
        {
            get;
            set;
        }

        public Characteristic RuneBonusPercent
        {
            get;
            set;
        }

        public Characteristic PermanentDamagePercent
        {
            get;
            set;
        }

        public Characteristic PushDamageBonus
        {
            get;
            set;
        }

        public Characteristic CriticalDamageBonus
        {
            get;
            set;
        }

        public Characteristic NeutralDamageBonus
        {
            get;
            set;
        }


        public Characteristic EarthDamageBonus
        {
            get;
            set;
        }

        public Characteristic WaterDamageBonus
        {
            get;
            set;
        }

        public Characteristic AirDamageBonus
        {
            get;
            set;
        }

        public Characteristic FireDamageBonus
        {
            get;
            set;
        }

        public Characteristic DodgePAProbability
        {
            get;
            set;
        }

        public Characteristic DodgePMProbability
        {
            get;
            set;
        }

        public ResistanceCharacteristic NeutralResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic EarthResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic WaterResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic AirResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic FireResistPercent
        {
            get;
            set;
        }

        public Characteristic NeutralReduction { get; set; }

        public Characteristic EarthReduction { get; set; }

        public Characteristic WaterReduction { get; set; }

        public Characteristic AirReduction { get; set; }

        public Characteristic FireReduction { get; set; }

        public Characteristic PushDamageReduction { get; set; }

        public Characteristic CriticalDamageReduction { get; set; }

        public ResistanceCharacteristic PvPNeutralResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic PvPEarthResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic PvPWaterResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic PvPAirResistPercent
        {
            get;
            set;
        }

        public ResistanceCharacteristic PvPFireResistPercent
        {
            get;
            set;
        }

        public Characteristic PvPNeutralReduction { get; set; }

        public Characteristic PvPEarthReduction { get; set; }

        public Characteristic PvPWaterReduction { get; set; }

        public Characteristic PvPAirReduction { get; set; }

        public Characteristic PvPFireReduction { get; set; }

        public short GlobalDamageReduction { get; set; }

        public Characteristic Strength { get; set; }

        public Characteristic Agility { get; set; }

        public Characteristic Chance { get; set; }

        public Characteristic Vitality { get; set; }

        public Characteristic Wisdom { get; set; }

        public Characteristic Intelligence { get; set; }

        public Characteristic TackleBlock { get; set; }

        public Characteristic TackleEvade { get; set; }

        public Characteristic PAAttack { get; set; }

        public Characteristic PMAttack { get; set; }

        public ushort WeightBonus { get; set; }

        // Puissance calculée = somme des 4 caractéristiques principales divisée par 5.
        // Utilisée pour équilibrer les stats des invocations selon le niveau de l'invocateur.
        public int Power
        {
            get
            {
                return (int)((double)Total() / (double)5);
            }
        }

        // Retourne une caractéristique par son identifiant enum via réflexion.
        // Permet aux buffs de cibler n'importe quelle stat sans switch/case.
        public Characteristic GetCharacteristic(StatsBoostEnum statId)
        {
            var proprety = this.GetType().GetProperty(statId.ToString());
            return proprety != null ? (Characteristic)proprety.GetValue(this) : null;
        }

        // Retourne la somme des 4 caractéristiques de combat principales (Force + Chance + Intelligence + Agilité)
        public uint Total()
        {
            return (uint)(Strength.Total() + Chance.Total() + Intelligence.Total() + Agility.Total());
        }

        // Crée un objet Stats vierge pour un nouveau personnage (niveau donné, classe donnée).
        // Les valeurs initiales sont lues depuis BreedRecord (points de vie, prospection de départ...).
        public static Stats New(ushort level, sbyte breedId)
        {
            BreedRecord breed = BreedRecord.GetBreed(breedId);

            var stats = new Stats()
            {
                ActionPoints = ApCharacteristic.New(StartActionPoints),
                MovementPoints = MpCharacteristic.New((short)StartMovementPoints),
                Agility = Characteristic.Zero(),
                AirDamageBonus = Characteristic.Zero(),
                AirReduction = Characteristic.Zero(),
                AirResistPercent = ResistanceCharacteristic.Zero(),
                AllDamagesBonus = Characteristic.Zero(),
                DamagesBonusPercent = Characteristic.Zero(),
                Chance = Characteristic.Zero(),
                CriticalDamageBonus = Characteristic.Zero(),
                CriticalDamageReduction = Characteristic.Zero(),
                CriticalHit = Characteristic.Zero(),
                Initiative = Characteristic.Zero(),
                CriticalHitWeapon = 0,
                DodgePAProbability = Characteristic.Zero(),
                DodgePMProbability = Characteristic.Zero(),
                EarthDamageBonus = Characteristic.Zero(),
                EarthReduction = Characteristic.Zero(),
                EarthResistPercent = ResistanceCharacteristic.Zero(),
                FireDamageBonus = Characteristic.Zero(),
                FireReduction = Characteristic.Zero(),
                FireResistPercent = ResistanceCharacteristic.Zero(),
                GlobalDamageReduction = 0,
                GlyphBonusPercent = Characteristic.Zero(),
                RuneBonusPercent = Characteristic.Zero(),
                PermanentDamagePercent = Characteristic.Zero(),
                HealBonus = Characteristic.Zero(),
                Intelligence = Characteristic.Zero(),
                LifePoints = breed.StartLifePoints,
                MaxEnergyPoints = (ushort)(level * 100),
                NeutralDamageBonus = Characteristic.Zero(),
                NeutralReduction = Characteristic.Zero(),
                NeutralResistPercent = ResistanceCharacteristic.Zero(),
                Prospecting = Characteristic.New(breed.StartProspecting),
                PushDamageBonus = Characteristic.Zero(),
                PushDamageReduction = Characteristic.Zero(),
                PvPAirReduction = Characteristic.Zero(),
                PvPAirResistPercent = ResistanceCharacteristic.Zero(),
                PvPEarthReduction = Characteristic.Zero(),
                PvPEarthResistPercent = ResistanceCharacteristic.Zero(),
                PvPFireReduction = Characteristic.Zero(),
                PvPFireResistPercent = ResistanceCharacteristic.Zero(),
                PvPNeutralReduction = Characteristic.Zero(),
                PvPNeutralResistPercent = ResistanceCharacteristic.Zero(),
                PvPWaterReduction = Characteristic.Zero(),
                PvPWaterResistPercent = ResistanceCharacteristic.Zero(),
                Range = RangeCharacteristic.Zero(),
                Reflect = Characteristic.Zero(),
                Strength = Characteristic.Zero(),
                SummonableCreaturesBoost = Characteristic.New(1),
                TrapBonus = Characteristic.Zero(),
                TrapBonusPercent = Characteristic.Zero(),
                Vitality = Characteristic.Zero(),
                WaterDamageBonus = Characteristic.Zero(),
                WaterReduction = Characteristic.Zero(),
                WaterResistPercent = ResistanceCharacteristic.Zero(),
                WeaponDamagesBonusPercent = Characteristic.Zero(),
                Wisdom = Characteristic.Zero(),
                TackleBlock = Characteristic.Zero(),
                TackleEvade = Characteristic.Zero(),
                PAAttack = Characteristic.Zero(),
                PMAttack = Characteristic.Zero(),
                Energy = (ushort)(level * 100),
                MaxLifePoints = breed.StartLifePoints,
                WeightBonus = 0,
            };
            return stats;
        }
        // Sérialise toutes les stats en CharacterCharacteristicsInformations pour le protocole client.
        // Ce message est envoyé lors de l'ouverture de la fiche de personnage ou après un changement de stats.
        public CharacterCharacteristicsInformations GetCharacterCharacteristics(Character character)
        {
            ulong expFloor = ExperienceRecord.GetExperienceForLevel(character.Level).Player;
            ulong expNextFloor = ExperienceRecord.GetExperienceForNextLevel(character.Level).Player;

            CharacterCharacteristicsInformations informations = new CharacterCharacteristicsInformations()
            {
                actionPoints = ActionPoints.GetBaseCharacteristic(),
                actionPointsCurrent = ActionPoints.TotalInContext(),
                additionnalPoints = 0,
                agility = Agility.GetBaseCharacteristic(),
                airDamageBonus = AirDamageBonus.GetBaseCharacteristic(),
                airElementReduction = AirReduction.GetBaseCharacteristic(),
                airElementResistPercent = AirResistPercent.GetBaseCharacteristic(),
                alignmentInfos = character.Record.Alignment.GetActorExtendedAlignement(),
                allDamagesBonus = AllDamagesBonus.GetBaseCharacteristic(),
                chance = Chance.GetBaseCharacteristic(),
                criticalDamageBonus = CriticalDamageBonus.GetBaseCharacteristic(),
                criticalDamageReduction = CriticalDamageReduction.GetBaseCharacteristic(),
                criticalHit = CriticalHit.GetBaseCharacteristic(),
                criticalHitWeapon = CriticalHitWeapon,
                criticalMiss = Characteristic.Zero().GetBaseCharacteristic(),
                damagesBonusPercent = DamagesBonusPercent.GetBaseCharacteristic(),
                dodgePALostProbability = DodgePAProbability.GetBaseCharacteristic(),
                dodgePMLostProbability = DodgePMProbability.GetBaseCharacteristic(),
                earthDamageBonus = EarthDamageBonus.GetBaseCharacteristic(),
                earthElementReduction = EarthReduction.GetBaseCharacteristic(),
                earthElementResistPercent = EarthResistPercent.GetBaseCharacteristic(),
                pvpEarthElementReduction = PvPEarthReduction.GetBaseCharacteristic(),
                pvpEarthElementResistPercent = PvPEarthResistPercent.GetBaseCharacteristic(),
                fireDamageBonus = FireDamageBonus.GetBaseCharacteristic(),
                fireElementReduction = FireReduction.GetBaseCharacteristic(),
                fireElementResistPercent = FireResistPercent.GetBaseCharacteristic(),
                pvpFireElementReduction = PvPFireReduction.GetBaseCharacteristic(),
                pvpFireElementResistPercent = PvPFireResistPercent.GetBaseCharacteristic(),
                glyphBonusPercent = GlyphBonusPercent.GetBaseCharacteristic(),
                healBonus = HealBonus.GetBaseCharacteristic(),
                initiative = new CharacterBaseCharacteristic((short)FormulasProvider.Instance.BaseInitiative(this), 0, Initiative.Total(), 0, 0),
                intelligence = Intelligence.GetBaseCharacteristic(),
                kamas = character.Record.Kamas,
                lifePoints = (uint)LifePoints,
                maxEnergyPoints = MaxEnergyPoints,
                maxLifePoints = (uint)MaxLifePoints,
                movementPoints = MovementPoints.GetBaseCharacteristic(),
                movementPointsCurrent = MovementPoints.TotalInContext(),
                PMAttack = PMAttack.GetBaseCharacteristic(),
                PAAttack = PAAttack.GetBaseCharacteristic(),
                pvpAirElementReduction = PvPAirReduction.GetBaseCharacteristic(),
                pvpAirElementResistPercent = PvPAirResistPercent.GetBaseCharacteristic(),
                pvpNeutralElementReduction = PvPNeutralReduction.GetBaseCharacteristic(),
                pvpNeutralElementResistPercent = PvPNeutralResistPercent.GetBaseCharacteristic(),
                pvpWaterElementReduction = PvPWaterReduction.GetBaseCharacteristic(),
                pvpWaterElementResistPercent = PvPWaterResistPercent.GetBaseCharacteristic(),
                energyPoints = Energy,
                experience = character.Experience,
                experienceLevelFloor = expFloor,
                experienceNextLevelFloor = expNextFloor,
                neutralDamageBonus = NeutralDamageBonus.GetBaseCharacteristic(),
                neutralElementReduction = NeutralReduction.GetBaseCharacteristic(),
                neutralElementResistPercent = NeutralResistPercent.GetBaseCharacteristic(),
                tackleEvade = TackleEvade.GetBaseCharacteristic(),
                tackleBlock = TackleBlock.GetBaseCharacteristic(),
                range = Range.GetBaseCharacteristic(),
                waterElementReduction = WaterReduction.GetBaseCharacteristic(),
                waterDamageBonus = WaterDamageBonus.GetBaseCharacteristic(),
                waterElementResistPercent = WaterResistPercent.GetBaseCharacteristic(),
                reflect = Reflect.GetBaseCharacteristic(),
                permanentDamagePercent = PermanentDamagePercent.GetBaseCharacteristic(),
                prospecting = Prospecting.GetBaseCharacteristic(),
                pushDamageBonus = PushDamageBonus.GetBaseCharacteristic(),
                pushDamageReduction = PushDamageReduction.GetBaseCharacteristic(),
                runeBonusPercent = RuneBonusPercent.GetBaseCharacteristic(),
                spellModifications = new CharacterSpellModification[0],
                spellsPoints = character.Record.SpellPoints,
                statsPoints = character.Record.StatsPoints,
                vitality = Vitality.GetBaseCharacteristic(),
                strength = Strength.GetBaseCharacteristic(),
                summonableCreaturesBoost = SummonableCreaturesBoost.GetBaseCharacteristic(),
                trapBonus = TrapBonus.GetBaseCharacteristic(),
                trapBonusPercent = TrapBonusPercent.GetBaseCharacteristic(),
                weaponDamagesBonusPercent = WeaponDamagesBonusPercent.GetBaseCharacteristic(),
                wisdom = Wisdom.GetBaseCharacteristic(),
                probationTime = 0,



            };

            return informations;
        }
        public virtual Stats Clone()
        {
            return new Stats()
            {
                LifePoints = this.LifePoints,
                MaxLifePoints = this.MaxLifePoints,
                MaxEnergyPoints = this.MaxEnergyPoints,
                Energy = this.Energy,
                Initiative = this.Initiative.Clone(),
                Prospecting = this.Prospecting.Clone(),
                ActionPoints = (ApCharacteristic)this.ActionPoints.Clone(),
                MovementPoints = (MpCharacteristic)this.MovementPoints.Clone(),
                Range = (RangeCharacteristic)this.Range.Clone(),
                SummonableCreaturesBoost = this.SummonableCreaturesBoost.Clone(),
                Reflect = this.Reflect.Clone(),
                CriticalHit = this.CriticalHit.Clone(),
                CriticalHitWeapon = this.CriticalHitWeapon,
                HealBonus = this.HealBonus.Clone(),
                AllDamagesBonus = this.AllDamagesBonus.Clone(),
                DamagesBonusPercent = this.DamagesBonusPercent.Clone(),
                WeaponDamagesBonusPercent = this.WeaponDamagesBonusPercent.Clone(),
                TrapBonus = this.TrapBonus.Clone(),
                TrapBonusPercent = this.TrapBonusPercent.Clone(),
                GlyphBonusPercent = this.GlyphBonusPercent.Clone(),
                RuneBonusPercent = this.RuneBonusPercent.Clone(),
                PermanentDamagePercent = this.PermanentDamagePercent.Clone(),
                PushDamageBonus = this.PushDamageBonus.Clone(),
                CriticalDamageBonus = this.CriticalDamageBonus.Clone(),
                NeutralDamageBonus = this.NeutralDamageBonus.Clone(),
                EarthDamageBonus = this.EarthDamageBonus.Clone(),
                WaterDamageBonus = this.WaterDamageBonus.Clone(),
                AirDamageBonus = this.AirDamageBonus.Clone(),
                FireDamageBonus = this.FireDamageBonus.Clone(),
                DodgePAProbability = this.DodgePAProbability.Clone(),
                DodgePMProbability = this.DodgePMProbability.Clone(),
                NeutralResistPercent = (ResistanceCharacteristic)this.NeutralResistPercent.Clone(),
                EarthResistPercent = (ResistanceCharacteristic)this.EarthResistPercent.Clone(),
                WaterResistPercent = (ResistanceCharacteristic)this.WaterResistPercent.Clone(),
                AirResistPercent = (ResistanceCharacteristic)this.AirResistPercent.Clone(),
                FireResistPercent = (ResistanceCharacteristic)this.FireResistPercent.Clone(),
                NeutralReduction = this.NeutralReduction.Clone(),
                EarthReduction = this.EarthReduction.Clone().Clone(),
                WaterReduction = this.WaterReduction.Clone(),
                AirReduction = this.AirReduction.Clone(),
                FireReduction = this.FireReduction.Clone(),
                PushDamageReduction = this.PushDamageReduction.Clone(),
                CriticalDamageReduction = this.CriticalDamageReduction.Clone(),
                PvPNeutralResistPercent = (ResistanceCharacteristic)this.PvPNeutralResistPercent.Clone(),
                PvPEarthResistPercent = (ResistanceCharacteristic)this.PvPEarthResistPercent.Clone(),
                PvPWaterResistPercent = (ResistanceCharacteristic)this.PvPWaterResistPercent.Clone(),
                PvPAirResistPercent = (ResistanceCharacteristic)this.PvPAirResistPercent.Clone(),
                PvPFireResistPercent = (ResistanceCharacteristic)this.PvPFireResistPercent.Clone(),
                PvPNeutralReduction = this.PvPNeutralReduction.Clone(),
                PvPEarthReduction = this.PvPEarthReduction.Clone(),
                PvPWaterReduction = this.PvPWaterReduction.Clone(),
                PvPAirReduction = this.PvPAirReduction.Clone(),
                PvPFireReduction = this.PvPFireReduction.Clone(),
                GlobalDamageReduction = this.GlobalDamageReduction,
                Strength = this.Strength.Clone(),
                Agility = this.Agility.Clone(),
                Chance = this.Chance.Clone(),
                Vitality = this.Vitality.Clone(),
                Wisdom = this.Wisdom.Clone(),
                Intelligence = this.Intelligence.Clone(),
                TackleBlock = this.TackleBlock.Clone(),
                TackleEvade = this.TackleEvade.Clone(),
                PAAttack = this.PAAttack.Clone(),
                PMAttack = this.PMAttack.Clone(),
                WeightBonus = this.WeightBonus,
            };
        }
    }
}
