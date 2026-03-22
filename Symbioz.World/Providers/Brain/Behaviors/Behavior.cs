using Symbioz.Core;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Providers.Brain.Actions;
using Symbioz.World.Providers.Fights.Buffs;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Brain.Behaviors
{
    /// <summary>
    /// Classe de base pour tous les comportements personnalisés des monstres.
    /// Un Behavior permet à un monstre de surcharger la logique générique de l'IA :
    ///   - GetSortedActions() : définit les types d'actions et leur ordre
    ///   - GetSpellsCategories() : définit les catégories de sorts à utiliser
    ///   - GetTargetCellForSpell() : définit la cellule cible pour un sort spécifique
    ///   - GetAgressiveCell()/GetBuffCell()/GetTeleportCell() : cellules cibles par type
    ///
    /// Les sous-classes sont enregistrées via [Behavior("NomDuBehavior")] et chargées
    /// par BehaviorManager au démarrage. Retourner null/-1 = déléguer à la logique générique.
    /// Fournit aussi des utilitaires : MakeInvulnerable, MakeVulnerable, Summon.
    /// </summary>
    public abstract class Behavior
    {
        // ID du sort factice utilisé pour appliquer l'invulnérabilité (état 56)
        public const ushort InvunlerabilitySpellId = 5199;

        // Monstre auquel ce comportement est attaché
        protected BrainFighter Fighter
        {
            get;
            set;
        }
        public Behavior(BrainFighter fighter)
        {
            this.Fighter = fighter;
        }

        // Crée un SpellLevelRecord fictif pour forcer l'application d'effets personnalisés
        public static SpellLevelRecord CreateBasicSpellLevel(short apCost, List<EffectInstance> effects, ushort spellId)
        {
            return new SpellLevelRecord(0, spellId, 1, apCost, 0, 10, true, true, true, 50, false, false, false, false, 10, 10, 10,
                0, 0, 0, new List<short>(), new List<short>(), effects, effects);
        }

        // Applique l'état invulnérabilité (état 56) sur la cible pour une durée donnée
        public void MakeInvulnerable(Fighter target, short duration)
        {
            EffectInstance effect = new EffectInstance()
            {
                Delay = 0,
                DiceMax = 0,
                DiceMin = 0,
                Duration = duration,
                EffectElement = 0,
                EffectId = (ushort)EffectsEnum.Effect_AddState,
                EffectUID = 0,
                Random = 0,
                RawZone = "P1",
                TargetMask = "A#a",
                Triggers = "I",
                Value = 56, // 56 = ID de l'état "invulnérable" dans le protocole
            };

            var level = CreateBasicSpellLevel(0, new List<EffectInstance>() { effect }, InvunlerabilitySpellId);

            bool sequence = Fighter.Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);

            target.ForceSpellCast(level, target.CellId);

            if (sequence)
                Fighter.Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }

        // Retourne null = la logique générique de l'EnvironmentAnalyser est utilisée
        public virtual Dictionary<int, SpellCategoryEnum> GetSpellsCategories()
        {
            return null;
        }

        // Supprime l'invulnérabilité en dissipant les buffs du sort d'invulnérabilité
        public void MakeVulnerable(Fighter target)
        {
            bool sequence = Fighter.Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);
            Fighter.DispellSpellBuffs(target, InvunlerabilitySpellId);
            if (sequence)
                Fighter.Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }

        // Invoque un monstre sur la cellule donnée et l'ajoute au combat
        public SummonedFighter Summon(Fighter source, ushort monsterId, short cellId)
        {
            SummonedFighter summon = CreateSummoned(source, MonsterRecord.GetMonster(monsterId), cellId);

            bool sequence = Fighter.Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);

            Fighter.Fight.AddSummon(summon);

            if (sequence)
                Fighter.Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);

            return summon;
        }

        // Crée le SummonedFighter avec un grade aléatoire entre 1 et 4
        private SummonedFighter CreateSummoned(Fighter master, MonsterRecord template, short cellId)
        {
            return new SummonedFighter(template, (sbyte)new AsyncRandom().Next(1, 5), master, master.Team, cellId);
        }

        // Retourne null = l'EnvironmentAnalyser choisit la cellule cible selon la catégorie du sort
        public virtual short? GetTargetCellForSpell(ushort spellId)
        {
            return null;
        }

        // Retourne -1 = l'EnvironmentAnalyser choisit la cible agressive par défaut
        public virtual short GetAgressiveCell()
        {
            return -1;
        }
        // Retourne -1 = l'EnvironmentAnalyser choisit la cible de buff par défaut
        public virtual short GetBuffCell()
        {
            return -1;
        }
        // Retourne -1 = l'EnvironmentAnalyser choisit la cible de téléportation par défaut
        public virtual short GetTeleportCell()
        {
            return -1;
        }
        // Retourne null = l'EnvironmentAnalyser détermine les actions et leur ordre
        public virtual ActionType[] GetSortedActions()
        {
            return null;
        }
    }
}