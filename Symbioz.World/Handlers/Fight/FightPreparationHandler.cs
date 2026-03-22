using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Dialogs.DialogBox;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Fights;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Network;
using Symbioz.World.Providers.Fights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.Fight
{
    /// <summary>
    /// Handler de la phase de préparation de combat.
    /// Gère toutes les actions possibles pendant la phase de placement (avant le début du combat) :
    ///   - Changement de position ou échange de position avec un allié
    ///   - Abandon du combat (quitter avant le lancement)
    ///   - Rejoindre un combat existant comme spectateur ou participant
    ///   - Expulsion d'un allié (kick par le chef d'équipe)
    ///   - Activation des options de combat (verrouillage, spectateurs...)
    ///   - Signalement de prêt (toggle ready)
    ///   - Défi PvP entre joueurs (amical ou agression)
    /// </summary>
    class FightPreparationHandler
    {
        /// <summary>
        /// Échange la position de placement entre deux alliés pendant la phase de placement.
        /// Vérifie que le joueur n'est pas encore prêt et que le combat n'a pas démarré,
        /// puis contrôle que la cellule demandée est bien celle occupée par la cible.
        /// </summary>
        [MessageHandler]
        public static void HandleGameFightPlacementSwapPositionsRequest(GameFightPlacementSwapPositionsRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting && !client.Character.FighterMaster.IsReady && !client.Character.FighterMaster.Fight.Started)
            {
                var target = client.Character.FighterMaster.Fight.GetFighter((int)message.requestedId);

                // Vérifie que la cellule demandée est bien celle de la cible (pas de triche de position)
                if (target.CellId == message.cellId)
                {
                    target.PlacementSwap(client.Character.FighterMaster);
                }
            }
        }

        /// <summary>
        /// Déplace le joueur sur la cellule demandée pendant la phase de placement.
        /// Vérifie que le joueur n'est pas encore prêt et que la cellule cible est libre.
        /// </summary>
        [MessageHandler]
        public static void HandleGameFightPlacementPositionRequest(GameFightPlacementPositionRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting && !client.Character.FighterMaster.IsReady && client.Character.Fighter.Fight.IsCellFree((short)message.cellId))
            {
                client.Character.FighterMaster.ChangePlacementPosition((short)message.cellId);
            }
        }

        /// <summary>
        /// Reçu quand un joueur quitte volontairement le combat (pendant la phase de préparation ou le combat).
        /// Le paramètre true indique que c'est une sortie volontaire (pas une déconnexion).
        /// </summary>
        [MessageHandler]
        public static void HandleGameContextQuitMessage(GameContextQuitMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
                client.Character.FighterMaster.Leave(true);
        }

        /// <summary>
        /// Reçu quand un joueur demande à rejoindre un combat en cours (comme spectateur ou combattant).
        /// Récupère le combat sur la carte du joueur, puis tente de l'y faire entrer.
        /// </summary>
        [MessageHandler]
        public static void HandleGameFightJoinRequestMessage(GameFightJoinRequestMessage message, WorldClient client)
        {
            // Recherche le combat par son ID sur la map courante du joueur
            var fight = client.Character.Map.Instance.GetFight(message.fightId);

            if (fight != null)
            {
                // fighterId = ID du combattant déjà dans le combat que le joueur veut rejoindre (son équipe)
                fight.TryJoin(client.Character, (int)message.fighterId);
            }
        }

        /// <summary>
        /// Reçu quand le chef d'équipe expulse un allié du combat pendant la phase de placement.
        /// Vérifie que l'expulseur est bien le chef de l'équipe et que la cible est un allié.
        /// </summary>
        [MessageHandler]
        public static void HandleGameContextKickMessage(GameContextKickMessage message, WorldClient client)
        {
            var target = client.Character.Fighter.Fight.GetFighter((int)message.targetId);

            // Seul le chef d'équipe peut expulser un allié (IsFriendly = même équipe)
            if (client.Character.Fighting && target != null && client.Character.Fighter.IsTeamLeader && client.Character.Fighter.IsFriendly(target))
            {
                target.Kick();
            }
        }

        /// <summary>
        /// Active ou désactive une option de combat pour l'équipe (spectateurs autorisés, combat verrouillé...).
        /// Seul le chef d'équipe peut modifier ces options.
        /// </summary>
        [MessageHandler]
        public static void HandleFightOptionToggle(GameFightOptionToggleMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
                client.Character.Fighter.Team.Options.ToggleOption((FightOptionsEnum)message.option);
        }

        /// <summary>
        /// Reçu quand un joueur signale qu'il est prêt (ou annule son prêt) pour lancer le combat.
        /// Quand tous les joueurs d'une équipe sont prêts, le combat peut démarrer automatiquement.
        /// </summary>
        [MessageHandler]
        public static void HandleGameFightReady(GameFightReadyMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
                client.Character.FighterMaster.ToggleReady(message.isReady);
        }

        /// <summary>
        /// Demande de combat PvP d'un joueur vers un autre.
        /// Si "friendly" = défi amical (boîte de dialogue d'acceptation) ;
        /// Sinon = agression (démarrage immédiat si les conditions sont remplies : alignements, zone PvP...).
        /// </summary>
        [MessageHandler]
        public static void HandleGameRolePlayPlayerFightRequest(GameRolePlayPlayerFightRequestMessage message, WorldClient client)
        {
            // Récupère le personnage cible sur la map courante
            Character target = client.Character.Map.Instance.GetEntity<Character>((long)message.targetId);

            if (target != null)
            {
                if (message.friendly)
                {
                    // Vérifie si le combat amical est autorisé (même map, pas déjà en combat...)
                    FighterRefusedReasonEnum fighterRefusedReasonEnum = client.Character.CanRequestFight(target);
                    if (fighterRefusedReasonEnum != FighterRefusedReasonEnum.FIGHTER_ACCEPTED)
                    {
                        // Informe l'initiateur de la raison du refus
                        client.Send(new ChallengeFightJoinRefusedMessage((ulong)client.Character.Id, (sbyte)fighterRefusedReasonEnum));
                    }
                    else
                    {
                        // Ouvre une boîte de dialogue pour que la cible accepte ou refuse le défi
                        target.OpenRequestBox(new DualRequest(client.Character, target));
                    }
                }
                else
                {
                    // Vérifie si l'agression est autorisée (alignements, zone PvP...)
                    FighterRefusedReasonEnum fighterRefusedReasonEnum = client.Character.CanAgress(target);
                    if (fighterRefusedReasonEnum != FighterRefusedReasonEnum.FIGHTER_ACCEPTED)
                    {
                        client.Send(new ChallengeFightJoinRefusedMessage((ulong)client.Character.Id, (sbyte)fighterRefusedReasonEnum));
                    }
                    else
                    {
                        // Crée le combat d'agression et démarre immédiatement la phase de placement
                        FightAgression fight = FightProvider.Instance.CreateFightAgression(client.Character, target, (short)client.Character.CellId);

                        // Ajoute la cible dans l'équipe rouge et l'agresseur dans l'équipe bleue
                        fight.RedTeam.AddFighter(target.CreateFighter(fight.RedTeam));
                        fight.BlueTeam.AddFighter(client.Character.CreateFighter(fight.BlueTeam));

                        fight.StartPlacement();
                    }
                }
            }
        }

        /// <summary>
        /// Réponse du joueur ciblé à une demande de duel amical (accepter/refuser/annuler).
        /// Distingue si c'est la cible qui refuse ou l'initiateur qui annule pour appeler la bonne méthode.
        /// </summary>
        [MessageHandler]
        public static void HandleGameRolePlayPlayerFightFriendlyAnswer(GameRolePlayPlayerFightFriendlyAnswerMessage message, WorldClient client)
        {
            if (client.Character.IsInRequest() && client.Character.RequestBox is DualRequest)
            {
                if (message.accept)
                {
                    client.Character.RequestBox.Accept();
                }
                else
                {
                    if (client.Character == client.Character.RequestBox.Target)
                    {
                        client.Character.RequestBox.Deny(); // La cible refuse le défi
                    }
                    else
                    {
                        client.Character.RequestBox.Cancel(); // L'initiateur annule sa propre demande
                    }
                }
            }

        }
    }
}
