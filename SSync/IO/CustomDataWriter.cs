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
    /// Écrivain de données avec support des types variable-length du protocole Dofus.
    /// Enveloppe un IDataWriter (généralement BigEndianWriter) et ajoute les méthodes
    /// WriteVarInt/Short/Long qui encodent les entiers sur le minimum d'octets possible.
    ///
    /// Fonctionnement du VarInt (encodage LEB128) :
    ///   - On extrait 7 bits à la fois en partant du bit de poids faible.
    ///   - Si des bits restent, on met le bit 7 à 1 pour signaler la continuation.
    ///   - Exemple : valeur 300 → [0xAC, 0x02].
    /// </summary>
    public class CustomDataWriter : ICustomDataOutput, IDisposable
    {
        // Taille en bits d'un int (32)
        private static int INT_SIZE = 32;

        // Valeurs limites pour valider les VarShort
        private static int SHORT_MIN_VALUE = -32768;
        private static int SHORT_MAX_VALUE = 32767;
        private static int UNSIGNED_SHORT_MAX_VALUE = 65536;

        // Nombre de bits utiles par octet dans l'encodage variable-length
        private static int CHUNCK_BIT_SIZE = 7;

        // Nombre maximum d'octets pour encoder un int : ceil(32/7) = 5
        private static int MAX_ENCODING_LENGTH = (int)Math.Ceiling((double)INT_SIZE / CHUNCK_BIT_SIZE);

        // Masque du bit de continuation (bit 7 = 0x80 = 128)
        private static int MASK_10000000 = 128;

        // Masque des 7 bits de données (bits 0-6 = 0x7F = 127)
        private static int MASK_01111111 = 127;

        // Écrivain sous-jacent (BigEndianWriter par défaut)
        private IDataWriter _data;

        /// <summary>
        /// Crée un CustomDataWriter avec un nouveau BigEndianWriter interne.
        /// </summary>
        public CustomDataWriter()
        {
            _data = new BigEndianWriter();
        }

        /// <summary>
        /// Crée un CustomDataWriter qui écrit dans un flux existant.
        /// </summary>
        public CustomDataWriter(Stream stream)
        {
            _data = new BigEndianWriter(stream);
        }

        /// <summary>
        /// Écrit un entier signé 32 bits en variable-length (LEB128).
        /// Les petites valeurs (0-127) tiennent en 1 seul octet.
        /// </summary>
        public void WriteVarInt(int value)
        {
            // Cas optimisé : valeur entre 0 et 127 → un seul octet sans bit de continuation
            if (value >= 0 && value <= MASK_01111111)
            {
                _data.WriteByte((byte)value);
                return;
            }
            int b = 0;
            int c = value;
            // Boucle jusqu'à ce que tous les bits soient écrits
            while (c != 0 && c != -1)
            {
                // Extrait les 7 bits de poids faible
                b = c & MASK_01111111;
                // Décale vers la droite pour traiter les bits suivants
                c = c >> CHUNCK_BIT_SIZE;
                if (c > 0)
                {
                    // Il reste des bits : active le bit de continuation
                    b = b | MASK_10000000;
                }
                _data.WriteByte((byte)b);
            }
        }

        /// <summary>
        /// Écrit un entier non signé 32 bits en variable-length.
        /// </summary>
        public void WriteVarUhInt(uint value)
        {
            if (value <= MASK_01111111)
            {
                _data.WriteByte((byte)value);
                return;
            }
            uint b = 0;
            uint c = value;
            while (c != 0)
            {
                b = (uint)(c & MASK_01111111);
                c = c >> CHUNCK_BIT_SIZE;
                if (c > 0)
                {
                    b = b | (uint)MASK_10000000;
                }
                _data.WriteByte((byte)b);
            }
        }

        /// <summary>
        /// Écrit un short signé 16 bits en variable-length.
        /// Lève une exception si la valeur est hors des limites d'un short.
        /// </summary>
        public void WriteVarShort(short value)
        {
            if (value > SHORT_MAX_VALUE || value < SHORT_MIN_VALUE)
            {
                throw new Exception("Forbidden value");
            }
            else
            {
                var b = 0;
                if ((value >= 0) && (value <= MASK_01111111))
                {
                    _data.WriteByte((byte)value);
                    return;
                }
                // Masque à 16 bits pour gérer correctement les valeurs négatives
                var c = value & 65535;
                while (c != 0 && c != -1)
                {
                    b = (c & MASK_01111111);
                    c = c >> CHUNCK_BIT_SIZE;
                    if (c > 0)
                    {
                        b = b | MASK_10000000;
                    }
                    _data.WriteByte((byte)b);
                }
            }
        }

        /// <summary>
        /// Écrit un short non signé 16 bits en variable-length.
        /// </summary>
        public void WriteVarUhShort(ushort value)
        {
            if (value > UNSIGNED_SHORT_MAX_VALUE || value < SHORT_MIN_VALUE)
            {
                throw new Exception("Forbidden value");
            }
            else
            {
                var b = 0;
                if ((value >= 0) && (value <= MASK_01111111))
                {
                    _data.WriteByte((byte)value);
                    return;
                }
                var c = value & 65535;
                while (c != 0)
                {
                    b = (c & MASK_01111111);
                    c = c >> CHUNCK_BIT_SIZE;
                    if (c > 0)
                    {
                        b = b | MASK_10000000;
                    }
                    _data.WriteByte((byte)b);
                }
            }
        }

        /// <summary>
        /// Écrit un long signé 64 bits en variable-length.
        /// Utilise CustomInt64 (deux uint) pour découper les 64 bits en parties low et high.
        /// </summary>
        public void WriteVarLong(long value)
        {
            uint i = 0;
            // Découpe la valeur long en deux uint (low = bits 0-31, high = bits 32-63)
            var val = CustomInt64.fromNumber(value);
            if (val.high == 0)
            {
                // Partie haute nulle : on encode uniquement la partie basse
                writeint32(_data, val.low);
            }
            else
            {
                i = 0;
                // Encode les 4 premiers groupes de 7 bits de la partie basse avec bit de continuation
                while (i < 4)
                {
                    this._data.WriteByte((byte)(val.low & 127 | 128));
                    val.low = val.low >> 7;
                    i++;
                }
                // Octet de jonction entre low (4 bits restants) et high (bits de poids faible)
                if ((val.high & 268435455 << 3) == 0)
                {
                    this._data.WriteByte((byte)(val.high << 4 | val.low));
                }
                else
                {
                    this._data.WriteByte((byte)(((val.high << 4) | val.low) & 127 | 128));
                    writeint32(this._data, val.high >> 3);
                }
            }
        }

        /// <summary>
        /// Écrit un long non signé 64 bits en variable-length (délègue à WriteVarLong).
        /// </summary>
        public void WriteVarUhLong(ulong value)
        {
            WriteVarLong((long)value);
        }

        // Retourne le contenu complet du buffer d'écriture sous forme d'octets
        public byte[] Data
        {
            get { return _data.Data; }
        }

        // Position courante dans le flux d'écriture
        public int Position
        {
            get { return _data.Position; }
        }

        // --- Méthodes d'écriture standard déléguées au writer sous-jacent ---

        public void WriteShort(short @short)
        {
            _data.WriteShort(@short);
        }

        public void WriteInt(int @int)
        {
            _data.WriteInt(@int);
        }

        public void WriteLong(long @long)
        {
            _data.WriteLong(@long);
        }

        public void WriteUShort(ushort @ushort)
        {
            _data.WriteUShort(@ushort);
        }

        public void WriteUInt(uint @uint)
        {
            _data.WriteUInt(@uint);
        }

        public void WriteULong(ulong @ulong)
        {
            _data.WriteULong(@ulong);
        }

        public void WriteByte(byte @byte)
        {
            _data.WriteByte(@byte);
        }

        public void WriteSByte(sbyte @byte)
        {
            _data.WriteSByte(@byte);
        }

        public void WriteFloat(float @float)
        {
            _data.WriteFloat(@float);
        }

        public void WriteBoolean(bool @bool)
        {
            _data.WriteBoolean(@bool);
        }

        public void WriteChar(char @char)
        {
            _data.WriteChar(@char);
        }

        public void WriteDouble(double @double)
        {
            _data.WriteDouble(@double);
        }

        public void WriteSingle(float single)
        {
            _data.WriteSingle(single);
        }

        public void WriteUTF(string str)
        {
            _data.WriteUTF(str);
        }

        public void WriteUTFBytes(string str)
        {
            _data.WriteUTFBytes(str);
        }

        public void WriteBytes(byte[] data)
        {
            _data.WriteBytes(data);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public void Seek(int offset)
        {
            _data.Seek(offset);
        }

        public void Dispose()
        {
            if (_data is BigEndianWriter)
            {
                (_data as BigEndianWriter).Dispose();
            }
        }

        /// <summary>
        /// Encode un uint 32 bits en variable-length et l'écrit dans le writer.
        /// Utilisé en interne pour la partie haute des VarLong.
        /// </summary>
        private static void writeint32(IDataWriter output, uint value)
        {
            // Tant que la valeur dépasse 7 bits, écrit un octet avec bit de continuation
            while (value >= 128)
            {
                output.WriteByte((byte)(value & 127 | 128));
                value = value >> 7;
            }
            // Dernier octet sans bit de continuation
            output.WriteByte((byte)value);
        }
    }
}
