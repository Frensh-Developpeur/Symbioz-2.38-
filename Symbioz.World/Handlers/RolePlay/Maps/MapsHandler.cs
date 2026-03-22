using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Dialogs;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Fights;
using Symbioz.World.Models.Maps;
using Symbioz.World.Network;
using Symbioz.World.Providers.Fights;
using Symbioz.World.Providers.Maps;
using Symbioz.World.Providers.Maps.Path;
using Symbioz.World.Records;
using Symbioz.World.Records.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay
{
    /// <summary>
    /// Handler des messages liés à la navigation sur les maps :
    ///   - Déplacement du personnage (mouvement, annulation, confirmation)
    ///   - Changement de map (scroll entre maps adjacentes)
    ///   - Attaque d'un groupe de monstres (démarrage d'un combat PvM)
    ///   - Jouer des emotes, changer d'orientation
    ///   - Téléportation via zaap ou zaapi
    ///   - Consultation de la liste des combats en cours sur la map
    /// </summary>
    class MapsHandler
    {
        // Envoie la liste des combats actuellement en cours sur la map du joueur
        [MessageHandler]
        public static void HandleMapRunningFightList(MapRunningFightListRequestMessage message, WorldClient client)
        {
            client.Send(new MapRunningFightListMessage(client.Character.Map.Instance.GetFightsExternalInformations()));
        }
        // Détails d'un combat en cours (non implémenté)
        [MessageHandler]
        public static void HandleMapRunning(MapRunningFightDetailsRequestMessage message, WorldClient client)
        {
            // var fight = client.Character.Map.Instance.GetFight(message.fightId);
            //client.Send(new MapRunningFightDetailsMessage(message.fightId, fight.RedTeam.GetFighterLightInformations(),
            //    fight.BlueTeam.GetFighterLightInformations()));
        }
        /// <summary>
        /// Demande d'attaque d'un groupe de monstres par le joueur :
        /// 1. Vérifie que le groupe est bien sur la même cellule que le joueur
        /// 2. Vérifie que la map a assez de cellules de placement pour l'équipe
        /// 3. Retire le groupe de la map, crée le combat PvM, place tous les fighters, démarre la phase de placement
        /// </summary>
        [MessageHandler]
        public static void HandleGameRolePlayAttackMonsterRequest(GameRolePlayAttackMonsterRequestMessage message, WorldClient client)
        {
            if (client.Character.Map != null)
            {
                MonsterGroup group = client.Character.Map.Instance.GetEntity<MonsterGroup>((long)message.monsterGroupId);

                if (group != null && client.Character.CellId == group.CellId)
                {
                    // Vérifie qu'il y a assez de cellules de placement pour les deux équipes
                    if (client.Character.Map.BlueCells.Count >= group.MonsterCount && client.Character.Map.RedCells.Count() >= client.Character.FighterCount)
                    {
                        // Retire le groupe de monstres de la map (il ne se déplace plus pendant le combat)
                        client.Character.Map.Instance.RemoveEntity(group);

                        FightPvM fight = FightProvider.Instance.CreateFightPvM(group, client.Character.Map, (short)group.CellId);

                        fight.RedTeam.AddFighter(client.Character.CreateFighter(fight.RedTeam));

                        foreach (var fighter in group.CreateFighters(fight.BlueTeam))
                        {
                            fight.BlueTeam.AddFighter(fighter);
                        }

                        fight.StartPlacement();
                    }
                    else
                    {
                        client.Character.ReplyError("Unable to fight on this map");
                    }
                }
                else
                {
                    client.Character.NoMove();
                }
            }
        }
        // Chargement de la map : le client demande les données de la map pour l'afficher
        // Si la map n'existe pas, téléporte au point de spawn par défaut
        [MessageHandler]
        public static void HandleMapGetInformation(MapInformationsRequestMessage message, WorldClient client)
        {
            if (client.Character.Record.MapId == message.mapId)
            {
                client.Character.Map = MapRecord.GetMap(message.mapId);

                if (client.Character.Map == null)
                {
                    client.Character.SpawnPoint();
                    client.Character.Reply("Unknown Map...(" + message.mapId + ")");
                    return;
                }

                client.Character.OnEnterMap();
            }
        }
        // Joue un emote (animation) si le personnage n'est pas en combat
        [MessageHandler]
        public static void HandleEmotePlay(EmotePlayRequestMessage message, WorldClient client)
        {
            if (!client.Character.Fighting)
                client.Character.PlayEmote(message.emoteId);
        }
        /// <summary>
        /// Demande de déplacement du personnage :
        /// - En combat : décode le chemin de combat via PathParser et déplace le fighter
        /// - Hors combat : déplace le personnage normalement sur la map
        /// </summary>
        [MessageHandler]
        public static void HandleMapMovementRequest(GameMapMovementRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
            {
                if (client.Character.Fighter.Fight.Started && client.Character.Fighter.IsFighterTurn)
                {
                    // Décode les keyMovements (format compressé du client) en liste de cellules
                    List<short> path = PathParser.FightMove(PathParser.ReturnDispatchedCells(message.keyMovements)).Keys.ToList();
                    path.Insert(0, (short)client.Character.Fighter.CellId); // Ajoute la cellule de départ
                    client.Character.Fighter.Move(path);
                }
            }
            else
            {
                if (!client.Character.ChangeMap && client.Character.Map.Id == message.mapId && !client.Character.Collecting)
                    client.Character.MoveOnMap(message.keyMovements);
                else
                    client.Character.NoMove(); // Ne peut pas bouger (changement de map en cours, etc.)
            }
        }
        // Le joueur annule son déplacement : met à jour la cellule dans le record
        [MessageHandler]
        public static void HandleMapMovementCancel(GameMapMovementCancelMessage message, WorldClient client)
        {
            client.Character.Record.CellId = message.cellId;
            client.Send(new BasicNoOperationMessage());

        }
        // Le déplacement est terminé côté client : appelle EndMove pour traiter l'arrivée
        [MessageHandler]
        public static void HandleMapMovementConfirm(GameMapMovementConfirmMessage message, WorldClient client)
        {
            if (client.Character.IsMoving)
                client.Character.EndMove();
        }
        // Change l'orientation du personnage (vers où il regarde sur la map)
        [MessageHandler]
        public static void HandleGameMapChangeOriantation(GameMapChangeOrientationRequestMessage message, WorldClient client)
        {
            client.Character.SetDirection((DirectionsEnum)message.direction);

        }
        // Téléportation via un portail de déplacement (zaap ou zaapi)
        [MessageHandler]
        public static void HandleTeleportRequest(TeleportRequestMessage message, WorldClient client)
        {
            switch ((TeleporterTypeEnum)message.teleporterType)
            {
                case TeleporterTypeEnum.TELEPORTER_ZAAP:
                    if (client.Character.GetDialog<ZaapDialog>() != null)
                        client.Character.GetDialog<ZaapDialog>().Teleport(MapRecord.GetMap(message.mapId));
                    break;
                case TeleporterTypeEnum.TELEPORTER_SUBWAY:
                    if (client.Character.GetDialog<ZaapiDialog>() != null)
                        client.Character.GetDialog<ZaapiDialog>().Teleport(MapRecord.GetMap(message.mapId));
                    break;
            }
        }
        /// <summary>
        /// Changement de map via scroll (bords de map).
        /// Détermine la direction de scroll (gauche/droite/haut/bas) en comparant l'ID de la map cible
        /// avec les maps adjacentes connues. Utilise ScrollActionRecord pour calculer la cellule d'arrivée
        /// et gérer les overrides (redirections de maps définies en base).
        /// </summary>
        [MessageHandler]
        public static void ChangeMapMessage(ChangeMapMessage message, WorldClient client)
        {
            MapScrollEnum scrollType = MapScrollEnum.UNDEFINED;
            // Identifie la direction de scroll en comparant la map demandée aux maps voisines
            if (client.Character.Map.LeftMap == message.mapId)
                scrollType = MapScrollEnum.Left;
            if (client.Character.Map.RightMap == message.mapId)
                scrollType = MapScrollEnum.Right;
            if (client.Character.Map.DownMap == message.mapId)
                scrollType = MapScrollEnum.Bottom;
            if (client.Character.Map.TopMap == message.mapId)
                scrollType = MapScrollEnum.Top;

            if (scrollType != MapScrollEnum.UNDEFINED)
            {
                // Vérifie si un override redirige vers une map différente
                int overrided = ScrollActionRecord.GetOverrideScrollMapId(client.Character.Map.Id, scrollType);
                ushort cellid = ScrollActionRecord.GetScrollDefaultCellId(client.Character.Record.CellId, scrollType);
                client.Character.Record.Direction = ScrollActionRecord.GetScrollDirection(scrollType);

                int teleportMapId = overrided != -1 ? overrided : message.mapId;
                if (overrided == 0)
                    teleportMapId = message.mapId;
                MapRecord teleportedMap = MapRecord.GetMap(teleportMapId);

                if (teleportedMap != null)
                {
                    // Vérifie que la cellule d'arrivée est accessible, sinon cherche une cellule valide
                    cellid = teleportedMap.Walkable(cellid) ? cellid : ScrollActionRecord.SearchScrollCellId(cellid, scrollType, teleportedMap);
                    client.Character.Teleport(teleportMapId, cellid);
                }
                else
                {
                    client.Character.ReplyError("This map cannot be founded");
                }
            }
            else
            {
                // Direction inconnue : détermine le scroll à partir de la cellule du joueur
                scrollType = ScrollActionRecord.GetScrollTypeFromCell((short)client.Character.Record.CellId);
                if (scrollType == MapScrollEnum.UNDEFINED)
                {
                    client.Character.ReplyError("Unknown Map Scroll Action...");
                }
                else
                {
                    int overrided = ScrollActionRecord.GetOverrideScrollMapId(client.Character.Map.Id, scrollType);
                    ushort cellid = ScrollActionRecord.GetScrollDefaultCellId(client.Character.Record.CellId, scrollType);
                    MapRecord teleportedMap = MapRecord.GetMap(overrided);
                    if (teleportedMap != null)
                    {
                        client.Character.Record.Direction = ScrollActionRecord.GetScrollDirection(scrollType);
                        cellid = teleportedMap.Walkable(cellid) ? cellid : ScrollActionRecord.SearchScrollCellId(cellid, scrollType, teleportedMap);
                        client.Character.Teleport(overrided, cellid);
                    }
                    else
                    {
                        client.Character.ReplyError("This map cannot be founded");
                    }
                }
            }
        }
    }
}
