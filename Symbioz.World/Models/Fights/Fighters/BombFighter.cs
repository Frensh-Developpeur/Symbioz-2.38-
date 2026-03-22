using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights.Damages;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Fights.Marks;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Providers.Brain.Behaviors;
using Symbioz.World.Providers.Fights.Effects;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Fighters
{
    /// <summary>
    /// Bombe du Roublard : entité posée sur le terrain qui grossit chaque tour
    /// et peut exploser pour infliger des dégâts en chaîne.
    /// Les bombes créent automatiquement des murs (Wall) entre elles via BombProvider.
    /// La bombe grossit visuellement (SCALE_PER_TURN) et ses dégâts de combo doublent
    /// chaque tour (COMBO_DAMAGE_FACTOR). Elle peut être détonée manuellement (Detonate).
    /// Les murs sont détruits si la bombe est déplacée, téléportée ou meurt.
    /// </summary>
    public class BombFighter : Fighter, ISummon<Fighter>, IMonster
    {
        // Grossissement visuel de la bombe à chaque tour du poseur (en unités de scale)
        public const short SCALE_PER_TURN = 30;

        // Nombre de tours pendant lesquels les buffs de combo sont actifs
        public const short BUFF_TURN_COUNT = 3;

        // Facteur multiplicatif des dégâts de combo (x2 chaque tour)
        public const short COMBO_DAMAGE_FACTOR = 2;

        // Dégâts initiaux de combo (avant les doublements)
        public const short INITIAL_COMBO_DAMAGE = 20;

        public ushort MonsterId
        {
            get
            {
                return Template.Id;
            }
        }
        // Joueur Roublard propriétaire de la bombe
        public Fighter Owner
        {
            get;
            set;
        }
        // Template du monstre-bombe (définit l'apparence et le grade)
        private MonsterRecord Template
        {
            get;
            set;
        }
        private MonsterGrade Grade
        {
            get;
            set;
        }
        // Cellule de pose initiale de la bombe
        private short SummonCellId
        {
            get;
            set;
        }
        private sbyte GradeId
        {
            get;
            set;
        }
        // Liste des murs créés entre cette bombe et d'autres bombes du même propriétaire
        public List<Wall> Walls
        {
            get;
            private set;
        }
        // Niveau du sort utilisé pour invoquer cette bombe
        public SpellLevelRecord SummonSpellLevel
        {
            get;
            private set;
        }
        // Données du sort-bombe (IDs des sorts d'explosion et de mur associés)
        public SpellBombRecord SpellBombRecord
        {
            get;
            private set;
        }
        // Niveau du sort de mur associé à cette bombe
        public SpellLevelRecord WallSpellLevel
        {
            get;
            private set;
        }
        // Nombre de tours restants avant que les buffs de combo s'arrêtent
        public int BuffTurnCount
        {
            get;
            private set;
        }
        // Dégâts de combo actuels (doublent chaque tour jusqu'à BUFF_TURN_COUNT)
        public short ComboDamages
        {
            get;
            private set;
        }
        public BombFighter(MonsterRecord template, Fighter owner, FightTeam team, short summonCellId, sbyte gradeId, SpellLevelRecord summonSpellLevel)
            : base(team, 0)
        {
            this.Owner = owner;
            this.Template = template;
            this.GradeId = gradeId;
            this.Grade = Template.GetGrade(gradeId);
            this.SummonCellId = summonCellId;
            this.BeforeSlideEvt += BombFighter_BeforeSlideEvt;
            this.Owner.OnTurnStartEvt += OnOwnerTurnStart;
            this.AfterSlideEvt += BombFighter_OnSlideEvt;
            this.OnTeleportEvt += BombFighter_OnTeleportEvt;
            this.AfterDeadEvt += BombFighter_AfterDeadEvt;
            this.OnDamageTaken += BombFighter_OnDamageTaken;
            this.BeforeDeadEvt += BombFighter_OnDeadEvt;
            this.Walls = new List<Wall>();
            this.SummonSpellLevel = summonSpellLevel;
            this.SpellBombRecord = SpellBombRecord.GetSpellBombRecord(summonSpellLevel.SpellId);
            this.WallSpellLevel = SpellRecord.GetSpellRecord(SpellBombRecord.WallSpellId).GetLevel(GradeId);
            this.BuffTurnCount = BUFF_TURN_COUNT;
        }

        private void BombFighter_AfterDeadEvt(Fighter arg1, bool arg2)
        {
            UnbindEvents(arg1);
        }

        private void BombFighter_OnDamageTaken(Fighter fighter, Damage dmg)
        {

        }

        private void UnbindEvents(Fighter obj)
        {
            this.BeforeSlideEvt -= BombFighter_BeforeSlideEvt;
            this.Owner.OnTurnStartEvt -= OnOwnerTurnStart;
            this.AfterSlideEvt -= BombFighter_OnSlideEvt;
            this.OnTeleportEvt -= BombFighter_OnTeleportEvt;
            this.AfterDeadEvt -= BombFighter_AfterDeadEvt;
            this.BeforeDeadEvt -= BombFighter_OnDeadEvt;
            this.OnDamageTaken -= BombFighter_OnDamageTaken;
        }

        // Appelé au début de chaque tour du propriétaire :
        // 1. Grossit visuellement la bombe (AddScale)
        // 2. Double les dégâts de combo (ou initialise à INITIAL_COMBO_DAMAGE)
        // 3. Décrémente le compteur de tours de buff ; arrête l'abonnement quand épuisé
        private void OnOwnerTurnStart(Fighter obj)
        {
            bool sequence = Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_SPELL);
            Look.AddScale(SCALE_PER_TURN);
            this.ChangeLook(Look, this);

            this.BuffTurnCount--;

            if (ComboDamages == 0)
            {
                ComboDamages = INITIAL_COMBO_DAMAGE;
            }
            else
            {
                ComboDamages *= COMBO_DAMAGE_FACTOR;
            }

            if (BuffTurnCount == 0)
            {
                Owner.OnTurnStartEvt -= OnOwnerTurnStart;
            }
            if (sequence)
                Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_SPELL);
        }

        // Quand la bombe est poussée, tous ses murs sont détruits avant le déplacement
        private void BombFighter_BeforeSlideEvt(Fighter target, Fighter source, short startCellId, short endCellId)
        {
            foreach (var wall in new List<Wall>(this.Walls))
            {
                wall.Destroy();
            }
        }
        // À la mort de la bombe, tous ses murs sont supprimés du terrain
        void BombFighter_OnDeadEvt(Fighter obj)
        {
            foreach (var wall in new List<Wall>(this.Walls))
            {
                this.Walls.Remove(wall);
                Fight.RemoveMark(Owner, wall);
            }

        }
        /// <summary>
        /// Fait exploser la bombe :
        /// 1. La tue (Die)
        /// 2. Force le lancer du sort d'explosion ciblé (CibleExplosionSpellId) depuis sa position
        /// 3. Propage la détonation aux bombes adjacentes du même propriétaire (réaction en chaîne)
        /// </summary>
        public void Detonate(Fighter source)
        {
            Die(this);
            var level = SpellRecord.GetSpellRecord(SpellBombRecord.CibleExplosionSpellId).GetLevel(GradeId);
            source.ForceSpellCast(level, CellId);

            // Déclenche les bombes voisines du même propriétaire en réaction en chaîne
            foreach (var fighter in this.GetNearFighters<BombFighter>())
            {
                if (fighter.Alive && fighter.Owner == Owner)
                {
                    fighter.Detonate(source);
                }
            }
        }
        void BombFighter_OnTeleportEvt(Fighter target, Fighter source)
        {
            BombProvider.Instance.UpdateWalls(this);
        }

        void BombFighter_OnSlideEvt(Fighter target, Fighter source, short startCellId, short endCellId)
        {
            BombProvider.Instance.UpdateWalls(this);
        }



        public bool IsOwner(Fighter fighter)
        {
            return Owner == fighter;
        }

        public override bool Sex
        {
            get
            {
                return false;
            }
        }

        public override string Name
        {
            get
            {
                return Template.Name;
            }
        }

        public override ushort Level
        {
            get
            {
                return Grade.Level;
            }

        }
        public override void Initialize()
        {
            this.Id = Fight.PopNextContextualId();
            this.Stats = new FighterStats(Grade, 0);
            this.Look = Template.Look.Clone();
            base.Initialize();
            this.FightStartCell = SummonCellId;
            this.CellId = SummonCellId;
            this.Direction = Owner.Point.OrientationTo(this.Point, false);
            this.Stats.InitializeBomb(Owner);

        }
        public override GameFightFighterInformations GetFightFighterInformations()
        {
            return new GameFightMonsterInformations(Id, Look.ToEntityLook(), new EntityDispositionInformations((short)CellId, (sbyte)Direction),
             Team.Id, 0, Alive, Stats.GetFightMinimalStats(), new ushort[0], Template.Id, GradeId);
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            throw new NotImplementedException();
        }


    }
}
