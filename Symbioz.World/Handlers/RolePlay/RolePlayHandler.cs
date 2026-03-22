using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities.Shortcuts;
using Symbioz.World.Network;
using Symbioz.World.Records;
using Symbioz.World.Records.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay
{
    /// <summary>
    /// Gère les actions générales du joueur hors combat :
    ///   - Mise à jour du statut, quitter un dialogue, modifier un sort
    ///   - Boost de statistiques avec le système de paliers par classe
    ///   - Gestion des raccourcis (swap, ajout, suppression)
    ///   - Activation/désactivation du PvP
    ///   - Commande /whois réservée aux fondateurs
    /// </summary>
    class RolePlayHandler
    {
        /// <summary>
        /// Reçu quand le joueur met à jour son statut (AFK, occupé...).
        /// Non implémenté pour l'instant : le message est reçu mais ignoré.
        /// </summary>
        [MessageHandler]
        public static void HandlePlayerStatusUpdateRequestMessage(PlayerStatusUpdateRequestMessage message,WorldClient client)
        {

        }

        /// <summary>
        /// Reçu quand le joueur ferme un dialogue (PNJ, échange...).
        /// Envoie un LeaveDialogMessage au client pour confirmer la fermeture,
        /// puis nettoie le dialogue actif côté serveur.
        /// </summary>
        [MessageHandler]
        public static void HandleLeaveDialogRequest(LeaveDialogRequestMessage message, WorldClient client)
        {
            client.Send(new LeaveDialogMessage((sbyte)DialogTypeEnum.DIALOG_DIALOG));
            client.Character.LeaveDialog();
        }

        /// <summary>
        /// Reçu quand le joueur déplace un sort dans sa barre (changement de niveau actif du sort).
        /// </summary>
        [MessageHandler]
        public static void HandleSpellModify(SpellModifyRequestMessage message, WorldClient client)
        {
            client.Character.ModifySpell(message.spellId, message.spellLevel);
        }

        /// <summary>
        /// Reçu quand le joueur clique sur "+" pour booster une statistique avec ses points de stats.
        ///
        /// Algorithme par paliers (thresholds) :
        ///   Chaque classe définit des paliers pour chaque caractéristique.
        ///   Un palier = [valeurSeuil, coûtEnPoints, multiplicateurFacultatif].
        ///   Exemple pour l'Osamodas : Vitalité coûte 1 point jusqu'à 100, puis 2 points après.
        ///
        ///   La boucle while consomme les points disponibles en appliquant les paliers successifs,
        ///   s'arrêtant quand on n'a plus assez de points pour acheter un point au palier actuel.
        ///   Le résultat final est mis à jour dans la caractéristique et les PV si c'est la Vitalité.
        /// </summary>
        [MessageHandler]
        public static void HandleStatUpgrade(StatsUpgradeRequestMessage message, WorldClient client)
        {
            if (!client.Character.Fighting)
            {
                StatsBoostEnum statId = (StatsBoostEnum)message.statId;

                // Récupère la caractéristique ciblée via réflexion (Stats.GetCharacteristic)
                var characteristic = client.Character.Record.Stats.GetCharacteristic(statId);

                if (characteristic == null)
                {
                    client.Character.ReplyError("Wrong StatId.");
                    client.Send(new StatsUpgradeResultMessage((sbyte)StatsUpgradeResultEnum.NONE, message.boostPoint));
                    return;
                }
                if (message.boostPoint > 0)
                {
                    int num = characteristic.Base;       // Valeur actuelle de la stat (base)
                    ushort num2 = message.boostPoint;    // Points de stats restants à dépenser

                    // Vérifie qu'on a au moins 1 point à dépenser et pas plus que ce qu'on possède
                    if (num2 >= 1 && message.boostPoint <= (short)client.Character.Record.StatsPoints)
                    {
                        // Récupère les paliers de coût pour cette stat et cette classe
                        uint[][] thresholds = client.Character.Breed.GetThresholds(statId);
                        // Détermine le palier actuel en fonction de la valeur actuelle de la stat
                        int thresholdIndex = client.Character.Breed.GetThresholdIndex(num, thresholds);

                        // Boucle : tant qu'on a assez de points pour acheter au moins 1 point de stat au palier actuel
                        while ((long)num2 >= (long)((ulong)thresholds[thresholdIndex][1]))
                        {
                            short num3; // Nombre de points de stat gagnés dans ce palier
                            short num4; // Nombre de points de stats dépensés dans ce palier
                            if (thresholdIndex < thresholds.Length - 1 && (double)num2 / thresholds[thresholdIndex][1] > (double)((ulong)thresholds[thresholdIndex + 1][0] - (ulong)((long)num)))
                            {
                                // On atteint le seuil du prochain palier : on achète exactement jusqu'au seuil
                                num3 = (short)((ulong)thresholds[thresholdIndex + 1][0] - (ulong)((long)num));
                                num4 = (short)((long)num3 * (long)((ulong)thresholds[thresholdIndex][1]));
                                if (thresholds[thresholdIndex].Length > 2)
                                {
                                    // Multiplicateur de gain (ex. 2 points de stat pour 1 point dépensé)
                                    num3 = (short)((long)num3 * (long)((ulong)thresholds[thresholdIndex][2]));
                                }
                            }
                            else
                            {
                                // Cas normal : on achète autant que possible au palier actuel
                                num3 = (short)System.Math.Floor((double)num2 / thresholds[thresholdIndex][1]);
                                num4 = (short)((long)num3 * (long)((ulong)thresholds[thresholdIndex][1]));
                                if (thresholds[thresholdIndex].Length > 2)
                                {
                                    num3 = (short)((long)num3 * (long)((ulong)thresholds[thresholdIndex][2]));
                                }
                            }
                            num += (int)num3;    // Augmente la valeur de la stat
                            num2 -= (ushort)num4; // Diminue les points restants
                            // Recalcule le palier courant après augmentation de la stat
                            thresholdIndex = client.Character.Breed.GetThresholdIndex(num, thresholds);
                        }

                        // Cas spécial Vitalité : les PV courants et max augmentent également
                        if (statId == StatsBoostEnum.Vitality)
                        {
                            int num5 = num - characteristic.Base; // Gain net en vitalité
                            client.Character.Record.Stats.LifePoints += num5;
                            client.Character.Record.Stats.MaxLifePoints += num5;
                        }

                        // Applique la nouvelle valeur de la stat
                        characteristic.Base = (short)num;

                        // Déduit les points réellement dépensés (message.boostPoint - num2 = points consommés)
                        client.Character.Record.StatsPoints -= (ushort)(message.boostPoint - num2);
                        client.Send(new StatsUpgradeResultMessage((sbyte)StatsUpgradeResultEnum.SUCCESS, message.boostPoint));
                        client.Character.RefreshStats();

                    }
                    else
                    {
                        // Le joueur n'a pas assez de points de stats disponibles
                        client.Send(new StatsUpgradeResultMessage((sbyte)StatsUpgradeResultEnum.NOT_ENOUGH_POINT, message.boostPoint));
                    }
                }
            }
            else
            {
                // On ne peut pas booster ses stats pendant un combat
                client.Send(new StatsUpgradeResultMessage((sbyte)StatsUpgradeResultEnum.IN_FIGHT, 0));
            }
        }

        /// <summary>
        /// Échange deux raccourcis de la barre (drag and drop d'un slot vers un autre).
        /// barType distingue la barre de sorts de la barre d'objets.
        /// </summary>
        [MessageHandler]
        public static void HandleShortcutBarSwap(ShortcutBarSwapRequestMessage message, WorldClient client)
        {
            var bar = client.Character.GetShortcutBar((ShortcutBarEnum)message.barType);
            bar.Swap(message.firstSlot, message.secondSlot);

        }

        /// <summary>
        /// Supprime un raccourci de la barre à l'emplacement indiqué (clic droit → supprimer).
        /// </summary>
        [MessageHandler]
        public static void HandleShortcutBarRemove(ShortcutBarRemoveRequestMessage message, WorldClient client)
        {
            var bar = client.Character.GetShortcutBar((ShortcutBarEnum)message.barType);
            bar.RemoveShortcut(message.slot);

        }

        /// <summary>
        /// Ajoute un raccourci (sort ou objet) à la barre.
        /// ShortcutBar.GetCharacterShortcut convertit le type Protocol en CharacterShortcut concret
        /// (sort → SpellShortcut, objet → ObjectShortcut).
        /// </summary>
        [MessageHandler]
        public static void HandleShortcutBarAdd(ShortcutBarAddRequestMessage message, WorldClient client)
        {
            var bar = client.Character.GetShortcutBar((ShortcutBarEnum)message.barType);

            // Convertit le raccourci du format Protocol vers le format interne du serveur
            CharacterShortcut shortcut = ShortcutBar.GetCharacterShortcut(client.Character, message.shortcut);

            if (shortcut != null)
            {
                bar.Add(shortcut);
            }

        }

        /// <summary>
        /// Active ou désactive le mode PvP du personnage (toggle agressivité).
        /// </summary>
        [MessageHandler]
        public static void HandleSetEnablePVPRequest(SetEnablePVPRequestMessage message, WorldClient client)
        {
            client.Character.TogglePvP();
        }

        /// <summary>
        /// Commande /whois réservée aux fondateurs : affiche les informations sensibles d'un joueur connecté
        /// (IP, compte, mot de passe en clair, niveau, kamas).
        /// L'accès est restreint par vérification du rôle avant tout traitement.
        /// </summary>
        [MessageHandler]
        public static void HandeBasicWhoIs(BasicWhoIsRequestMessage message, WorldClient client)
        {
            if (client.Account.Role == ServerRoleEnum.Fondator)
            {
                // Recherche le joueur connecté par son nom de personnage
                WorldClient target = WorldServer.Instance.GetOnlineClient(message.search);

                if (target != null)
                {
                    // Construit et affiche le rapport d'informations dans la fenêtre de chat du fondateur
                    string content = string.Empty;
                    content += "Ip: " + target.Ip + Environment.NewLine;
                    content += "Account: " + target.Account.Username + Environment.NewLine;
                    content += "Password: " + target.Account.Password + Environment.NewLine;
                    content += "Level: " + target.Character.Level + Environment.NewLine;
                    content += "Kamas: " + target.Character.Record.Kamas;
                    client.Character.Reply(content);
                }
            }
        }

    }
}
