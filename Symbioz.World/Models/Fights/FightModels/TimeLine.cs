using Symbioz.World.Models.Fights.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.FightModels
{
    /// <summary>
    /// Gère l'ordre de jeu des combattants dans un combat (la "timeline" ou file d'initiative).
    /// Détermine quel combattant joue, dans quel ordre, et quand un nouveau round commence.
    /// L'ordre est calculé en début de combat selon l'initiative de chaque combattant,
    /// en alternant les combattants des deux équipes.
    /// </summary>
    public class TimeLine
    {
        // Liste ordonnée des combattants (selon leur initiative)
        internal List<Fighter> Fighters
        {
            get;
            private set;
        }
        public Fight Fight
        {
            get;
            private set;
        }
        // Combattant actuellement en train de jouer (null si aucun)
        public Fighter Current
        {
            get
            {
                return (this.Index == -1 || this.Index >= this.Fighters.Count) ? null : this.Fighters[this.Index];
            }
        }
        // Position actuelle dans la liste des combattants
        public int Index
        {
            get;
            private set;
        }
        // Nombre total de combattants dans la timeline
        public int Count
        {
            get
            {
                return this.Fighters.Count;
            }
        }
        // Numéro du round actuel (commence à 1, s'incrémente quand on revient au début)
        public uint RoundNumber
        {
            get;
            private set;
        }
        // Indique si un nouveau round vient de commencer (remis à false après chaque tour)
        public bool NewRound
        {
            get;
            private set;
        }
        public TimeLine(Fight fight)
        {
            this.Fight = fight;
            this.Fighters = new List<Fighter>();
            this.RoundNumber = 1;
        }
        /// <summary>
        /// Retire un combattant de la timeline (mort, fuite...).
        /// Ajuste l'index actuel si nécessaire pour ne pas sauter un combattant.
        /// Retourne true si le combattant était bien dans la liste.
        /// </summary>
        public bool RemoveFighter(Fighter fighter)
        {
            bool result;
            if (!this.Fighters.Contains(fighter))
            {
                result = false;
            }
            else
            {
                int num = this.Fighters.IndexOf(fighter);
                this.Fighters.Remove(fighter);
                // Si on retire quelqu'un avant ou à l'index actuel, on décale l'index pour rester sur le bon combattant
                if (num <= this.Index && num > 0)
                {
                    this.Index--;
                }
                result = true;
            }
            return result;
        }
        public bool InsertFighter(Fighter fighter, int index)
        {
            bool result;
            if (index > this.Fighters.Count)
            {
                result = false;
            }
            else
            {
                this.Fighters.Insert(index, fighter);
                if (index <= this.Index)
                {
                    this.Index++;
                }
                result = true;
            }
            return result;
        }
        public bool SelectNextFighter()
        {
            bool result;
            if (this.Fighters.Count == 0)
            {
                this.Index = -1;
                result = false;
            }
            else
            {
                int num = 0;
                int num2 = (this.Index + 1 < this.Fighters.Count) ? (this.Index + 1) : 0;
                if (num2 == 0)
                {
                    this.RoundNumber += 1;
                    this.NewRound = true;
                }
                else
                {
                    this.NewRound = false;
                }
                while (!this.Fighters[num2].CanPlay() && num < this.Fighters.Count)
                {
                    num2 = ((num2 + 1 < this.Fighters.Count) ? (num2 + 1) : 0);
                    if (num2 == 0)
                    {
                        this.RoundNumber += 1;
                        this.NewRound = true;
                    }
                    num++;
                }
                if (!this.Fighters[num2].CanPlay())
                {
                    this.Index = -1;
                    result = false;
                }
                else
                {
                    this.Index = num2;
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// Calcule l'ordre de jeu en début de combat selon l'initiative.
        /// Chaque équipe est triée par initiative décroissante.
        /// L'équipe dont le leader a la plus haute initiative joue en premier.
        /// Les combattants sont ensuite intercalés : 1 bleu, 1 rouge, 1 bleu, 1 rouge...
        /// </summary>
        public void OrderLine()
        {
            // Tri des combattants de chaque équipe par initiative décroissante
            IOrderedEnumerable<Fighter> orderedEnumerable =
                from entry in this.Fight.BlueTeam.GetFighters(false)
                orderby entry.Stats.TotalInitiative descending
                select entry;
            IOrderedEnumerable<Fighter> orderedEnumerable2 =
                from entry in this.Fight.RedTeam.GetFighters(false)
                orderby entry.Stats.TotalInitiative descending
                select entry;

            // L'équipe avec la plus haute initiative du leader joue en premier (flag = true = bleu d'abord)
            bool flag = orderedEnumerable.First().Stats.Initiative.Total() > orderedEnumerable2.First().Stats.Initiative.Total();
            System.Collections.Generic.IEnumerator<Fighter> enumerator = orderedEnumerable.GetEnumerator();
            System.Collections.Generic.IEnumerator<Fighter> enumerator2 = orderedEnumerable2.GetEnumerator();
            System.Collections.Generic.List<Fighter> list = new System.Collections.Generic.List<Fighter>();
            bool flag2;
            bool flag3;
            while ((flag2 = enumerator.MoveNext()) | (flag3 = enumerator2.MoveNext()))
            {
                if (flag)
                {
                    if (flag2)
                    {
                        list.Add(enumerator.Current);
                    }
                    if (flag3)
                    {
                        list.Add(enumerator2.Current);
                    }
                }
                else
                {
                    if (flag3)
                    {
                        list.Add(enumerator2.Current);
                    }
                    if (flag2)
                    {
                        list.Add(enumerator.Current);
                    }
                }
            }
            this.Fighters = list;
            this.Index = 0;
        }
        public double[] GetIds()
        {
            return Fighters.FindAll(x=>x.Alive).ConvertAll<double>(x => x.Id).ToArray();
        }
    }
}
