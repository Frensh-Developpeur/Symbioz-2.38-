using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Network;
using Symbioz.World.Providers.Maps.Path;
using Symbioz.World.Records.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.Fight
{
    /// <summary>
    /// Handler des messages réseau liés au combat (actions des joueurs pendant le combat).
    /// Chaque méthode est déclenchée quand le client envoie le message correspondant.
    /// [MessageHandler] = attribut qui lie automatiquement la méthode au type de message.
    /// </summary>
    class FightHandler
    {
        /// <summary>
        /// Reçu quand un joueur confirme qu'il est prêt pour le prochain tour (synchronisation).
        /// Permet au Synchronizer de savoir que ce client a bien reçu les actions précédentes.
        /// </summary>
        [MessageHandler]
        public static void HandleTurnReady(GameFightTurnReadyMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
                client.Character.FighterMaster.ToggleSyncReady(message.isReady);
        }

        /// <summary>
        /// Reçu quand un joueur clique sur "Fin de tour".
        /// Ne traite la demande que si le combat n'est pas terminé ET qu'aucune séquence n'est en cours
        /// (pour éviter de passer le tour au milieu d'une animation).
        /// </summary>
        [MessageHandler]
        public static void HandleFightTurnFinishMessage(GameFightTurnFinishMessage message, WorldClient client)
        {
            if (client.Character.Fighting && !client.Character.Fighter.Fight.Ended && !client.Character.Fighter.Fight.SequencesManager.Sequencing)
            {
                client.Character.Fighter.PassTurn();
            }
        }

        /// <summary>
        /// Accusé de réception d'une action de combat. Non implémenté pour l'instant.
        /// </summary>
        [MessageHandler]
        public static void HandleGameActionAcknowledgement(GameActionAcknowledgementMessage message, WorldClient client)
        {


        }

        /// <summary>
        /// Reçu quand un joueur tente de lancer un sort sur une cellule.
        /// Récupère le sort du personnage et demande le lancer au fighter.
        /// </summary>
        [MessageHandler]
        public static void HandleGameActionFightSpellCastRequest(GameActionFightCastRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
            {
                // Cherche le sort dans la liste des sorts connus du personnage
                CharacterSpell spell = client.Character.Fighter.GetSpell(message.spellId);

                if (spell != null)
                    // Lance le sort sur la cellule cible avec le niveau (grade) actuel du sort
                    client.Character.Fighter.CastSpell(spell.Template, spell.Grade, message.cellId);
            }
        }

        /// <summary>
        /// Reçu quand un joueur tente de lancer un sort directement sur une cible (par ID).
        /// Vérifie que la cible existe et peut être ciblée avant de lancer le sort.
        /// </summary>
        [MessageHandler]
        public static void HandleGameActionFightCastOnTargetRequest(GameActionFightCastOnTargetRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
            {
                CharacterSpell spell = client.Character.Fighter.GetSpell(message.spellId);
                // Récupère le fighter cible par son identifiant dans la liste des combattants du combat
                Fighter fighter = client.Character.Fighter.Fight.GetFighter((int)message.targetId);

                if (spell != null && fighter != null)
                {
                    // Vérifie que la cible peut être ciblée par ce joueur (alignement, invocations...)
                    if (fighter.CanBeTargeted(client.Character))
                        client.Character.Fighter.CastSpell(spell.Template, spell.Grade, fighter.CellId, fighter.Id);
                }
                else
                {
                    client.Character.ReplyError("Fatal error while casting spell!!");
                }
            }
        }

        /// <summary>
        /// Reçu quand un joueur demande la liste des cibles d'un challenge (défi de combat).
        /// Un challenge est un objectif bonus (ex. "tuer le boss en dernier") qui donne des récompenses supplémentaires.
        /// </summary>
        [MessageHandler]
        public static void HandleChallengeTargetsListRequest(ChallengeTargetsListRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
            {
                client.Character.Fighter.ShowChallengeTargetsList(message.challengeId);
            }
        }

        /// <summary>
        /// Reçu quand un joueur "montre" une cellule à son équipe (indicateur visuel partagé).
        /// Utile en équipe pour coordonner les attaques ou signaler un danger sur une case précise.
        /// </summary>
        [MessageHandler]
        public static void HandleShowCellRequest(ShowCellRequestMessage message, WorldClient client)
        {
            if (client.Character.Fighting)
            {
                client.Character.Fighter.Team.ShowCell(client.Character.Fighter, message.cellId);
            }
        }
    }
}
