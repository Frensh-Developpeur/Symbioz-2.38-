using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Providers.Brain;
using Symbioz.World.Records.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Fighters
{
    /// <summary>
    /// Combattant invoqué par un joueur ou un monstre pendant le combat.
    /// Un SummonedFighter est un monstre contrôlé par une IA (BrainFighter) mais appartient
    /// à un propriétaire (Owner). Il utilise les stats d'un MonsterRecord avec un grade donné.
    /// À son tour, il passe automatiquement (PassTurn) sans jouer d'actions personnalisées.
    /// Il n'apparaît dans la timeline que si le monstre utilise un slot d'invocation (UseSummonSlot).
    /// </summary>
    public class SummonedFighter : BrainFighter, ISummon<Fighter>
    {
        // Le combattant qui a invoqué cette créature
        public Fighter Owner
        {
            get;
            set;
        }
        // Cellule sur laquelle l'invocation a été placée lors de sa création
        private short SummonCellId
        {
            get;
            set;
        }


        public SummonedFighter(MonsterRecord template, sbyte gradeId, Fighter owner, FightTeam team, short cellId)
            : base(team, 0, template, gradeId)
        {
            this.Owner = owner;
            this.SummonCellId = cellId;
        }
        public bool IsOwner(Fighter fighter)
        {
            return this.Owner == fighter;
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
        // Initialise la position de l'invocation et ses stats en fonction du propriétaire
        public override void Initialize()
        {
            base.Initialize();
            this.CellId = SummonCellId;
            this.FightStartCell = SummonCellId;
            // L'invocation regarde dans la direction de son propriétaire
            this.Direction = Owner.Point.OrientationTo(this.Point, false);
            // Les stats de l'invocation dépendent du propriétaire (bonus hérités)
            this.Stats.InitializeSummon(Owner, true);
        }
        // À son tour, l'invocation passe automatiquement sans jouer d'actions
        public override void OnTurnStarted()
        {
            base.OnTurnStarted();
            if (!Fight.Ended)
                PassTurn();
        }
        // L'invocation n'est dans la timeline que si le monstre utilise un slot d'invocation
        public override bool InsertInTimeline()
        {
            return Template.UseSummonSlot;
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
