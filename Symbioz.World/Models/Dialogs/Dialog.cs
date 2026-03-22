using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Entities;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Dialogs
{
    /// <summary>
    /// Classe de base abstraite pour tous les dialogues du jeu.
    /// Un "dialogue" représente une interaction entre un joueur et un élément du jeu :
    ///   - Discussion avec un PNJ (NpcTalkDialog)
    ///   - Zaap ou Zaapi (TeleporterDialog)
    ///   - Coffre de guilde, banque, échange entre joueurs, etc.
    ///
    /// Le dialogue est stocké dans Character.Dialog et empêche le joueur de faire
    /// d'autres actions pendant qu'il est ouvert (voir Character.Busy).
    /// Open() déclenche l'affichage côté client, Close() réinitialise l'état.
    /// </summary>
    public abstract class Dialog
    {
        /// <summary>
        /// Le personnage joueur qui a ouvert ce dialogue.
        /// </summary>
        public Character Character { get; set; }

        /// <summary>
        /// Constructeur de base : associe ce dialogue au personnage donné.
        /// </summary>
        public Dialog(Character character)
        {
            this.Character = character;
        }

        /// <summary>
        /// Type de dialogue au sens du protocole Dofus (DIALOG_DIALOG, DIALOG_EXCHANGE, etc.).
        /// Utilisé dans le message LeaveDialogMessage pour indiquer au client quel dialogue se ferme.
        /// </summary>
        public abstract DialogTypeEnum DialogType { get; }

        /// <summary>
        /// Ouvre le dialogue et envoie les messages d'initialisation au client.
        /// Implémenté dans chaque sous-classe.
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// Ferme le dialogue : efface Character.Dialog et signale la fermeture au client.
        /// Les sous-classes peuvent surcharger cette méthode pour des nettoyages supplémentaires.
        /// </summary>
        public virtual void Close()
        {
            // Réinitialise la référence au dialogue dans le client (le joueur n'est plus "occupé")
            Character.Client.Character.Dialog = null;
        }

        /// <summary>
        /// Envoie au client le message indiquant que le dialogue est terminé.
        /// Appelé par Close() dans les sous-classes.
        /// </summary>
        protected void LeaveDialogMessage()
        {
            Character.Client.Send(new LeaveDialogMessage((sbyte)DialogType));
        }
    }
}
