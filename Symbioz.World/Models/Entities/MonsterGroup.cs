using Symbioz.Core;
using Symbioz.Protocol.Types;
using Symbioz.Protocol.Enums;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Providers.Maps.Path;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Records.Maps;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Records.Monsters;

namespace Symbioz.World.Models.Entities
{
    /// <summary>
    /// Groupe de monstres visible sur une map de jeu de rôle.
    /// Un groupe contient un ou plusieurs monstres ; le premier ajouté est le Leader
    /// et son apparence (Look) représente tout le groupe sur la map.
    ///
    /// Le groupe peut se déplacer aléatoirement via RandomMapMove (pathfinder sur losange).
    /// À l'engagement, CreateFighters() convertit chaque monstre en MonsterFighter pour le combat.
    ///
    /// Le système de bonus d'étoiles (StarsBonusLimit, StarsBonusInterval) permet
    /// d'augmenter la difficulté et les gains des groupes vieux, mais il est désactivé ici
    /// (AgeBonusRate = 0, AgeBonus retourne 0).
    /// </summary>
    public class MonsterGroup : Entity
    {
        // Limite des étoiles affichées côté client (le client plafonne à 200)
        public const short ClientStarsBonusLimit = 200;

        // Intervalle en secondes entre chaque gain d'étoiles (mécanisme désactivé)
        public static int StarsBonusInterval = 300;

        // Nombre d'étoiles gagnées par intervalle (mécanisme désactivé)
        public static short StarsBonusIncrementation = 2;

        // Limite maximale d'étoiles (mécanisme désactivé)
        public static short StarsBonusLimit = 300;

        // Taux de bonus de vieillissement (0 = mécanisme désactivé)
        public const uint AgeBonusRate = 0;

        // Liste des monstres composant ce groupe
        private List<Monster> Monsters = new List<Monster>();

        // Nombre de monstres dans le groupe
        public int MonsterCount
        {
            get
            {
                return Monsters.Count;
            }
        }

        // Crée un groupe sur une cellule aléatoire non-bordure de la map, avec un Id négatif unique
        public MonsterGroup(MapRecord map)
        {
            this.Map = map;
            this.CellId = this.Map.RandomNoBorderFightCell();
            this.m_UId = Map.Instance.PopNextNPEntityId();
            this.CreationDate = DateTime.Now;
        }

        public override string Name
        {
            get
            {
                return Leader.Template.Name;
            }
        }

        private long m_UId;

        public override long Id
        {
            get
            {
                return m_UId;
            }
        }

        public override ushort CellId
        {
            get;
            set;
        }

        public DateTime CreationDate
        {
            get;
            set;
        }
        public short AgeBonus
        {
            get
            {
                return 0;
                return (short)Math.Min(200d, (DateTime.Now.DateTimeToUnixTimestamp() - CreationDate.DateTimeToUnixTimestamp()) / 60000);
            }
        }
        public override DirectionsEnum Direction
        {
            get;
            set;
        }

        public override ContextActorLook Look
        {
            get { return Leader.Look; }
            set { return; }
        }

        public Monster Leader
        {
            get;
            private set;
        }
        // Ajoute un monstre au groupe. Le premier ajouté devient le Leader (représentant visuel).
        public void AddMonster(Monster monster)
        {
            Monsters.Add(monster);

            if (Monsters.Count == 1)
                this.Leader = monster;
        }

        // Retourne une copie du tableau des monstres (les modifications n'affectent pas la liste interne)
        public Monster[] GetMonsters()
        {
            return Monsters.ToArray();
        }

        // Envoie l'animation de déplacement à tous les joueurs de la map et met à jour CellId
        private void Move(List<short> keys)
        {
            this.Map.Instance.Send(new GameMapMovementMessage(keys.ToArray(), Id));
            this.CellId = (ushort)keys.Last();
        }

        // Déplace le groupe vers une cellule aléatoire praticable dans un losange de rayon 1-4.
        // Utilise le pathfinder A* pour calculer le trajet.
        public void RandomMapMove()
        {
            Lozenge lozenge = new Lozenge(1, 4);
            short cellId = lozenge.GetCells((short)this.CellId, Map).Where((short entry) => Map.Walkable((ushort)entry)).Random();

            if (cellId != 0)
            {
                Pathfinder pathfinder = new Pathfinder(Map, (short)this.CellId, cellId);
                var cells = pathfinder.FindPath();

                if (cells != null && cells.Count > 0)
                {
                    cells.Insert(0, (short)this.CellId); // Inclut la cellule de départ dans le chemin
                    this.Move(cells);
                }
            }
        }

        // Crée un Fighter pour chaque monstre du groupe et les assigne à l'équipe donnée.
        // Utilisé lors de l'engagement en combat.
        public IEnumerable<Fighter> CreateFighters(FightTeam team)
        {
            foreach (var monster in Monsters)
            {
                yield return monster.CreateFighter(team);
            }
        }

        public static MonsterGroup FromTemplates(MapRecord map, MonsterRecord[] templates)
        {
            MonsterGroup group = new MonsterGroup(map);

            foreach (var template in templates)
            {
                group.AddMonster(new Monster(template, group));
            }
            return group;
        }
        public MonsterInGroupInformations[] GetMonsterInGroupInformations()
        {
            return Monsters.FindAll(x => x != Leader).ConvertAll<MonsterInGroupInformations>(x => x.GetMonsterInGroupInformations()).ToArray();
        }

        public GroupMonsterStaticInformations GetGroupMonsterStaticInformations()
        {
            return new GroupMonsterStaticInformations(Leader.GetMonsterInGroupLightInformations(), GetMonsterInGroupInformations());
        }

        public override GameRolePlayActorInformations GetActorInformations()
        {
            return new GameRolePlayGroupMonsterInformations((int)Id, Look.ToEntityLook(),
                new EntityDispositionInformations((short)CellId, (sbyte)Direction), false,
                false, false, GetGroupMonsterStaticInformations(),
             this.CreationDate.DateTimeToUnixTimestamp(), 0, 0, 0);
        }
        public override string ToString()
        {
            return "Monsters (" + Name + "' group)";
        }
    }
}
