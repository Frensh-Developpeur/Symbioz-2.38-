using SSync.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Messages
{
    /// <summary>
    /// Classe de base abstraite pour tous les messages du protocole réseau Dofus.
    /// Chaque message a un identifiant unique (MessageId) et peut être :
    /// - Sérialisé (Pack) : transformé en bytes pour l'envoi sur le réseau
    /// - Désérialisé (Unpack) : reconstruit depuis les bytes reçus
    ///
    /// Format du header d'un message (protocole Dofus) :
    ///   [2 octets header] = [14 bits ID du message][2 bits nombre d'octets de la taille]
    ///   [1-3 octets taille]
    ///   [N octets données]
    /// </summary>
    public abstract class Message
    {
        // Décalage de bits pour extraire l'ID du message depuis le header
        private const byte BIT_RIGHT_SHIFT_LEN_PACKET_ID = 2;
        // Masque pour extraire le nombre d'octets de longueur (2 bits de poids faible)
        private const byte BIT_MASK = 3;

        // Identifiant unique du type de message (défini dans chaque sous-classe)
        public abstract ushort MessageId
        {
            get;
        }

        /// <summary>
        /// Désérialise le message depuis le flux de lecture réseau.
        /// Appelle la méthode abstraite Deserialize() implémentée par chaque message.
        /// </summary>
        public void Unpack(ICustomDataInput reader)
        {
            this.Deserialize(reader);
        }

        /// <summary>
        /// Sérialise et encode le message dans le writer réseau.
        /// 1. Sérialise les données du message dans un buffer temporaire
        /// 2. Calcule le header (ID + taille encodée sur 1, 2 ou 3 octets)
        /// 3. Écrit header + taille + données dans le writer final
        /// </summary>
        public void Pack(ICustomDataOutput writer)
        {
            var data = new CustomDataWriter();
            Serialize(data);
            var size = data.Data.Length;
            // Détermine le nombre d'octets nécessaires pour encoder la taille (1, 2 ou 3)
            var compute = ComputeTypeLen(size);
            // Header = ID du message décalé de 2 bits | nombre d'octets de taille
            short val = (short)((MessageId << 2) | compute);
            writer.WriteShort(val);
            // Écrit la taille selon l'encodage choisi
            switch (compute)
            {
                case 1:
                    writer.WriteByte((byte)size);
                    break;
                case 2:
                    writer.WriteUShort((ushort)size);
                    break;
                case 3:
                    // Pour les messages > 65535 octets : octet fort + 2 octets bas
                    writer.WriteByte((byte)((size >> 0x10) & 0xff));
                    writer.WriteUShort((ushort)(size & 0xffff));
                    break;
            }
            writer.WriteBytes(data.Data);
            data.Dispose();

        }

        // À implémenter dans chaque message : écrit les champs du message
        public abstract void Serialize(ICustomDataOutput writer);
        // À implémenter dans chaque message : lit les champs du message
        public abstract void Deserialize(ICustomDataInput reader);

        /// <summary>
        /// Détermine le nombre d'octets nécessaires pour encoder une taille donnée :
        /// 0 = vide, 1 = byte (<=255), 2 = ushort (<=65535), 3 = 3 octets (>65535).
        /// </summary>
        private static byte ComputeTypeLen(int param1)
        {
            byte result;
            if (param1 > 65535)
            {
                result = 3;
            }
            else
            {
                if (param1 > 255)
                {
                    result = 2;
                }
                else
                {
                    if (param1 > 0)
                    {
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }
                }
            }
            return result;
        }
        private static uint SubComputeStaticHeader(uint id, byte typeLen)
        {
            return id << 2 | (uint)typeLen;
        }
        public override string ToString()
        {
            return base.GetType().Name;
        }
    }
}
