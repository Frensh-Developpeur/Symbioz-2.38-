using Symbioz.Core;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Fights.Results;
using Symbioz.World.Models.Items;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Providers;
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
    /// Représente un monstre dans un combat (PvM).
    /// Hérite de BrainFighter qui gère l'IA (MonsterBrain).
    /// Gère le loot (drops d'items et kamas) et les informations d'affichage du monstre.
    /// Dès que son tour commence, il joue automatiquement (IA) puis passe son tour.
    /// </summary>
    public class MonsterFighter : BrainFighter
    {
        // Compteur de drops par item pour respecter les limites de drop
        private readonly Dictionary<MonsterDrop, int> m_dropsCount = new Dictionary<MonsterDrop, int>();

        public MonsterFighter(FightTeam team, Monster monster, ushort mapCellId)
            : base(team, mapCellId, monster.Template, monster.GradeId)
        {
        }
        public MonsterFighter(FightTeam team, MonsterRecord template, sbyte gradeId, ushort mapCellId)
            : base(
                team, mapCellId, template, gradeId)
        {

        }
        public override GameFightFighterInformations GetFightFighterInformations()
        {
            return new GameFightMonsterInformations(Id, Look.ToEntityLook(), new EntityDispositionInformations((short)CellId, (sbyte)Direction),
                Team.Id, 0, Alive, Stats.GetFightMinimalStats(), new ushort[0], Template.Id, GradeId);
        }
        public override uint GetDroppedKamas()
        {
            AsyncRandom asyncRandom = new AsyncRandom();
            return (uint)asyncRandom.Next(base.Template.MinDroppedKamas, base.Template.MaxDroppedKamas + 1);
        }
        public override void OnTurnStarted()
        {
            base.OnTurnStarted();

            this.PassTurn();
        }
        /// <summary>
        /// Calcule le loot (items droppés) par ce monstre pour un looter donné.
        /// Si le monstre est encore vivant, aucun loot n'est donné.
        /// Sinon, pour chaque item droppable du template :
        ///   1. Vérifie que la prospection de l'équipe est suffisante
        ///   2. Lance un dé (probabilité calculée par FormulasProvider) pour chaque tentative
        ///   3. Respecte les limites de drop (DropLimit) par item
        /// </summary>
        public override IEnumerable<DroppedItem> RollLoot(IFightResult looter, int dropBonusPercent)
        {
            IEnumerable<DroppedItem> result;
            if (Alive)
            {
                // Un monstre vivant ne droppe rien
                result = new DroppedItem[0];
            }
            else
            {
                AsyncRandom asyncRandom = new AsyncRandom();
                List<DroppedItem> list = new List<DroppedItem>();
                // Somme la prospection de tous les personnages ennemis (affecte les chances de drop)
                int prospectingSum = base.OposedTeam().GetFighters<CharacterFighter>().Sum((CharacterFighter entry) => entry.Stats.Prospecting.TotalInContext());
                // Filtre les items dont la prospection requise est atteinte
                foreach (var current in
                    from droppableItem in base.Template.Drops
                    where prospectingSum >= droppableItem.ProspectingLock && !droppableItem.HasCriteria
                    select droppableItem)
                {
                    int num = 0;
                    // Tente de dropper l'item jusqu'à Count fois (ou jusqu'à la limite de drop)
                    while (num < current.Count && (current.DropLimit <= 0 || !this.m_dropsCount.ContainsKey(current) || this.m_dropsCount[current] < current.DropLimit))
                    {
                        var deci = asyncRandom.NextDouble();
                        double num2 = (double)asyncRandom.Next(0, 100) + deci;
                        // Calcule le taux de drop ajusté (prospection, âge du monstre, taux configuré)
                        double num3 = FormulasProvider.Instance.AdjustDropChance(looter, current, GradeId, (int)base.Fight.AgeBonus, dropBonusPercent);

                        if (num3 >= num2)
                        {
                            // L'item est droppé : on l'ajoute à la liste et on incrémente le compteur
                            list.Add(new DroppedItem(current.ItemId, 1u));
                            if (!this.m_dropsCount.ContainsKey(current))
                            {
                                this.m_dropsCount.Add(current, 1);
                            }
                            else
                            {
                                System.Collections.Generic.Dictionary<MonsterDrop, int> dropsCount;
                                MonsterDrop key;
                                (dropsCount = this.m_dropsCount)[key = current] = dropsCount[key] + 1;
                            }
                        }
                        num++;
                    }
                }
                result = list;
            }
            return result;
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberMonsterInformations((double)Id, (int)base.Template.Id, base.GradeId);
        }




    }
}
