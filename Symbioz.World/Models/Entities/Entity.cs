using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Entities
{
    /// <summary>
    /// Classe de base pour toute entité visible sur une map de roleplay :
    ///   - Personnages joueurs (Character)
    ///   - Groupes de monstres (MonsterGroup)
    ///   - PNJ (Npc)
    ///   - Portails de zone (Portal)
    ///
    /// Une entité possède un ID unique, un nom, une cellule et une map.
    /// Elle peut envoyer des messages à toute la map (SendMap) ou
    /// des animations de sort (SpellAnim) et smileys.
    /// GetActorInformations() est sérialisée dans le protocole pour informer les clients
    /// de la présence de cette entité sur la map.
    /// </summary>
    public abstract class Entity
    {
        // Identifiant unique de l'entité (négatif pour les entités non-joueurs)
        public abstract long Id
        {
            get;
        }

        // Nom affiché de l'entité
        public abstract string Name
        {
            get;
        }
        // Position de l'entité sur la grille isométrique (calculée depuis CellId)
        public MapPoint Point
        {
            get
            {
                return new MapPoint((short)CellId);
            }
        }
        // Cellule sur laquelle se trouve l'entité
        public abstract ushort CellId
        {
            get;
            set;
        }
        // Map sur laquelle se trouve l'entité
        public MapRecord Map
        {
            get;
            set;
        }

        // Direction vers laquelle regarde l'entité (0-7)
        public abstract DirectionsEnum Direction { get; set; }

        // Apparence visuelle de l'entité
        public abstract ContextActorLook Look { get; set; }

        // Retourne les informations de l'acteur pour la synchronisation client (GameRolePlay)
        public abstract GameRolePlayActorInformations GetActorInformations();

        // Envoie un message à tous les clients présents sur la même map
        public void SendMap(Message message)
        {
            if (Map != null && Map.Instance != null)
                Map.Instance.Send(message);
        }

        // Fait parler l'entité à tous les joueurs de la map
        public void Say(string msg)
        {
            SendMap(new EntityTalkMessage(Id, 4, new string[] { msg }));
        }
        // Fait parler l'entité uniquement à un joueur spécifique
        public void Say(Character character, string msg)
        {
            character.Client.Send(new EntityTalkMessage(Id, 4, new string[] { msg }));
        }

        // Affiche une émoticône au-dessus de l'entité pour tous les joueurs de la map
        public virtual void DisplaySmiley(ushort smileyid)
        {
            SendMap(new ChatSmileyMessage(Id, smileyid, 0));
        }
        // Affiche une émoticône uniquement pour un joueur spécifique
        public void DisplaySmiley(Character character, ushort id)
        {
            character.Client.Send(new ChatSmileyMessage(Id, id, 0));
        }
        // Joue une animation de sort pour un joueur spécifique
        public void SpellAnim(Character character, ushort id)
        {
            character.Client.Send(new GameRolePlaySpellAnimMessage((ulong)Id, CellId, id, 1));
        }
        // Joue une animation de sort pour tous les joueurs de la map
        public void SpellAnim(ushort id)
        {
            SendMap(new GameRolePlaySpellAnimMessage((ulong)Id, CellId, id, 1));
        }

    }
}
