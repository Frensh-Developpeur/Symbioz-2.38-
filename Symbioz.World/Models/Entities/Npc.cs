using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Providers.Maps.Cinematics;
using Symbioz.World.Providers.Maps.Npcs;
using Symbioz.World.Records.Maps;
using Symbioz.World.Records.Npcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Entities
{
    /// <summary>
    /// Personnage Non Joueur (PNJ) sur une map de jeu de rôle.
    ///
    /// Un Npc est créé à partir d'un NpcSpawnRecord (position, direction, map) et d'un
    /// NpcRecord (template : apparence, nom, actions disponibles).
    ///
    /// Les interactions avec le joueur passent par InteractWith() qui :
    ///   - Délègue au CinematicProvider si le PNJ a une cinématique scriptée (Talk)
    ///   - Sinon, exécute l'action correspondante via NpcActionProvider
    ///
    /// GetActorInformations() détermine si le PNJ affiche l'icône de quête ou non.
    /// </summary>
    public class Npc : Entity
    {
        // Nom du PNJ, lu depuis le template
        public override string Name
        {
            get
            {
                return Template.Name;
            }
        }

        // Données de spawn : position (cellule, direction, map) persistées en base de données
        public NpcSpawnRecord SpawnRecord
        {
            get;
            set;
        }

        // Template du PNJ : apparence, nom, liste des actions disponibles
        public NpcRecord Template
        {
            get
            {
                return SpawnRecord.Template;
            }
        }

        // Actions disponibles pour ce PNJ (parler, ouvrir boutique, soigner, etc.)
        public List<NpcActionRecord> ActionsRecord = new List<NpcActionRecord>();

        private long m_Id; // Id négatif unique généré par PopNextNPEntityId

        public override long Id
        {
            get { return m_Id; }
        }

        public override ushort CellId
        {
            get
            {
                return SpawnRecord.CellId;
            }
            set
            {
                SpawnRecord.CellId = value;
                SpawnRecord.UpdateInstantElement();
            }
        }

        public override DirectionsEnum Direction
        {
            get
            {
                return SpawnRecord.DirectionEnum;
            }
            set
            {
                SpawnRecord.DirectionEnum = value;
                SpawnRecord.UpdateInstantElement();
            }
        }

        public override ContextActorLook Look
        {
            get
            {
                return Template.Look;
            }
            set
            {
                Template.Look = value;
                Template.UpdateInstantElement();
            }
        }
        public Npc(NpcSpawnRecord spawnRecord)
        {
            this.SpawnRecord = spawnRecord;
            this.ActionsRecord = NpcActionRecord.GetActions(SpawnRecord.Id);
            this.Map = MapRecord.GetMap(spawnRecord.MapId);
            this.m_Id = this.Map.Instance.PopNextNPEntityId();
        }

        // Retourne l'action du PNJ correspondant au type d'interaction demandé
        private NpcActionRecord GetAction(NpcActionTypeEnum actionType)
        {
            return ActionsRecord.Find(x => x.ActionIdEnum == actionType);
        }

        // Traite une interaction du joueur avec ce PNJ.
        // Priorité : cinématique scriptée (CinematicProvider) > action standard (NpcActionProvider)
        public void InteractWith(Character character, NpcActionTypeEnum actionType)
        {
            if (character.Busy)
                return;

            // Si une cinématique est associée à ce PNJ, l'orienter vers le joueur puis lancer la cinématique
            if (actionType == NpcActionTypeEnum.Talk && CinematicProvider.Instance.IsNpcHandled(character, SpawnRecord.Id))
            {
                var npcPoint = new Maps.MapPoint((short)this.CellId);
                character.SetDirection(character.Point.OrientationTo(npcPoint,true));
                character.RandomTalkEmote();
                CinematicProvider.Instance.TalkToNpc(character, SpawnRecord.Id);
                return;
            }

            NpcActionRecord action = GetAction(actionType);

            if (action != null)
            {
                NpcActionProvider.Handle(character, this, action);
            }
            else if (character.Client.Account.Role > ServerRoleEnum.Player)
            {
                // Message de débogage pour les GMs : action non configurée
                character.Reply("No (" + actionType + ") action linked to this npc...(" + SpawnRecord.Id + ")");
            }
        }

        // Sérialise le PNJ pour le protocole client.
        // Si le PNJ n'a qu'une seule action de type Talk, affiche l'icône de quête (GameRolePlayNpcWithQuestInformations).
        public override GameRolePlayActorInformations GetActorInformations()
        {
            if (SpawnRecord.Template.ActionTypesEnum.Contains(NpcActionTypeEnum.Talk) && SpawnRecord.Template.ActionTypesEnum.Count == 1)
            {
                return new GameRolePlayNpcWithQuestInformations(Id, Look.ToEntityLook(), new EntityDispositionInformations((short)SpawnRecord.CellId, SpawnRecord.Direction), Template.Id, true, 0, new GameRolePlayNpcQuestFlag(new ushort[] { 0 }, new ushort[] { 3 }));
            }
            else
            {
                return new GameRolePlayNpcInformations((double)Id, Template.Look.ToEntityLook(), new EntityDispositionInformations((short)SpawnRecord.CellId, SpawnRecord.Direction),
                     Template.Id, true, 0);
            }
        }
        public override string ToString()
        {
            return "Npc (" + Name + ") (SpawnId:" + SpawnRecord.Id + ") (CellId:" + CellId + ")";
        }
    }
}
