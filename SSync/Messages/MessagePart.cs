

using SSync.IO;
using System;

namespace SSync.Messages
{
    /// <summary>
    /// Analyse le header d'un message réseau Dofus pour en extraire l'ID et la taille.
    ///
    /// Format du protocole Dofus (Big Endian) :
    ///   [2 octets header] où :
    ///     - bits 15→2 (14 bits) = ID du message (MessageId = Header >> 2)
    ///     - bits 1→0  (2 bits)  = nombre d'octets pour encoder la taille (0, 1, 2 ou 3)
    ///   [LengthBytesCount octets] = taille des données qui suivent
    ///   [Length octets]           = données du message
    ///
    /// Gère les messages fragmentés (reçus en plusieurs paquets TCP) via m_dataMissing.
    /// </summary>
    public class MessagePart
    {
        private readonly bool m_readData;       // Si true : lit aussi les données ; sinon : compte seulement les octets
        private long m_availableBytes = 0L;     // Octets disponibles si readData = false
        private bool m_dataMissing = false;     // True si le message est fragmenté (pas encore complet)
        private byte[] m_data;                  // Données brutes du message (sans header)

        // Retourne true si le header, la taille et les données sont tous correctement remplis
        public bool IsValid
        {
            get
            {
                bool arg_69_0;
                if (this.Header.HasValue && this.Length.HasValue && (!this.ReadData || this.Data != null))
                {
                    int? length = this.Length;
                    long num = this.ReadData ? ((long)this.Data.Length) : this.m_availableBytes;
                    arg_69_0 = ((long)length.GetValueOrDefault() == num && length.HasValue);
                }
                else
                {
                    arg_69_0 = false;
                }
                return arg_69_0;
            }
        }
        // Les 2 premiers octets reçus : contient l'ID du message + le nombre d'octets de taille
        public int? Header
        {
            get;
            private set;
        }

        // ID du message : les 14 bits de poids fort du header (Header >> 2)
        public int? MessageId
        {
            get
            {
                int? result;
                if (!this.Header.HasValue)
                {
                    result = null;
                }
                else
                {
                    result = this.Header >> 2; // Décale de 2 bits pour ignorer les 2 bits de LengthBytesCount
                }
                return result;
            }
        }

        // Nombre d'octets utilisés pour encoder la taille : les 2 bits de poids faible du header (Header & 0b11)
        public int? LengthBytesCount
        {
            get
            {
                int? result;
                if (!this.Header.HasValue)
                {
                    result = null;
                }
                else
                {
                    result = (this.Header & 3); // Masque les 2 bits bas : 0=vide, 1=byte, 2=ushort, 3=3octets
                }
                return result;
            }
        }

        // Taille en octets des données du message (lu après le header)
        public int? Length
        {
            get;
            private set;
        }

        // Données brutes du message (null si readData=false ou si le message n'est pas encore complet)
        public byte[] Data
        {
            get
            {
                return this.m_data;
            }
            private set
            {
                this.m_data = value;
            }
        }

        // Indique si Build() doit aussi lire les données (true) ou seulement compter les octets (false)
        public bool ReadData
        {
            get
            {
                return this.m_readData;
            }
        }

        /// <summary>
        /// Constructeur.
        /// readData = true : lit et stocke les données du message dans Data
        /// readData = false : compte seulement les octets disponibles (utilisé pour le décodage partiel)
        /// </summary>
        public MessagePart(bool readData)
        {
            this.m_readData = readData;
        }
        /// <summary>
        /// Tente de construire le message à partir des octets disponibles dans le reader.
        /// Retourne true si le message est complet et valide, false sinon (données manquantes).
        ///
        /// Étapes :
        ///   1. Lit le header (2 octets) si pas encore fait
        ///   2. Lit la taille (LengthBytesCount octets)
        ///   3. Lit les données (Length octets)
        ///
        /// Si les octets arrivent en plusieurs morceaux (fragmentation TCP), cette méthode
        /// peut être appelée plusieurs fois jusqu'à ce que IsValid soit true.
        /// </summary>
        public bool Build(CustomDataReader reader)
        {
            bool result;
            if (reader.BytesAvailable <= 0L)
            {
                result = false; // Rien à lire
            }
            else
            {
                if (this.IsValid)
                {
                    result = true; // Message déjà complet
                }
                else
                {
                    if (!this.Header.HasValue && reader.BytesAvailable < 2L)
                    {
                        result = false; // Pas assez de données pour lire le header (2 octets minimum)
                    }
                    else
                    {
                        if (reader.BytesAvailable >= 2L && !this.Header.HasValue)
                        {
                            // Lit les 2 premiers octets = header (contient ID + nombre d'octets de taille)
                            this.Header = new int?((int)reader.ReadShort());
                        }
                        bool formatedHeader;
                        if (this.LengthBytesCount.HasValue)
                        {
                            long num = reader.BytesAvailable;
                            int? num2 = this.LengthBytesCount;
                            if (num >= (long)num2.GetValueOrDefault() && num2.HasValue)
                            {
                                formatedHeader = this.Length.HasValue;
                                goto CheckHeader;
                            }
                        }
                        formatedHeader = true;
                    CheckHeader:
                        if (!formatedHeader)
                        {
                            if (this.LengthBytesCount < 0 || this.LengthBytesCount > 3)
                            {
                                throw new Exception("Malformated Message Header, invalid bytes number to read message length (inferior to 0 or superior to 3)");
                            }
                            this.Length = new int?(0);
                            for (int i = this.LengthBytesCount.Value - 1; i >= 0; i--)
                            {
                                this.Length |= (int)reader.ReadByte() << i * 8;
                            }
                        }
                        if (this.Length.HasValue && !this.m_dataMissing)
                        {
                            if (this.Length == 0)
                            {
                                if (this.ReadData)
                                {
                                    this.Data = new byte[0];
                                }
                                result = true;
                                return result;
                            }
                            long num = reader.BytesAvailable;
                            int? num2 = this.Length;
                            if (num >= (long)num2.GetValueOrDefault() && num2.HasValue)
                            {
                                if (this.ReadData)
                                {
                                    this.Data = reader.ReadBytes(this.Length.Value);
                                }
                                else
                                {
                                    this.m_availableBytes = reader.BytesAvailable;
                                }
                                result = true;
                                return result;
                            }
                            num2 = this.Length;
                            num = reader.BytesAvailable;
                            if ((long)num2.GetValueOrDefault() > num && num2.HasValue)
                            {
                                if (this.ReadData)
                                {
                                    this.Data = reader.ReadBytes((int)reader.BytesAvailable);
                                }
                                else
                                {
                                    this.m_availableBytes = reader.BytesAvailable;
                                }
                                this.m_dataMissing = true;
                                result = false;
                                return result;
                            }
                        }
                        else
                        {
                            if (this.Length.HasValue && this.m_dataMissing)
                            {
                                long num = (long)(this.ReadData ? this.Data.Length : 0) + reader.BytesAvailable;
                                int? num2 = this.Length;
                                if (num < (long)num2.GetValueOrDefault() && num2.HasValue)
                                {
                                    if (this.ReadData)
                                    {
                                        int destinationIndex = this.m_data.Length;
                                        Array.Resize<byte>(ref this.m_data, (int)((long)this.Data.Length + reader.BytesAvailable));
                                        byte[] array = reader.ReadBytes((int)reader.BytesAvailable);
                                        Array.Copy(array, 0, this.Data, destinationIndex, array.Length);
                                    }
                                    else
                                    {
                                        this.m_availableBytes = reader.BytesAvailable;
                                    }
                                    this.m_dataMissing = true;
                                }
                                num = (long)(this.ReadData ? this.Data.Length : 0) + reader.BytesAvailable;
                                num2 = this.Length;
                                if (num >= (long)num2.GetValueOrDefault() && num2.HasValue)
                                {
                                    if (this.ReadData)
                                    {
                                        int num3 = this.Length.Value - this.Data.Length;
                                        Array.Resize<byte>(ref this.m_data, this.Data.Length + num3);
                                        byte[] array = reader.ReadBytes(num3);
                                        Array.Copy(array, 0, this.Data, this.Data.Length - num3, num3);
                                    }
                                    else
                                    {
                                        this.m_availableBytes = reader.BytesAvailable;
                                    }
                                }
                            }
                        }
                        result = this.IsValid;
                    }
                }
            }
            return result;
        }
    }
}
