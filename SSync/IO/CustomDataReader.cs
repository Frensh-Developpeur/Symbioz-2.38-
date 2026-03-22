using SSync.IO.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.IO
{
    /// <summary>
    /// Lecteur de données avec support des types variable-length du protocole Dofus.
    /// Enveloppe un IDataReader (généralement BigEndianReader) et ajoute les méthodes
    /// ReadVarInt/Short/Long qui décodent l'encodage 7 bits par octet utilisé par Dofus.
    ///
    /// Fonctionnement du VarInt (encodage LEB128) :
    ///   - Chaque octet contribue 7 bits à la valeur finale.
    ///   - Le bit de poids fort (bit 7 = 0x80) indique s'il y a un octet supplémentaire.
    ///   - Exemple : valeur 300 → [0xAC, 0x02] (300 = 0b100101100, découpé en 7+7 bits).
    /// </summary>
    public class CustomDataReader : ICustomDataInput, IDisposable
    {
        // Taille en bits d'un int (32) et d'un short (16), utilisées comme bornes de boucle
        private static int INT_SIZE = 32;
        private static int SHORT_SIZE = 16;

        // Valeurs limites pour détecter les dépassements lors de la lecture d'un VarShort
        private static int SHORT_MAX_VALUE = 32767;
        private static int UNSIGNED_SHORT_MAX_VALUE = 65536;

        // Nombre de bits utiles par octet dans l'encodage variable-length (7 bits)
        private static int CHUNCK_BIT_SIZE = 7;

        // Nombre maximum d'octets pour encoder un int sur 7 bits : ceil(32/7) = 5
        private static int MAX_ENCODING_LENGTH = (int)Math.Ceiling((double)INT_SIZE / CHUNCK_BIT_SIZE);

        // Masque pour tester/extraire le bit de continuation (bit 7 = 0x80 = 128)
        private static int MASK_10000000 = 128;

        // Masque pour extraire les 7 bits de données (bits 0-6 = 0x7F = 127)
        private static int MASK_01111111 = 127;

        // Lecteur sous-jacent (BigEndianReader ou autre implémentation de IDataReader)
        private IDataReader _data;

        /// <summary>
        /// Crée un CustomDataReader en enveloppant un IDataReader existant.
        /// </summary>
        public CustomDataReader(IDataReader reader)
        {
            _data = reader;
        }

        /// <summary>
        /// Crée un CustomDataReader directement depuis un tableau d'octets bruts.
        /// Crée automatiquement un BigEndianReader pour la lecture.
        /// </summary>
        public CustomDataReader(byte[] data)
        {
            _data = new BigEndianReader(data);
        }

        /// <summary>
        /// Lit un entier signé 32 bits encodé en variable-length (LEB128).
        /// Lit les octets un par un ; s'arrête quand le bit de continuation est à 0.
        /// Lève une exception si la valeur dépasse 32 bits.
        /// </summary>
        public int ReadVarInt()
        {
            int b = 0;
            int value = 0;
            int offset = 0;
            bool hasNext = false;
            while (offset < INT_SIZE)
            {
                b = _data.ReadByte();
                // Vérifie si le bit de poids fort indique qu'il y a encore un octet
                hasNext = (b & MASK_10000000) == MASK_10000000;
                if (offset > 0)
                {
                    // Décale les 7 bits de données vers leur position correcte dans la valeur finale
                    value = value + ((b & MASK_01111111) << offset);
                }
                else
                {
                    // Premier octet : pas de décalage nécessaire
                    value = value + (b & MASK_01111111);
                }
                offset = offset + CHUNCK_BIT_SIZE;
                if (!hasNext)
                {
                    return value;
                }
            }
            throw new Exception("Too much data");
        }

        /// <summary>
        /// Lit un entier non signé 32 bits encodé en variable-length.
        /// Identique à ReadVarInt mais sans gestion du signe.
        /// </summary>
        public uint ReadVarUhInt()
        {
            int b = 0;
            uint value = 0;
            int offset = 0;
            bool hasNext = false;
            while (offset < INT_SIZE)
            {
                b = _data.ReadByte();
                hasNext = (b & MASK_10000000) == MASK_10000000;
                if (offset > 0)
                {
                    value = (uint)(value + ((b & MASK_01111111) << offset));
                }
                else
                {
                    value = (uint)(value + (b & MASK_01111111));
                }
                offset = offset + CHUNCK_BIT_SIZE;
                if (!hasNext)
                {
                    return value;
                }
            }
            throw new Exception("Too much data");
        }

        /// <summary>
        /// Lit un short signé 16 bits encodé en variable-length.
        /// Applique un ajustement si la valeur dépasse SHORT_MAX_VALUE (gestion du signe).
        /// </summary>
        public short ReadVarShort()
        {
            int b = 0;
            short value = 0;
            int offset = 0;
            bool hasNext = false;
            while (offset < SHORT_SIZE)
            {
                b = _data.ReadByte();
                hasNext = (b & MASK_10000000) == MASK_10000000;
                if (offset > 0)
                {
                    value = (short)(value + ((b & MASK_01111111) << offset));
                }
                else
                {
                    value = (short)(value + (b & MASK_01111111));
                }
                offset = offset + CHUNCK_BIT_SIZE;
                if (!hasNext)
                {
                    // Conversion en valeur signée si nécessaire (ex. -1 encodé comme 65535)
                    if (value > SHORT_MAX_VALUE)
                    {
                        value = (short)(value - UNSIGNED_SHORT_MAX_VALUE);
                    }
                    return value;
                }
            }
            throw new Exception("Too much data");
        }

        /// <summary>
        /// Lit un short non signé 16 bits encodé en variable-length.
        /// </summary>
        public ushort ReadVarUhShort()
        {
            int b = 0;
            ushort value = 0;
            int offset = 0;
            bool hasNext = false;
            while (offset < SHORT_SIZE)
            {
                b = _data.ReadByte();
                hasNext = (b & MASK_10000000) == MASK_10000000;
                if (offset > 0)
                {
                    value = (ushort)(value + ((b & MASK_01111111) << offset));
                }
                else
                {
                    value = (ushort)(value + (b & MASK_01111111));
                }
                offset = offset + CHUNCK_BIT_SIZE;
                if (!hasNext)
                {
                    if (value > SHORT_MAX_VALUE)
                    {
                        value = (ushort)(value - UNSIGNED_SHORT_MAX_VALUE);
                    }
                    return value;
                }
            }
            throw new Exception("Too much data");
        }

        /// <summary>
        /// Lit un long signé 64 bits encodé en variable-length.
        /// Utilise la structure CustomInt64 (deux uint : low et high) pour gérer les 64 bits.
        /// </summary>
        public long ReadVarLong()
        {
            return readInt64(_data).toNumber();
        }

        /// <summary>
        /// Lit un long non signé 64 bits encodé en variable-length.
        /// </summary>
        public ulong ReadVarUhLong()
        {
            return readUInt64(_data).toNumber();
        }

        // Position courante dans le flux de lecture
        public int Position
        {
            get { return _data.Position; }
        }

        // Nombre d'octets encore disponibles à lire
        public int BytesAvailable
        {
            get { return _data.BytesAvailable; }
        }

        // --- Méthodes de lecture standard déléguées au lecteur sous-jacent ---

        public short ReadShort()
        {
            return _data.ReadShort();
        }

        public int ReadInt()
        {
            return _data.ReadInt();
        }

        public long ReadLong()
        {
            return _data.ReadLong();
        }

        public ushort ReadUShort()
        {
            return _data.ReadUShort();
        }

        public uint ReadUInt()
        {
            return _data.ReadUInt();
        }

        public ulong ReadULong()
        {
            return _data.ReadULong();
        }

        public byte ReadByte()
        {
            return _data.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return _data.ReadSByte();
        }

        public byte[] ReadBytes(int n)
        {
            return _data.ReadBytes(n);
        }

        public bool ReadBoolean()
        {
            return _data.ReadBoolean();
        }

        public char ReadChar()
        {
            return _data.ReadChar();
        }

        public double ReadDouble()
        {
            return _data.ReadDouble();
        }

        public float ReadFloat()
        {
            return _data.ReadFloat();
        }

        public string ReadUTF()
        {
            return _data.ReadUTF();
        }

        public string ReadUTFBytes(ushort len)
        {
            return _data.ReadUTFBytes(len);
        }

        public void Seek(int offset, System.IO.SeekOrigin seekOrigin)
        {
            _data.Seek(offset, seekOrigin);
        }

        public void SkipBytes(int n)
        {
            _data.SkipBytes(n);
        }

        public void Dispose()
        {
            _data.Dispose();
        }

        /// <summary>
        /// Lit un long signé 64 bits encodé en variable-length depuis un IDataReader.
        /// Gère la coupure à 28 bits (frontière entre les parties low et high du CustomInt64).
        /// </summary>
        private static CustomInt64 readInt64(IDataReader input)
        {
            uint b = 0;
            CustomInt64 result = new CustomInt64();
            int i = 0;
            while (true)
            {
                b = input.ReadByte();
                // À 28 bits on atteint la frontière entre low (28 bits) et high (4+ bits)
                if (i == 28)
                {
                    break;
                }
                if (b >= 128)
                {
                    // Bit de continuation présent : accumule les 7 bits utiles dans la partie low
                    result.low = result.low | (b & 127) << i;
                    i = i + 7;
                    continue;
                }
                // Dernier octet de la partie low
                result.low = result.low | b << i;
                return result;
            }

            if (b >= 128)
            {
                b = b & 127;
                // Les 4 bits de poids faible de b complètent la partie low
                result.low = result.low | b << i;
                // Les bits restants de b initialisent la partie high (décalage de 4)
                result.high = b >> 4;
                i = 3;
                while (true)
                {
                    b = input.ReadByte();
                    if (i < 32)
                    {
                        if (b >= 128)
                        {
                            result.high = (uint)(result.high | (b & 127) << i);
                        }
                        else
                        {
                            break;
                        }
                    }
                    i = i + 7;
                }

                result.high = (uint)(result.high | (b << i));
                return result;
            }
            result.low = result.low | b << i;
            result.high = b >> 4;
            return result;
        }

        /// <summary>
        /// Lit un long non signé 64 bits encodé en variable-length.
        /// Même logique que readInt64 mais avec des champs uint non signés.
        /// </summary>
        private CustomUInt64 readUInt64(IDataReader input)
        {
            uint b = 0;
            var result = new CustomUInt64();
            int i = 0;
            while (true)
            {
                b = input.ReadByte();
                if (i == 28)
                {
                    break;
                }
                if (b >= 128)
                {
                    result.low = result.low | (b & 127) << i;
                    i = i + 7;
                    continue;
                }
                result.low = result.low | b << i;
                return result;
            }

            if (b >= 128)
            {
                b = b & 127;
                result.low = result.low | b << i;
                result.high = b >> 4;
                i = 3;
                while (true)
                {
                    b = input.ReadByte();
                    if (i < 32)
                    {
                        if (b >= 128)
                        {
                            result.high = result.high | (b & 127) << i;
                        }
                        else
                        {
                            break;
                        }
                    }
                    i = i + 7;
                }

                result.high = result.high | b << i;
                return result;
            }
            result.low = result.low | b << i;
            result.high = b >> 4;
            return result;
        }
    }
}
