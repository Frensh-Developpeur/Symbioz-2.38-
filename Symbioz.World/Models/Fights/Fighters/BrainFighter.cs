using Symbioz.Core;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights.FightModels;
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
    /// Classe abstraite pour tous les combattants contrôlés par l'IA (intelligence artificielle).
    /// Hérite de Fighter et implémente IMonster.
    /// Contient le MonsterBrain qui décide des actions du monstre chaque tour.
    /// Dès que son tour commence (OnTurnStarted), l'IA joue automatiquement
    /// puis le tour se termine via PassTurn() (dans MonsterFighter).
    /// </summary>
    public abstract class BrainFighter : Fighter, IMonster
    {
        public BrainFighter(FightTeam team, ushort mapCellId, MonsterRecord template, sbyte gradeId)
            : base(team, mapCellId)
        {
            this.Template = template;
            this.GradeId = gradeId;
            // Récupère le grade correspondant (niveau, stats, XP...)
            this.Grade = Template.GetGrade(this.GradeId);
        }

        // L'IA du monstre : décide des actions à chaque tour
        public MonsterBrain Brain
        {
            get;
            protected set;
        }
        // Données de base du monstre (nom, drops, stats par grade...)
        public MonsterRecord Template
        {
            get;
            private set;
        }
        // Grade actuel du monstre (1-8 selon la difficulté du groupe)
        public sbyte GradeId
        {
            get;
            private set;
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
        /// <summary>
        /// Pas de monstres femelles! que des hommes :D
        /// </summary>
        public override bool Sex
        {
            get
            {
                return false;
            }
        }
        public int Xp
        {
            get
            {
                return Grade.GradeXp;
            }
        }
        public MonsterGrade Grade
        {
            get;
            private set;
        }
        /// <summary>
        /// Initialise le monstre pour le combat : génère un ID contextuel unique,
        /// calcule ses statistiques de combat selon son grade et sa puissance,
        /// crée son cerveau (IA) et marque le monstre comme prêt (toujours prêt pour les monstres).
        /// </summary>
        public override void Initialize()
        {
            // ID négatif unique pour différencier des joueurs (IDs positifs)
            this.Id = Fight.PopNextContextualId();
            this.Stats = new FighterStats(Grade, Template.Power);
            this.Brain = new MonsterBrain(this);
            this.Look = Template.Look.Clone();
            base.Initialize();
            // Les monstres sont toujours "prêts" (pas de phase de placement manuelle)
            this.IsReady = true;
        }

        /// <summary>
        /// Quand c'est le tour du monstre, l'IA joue automatiquement.
        /// Les erreurs de l'IA sont capturées pour éviter un crash du combat.
        /// </summary>
        public override void OnTurnStarted()
        {
            base.OnTurnStarted();

            if (Alive && !Fight.Ended)
            {
                try
                {
                    Brain.Play();
                }
                catch (Exception ex)
                {
                    Logger.Write<BrainFighter>(ex.ToString(), ConsoleColor.DarkRed);
                }
            }
        }

        public ushort MonsterId
        {
            get
            {
                return Template.Id;
            }
        }


    }
}
