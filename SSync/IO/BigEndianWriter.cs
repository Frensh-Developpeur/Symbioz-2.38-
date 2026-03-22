using System;
using System.Linq;
using System.IO;
using System.Text;

namespace SSync.IO
{
    /// <summary>
    /// Écrivain de données en "Big Endian" (octet de poids fort en premier).
    /// Symétrique de BigEndianReader : écrit les nombres en inversant l'ordre des octets
    /// pour respecter le protocole réseau de Dofus.
    /// La propriété Data retourne le contenu complet du buffer d'écriture sous forme de tableau d'octets.
    /// </summary>
    public class BigEndianWriter : IDataWriter, IDisposable
    {
        private BinaryWriter m_writer;
        public Stream BaseStream
        {
            get
            {
                return this.m_writer.BaseStream;
            }
        }
        // Espace encore disponible dans le flux (longueur - position)
        public long BytesAvailable
        {
            get
            {
                return this.m_writer.BaseStream.Length - this.m_writer.BaseStream.Position;
            }
        }
        // Position actuelle dans le flux d'écriture (peut être modifiée pour réécrire des données)
        public int Position
        {
            get
            {
                return (int)m_writer.BaseStream.Position;
            }
            set
            {
                this.m_writer.BaseStream.Position = value;
            }
        }
        /// <summary>
        /// Retourne tout le contenu écrit jusqu'ici sous forme de tableau d'octets.
        /// Sauvegarde et restaure la position actuelle pour ne pas perturber l'écriture en cours.
        /// </summary>
        public byte[] Data
        {
            get
            {
                long position = this.m_writer.BaseStream.Position;
                byte[] array = new byte[this.m_writer.BaseStream.Length];
                this.m_writer.BaseStream.Position = 0L;
                this.m_writer.BaseStream.Read(array, 0, (int)this.m_writer.BaseStream.Length);
                this.m_writer.BaseStream.Position = position;
                return array;
            }
        }

        public BigEndianWriter()
        {
            this.m_writer = new BinaryWriter(new MemoryStream(), Encoding.UTF8);
        }

        public BigEndianWriter(Stream stream)
        {
            this.m_writer = new BinaryWriter(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Écrit un tableau d'octets dans l'ordre inversé (Little Endian → Big Endian).
        /// BitConverter.GetBytes() retourne en Little Endian, donc on inverse pour le réseau.
        /// </summary>
        private void WriteBigEndianBytes(byte[] endianBytes)
        {
            for (int i = endianBytes.Length - 1; i >= 0; i--)
            {
                this.m_writer.Write(endianBytes[i]);
            }
        }

        public void WriteShort(short @short)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@short));
        }

        public void WriteInt(int @int)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@int));
        }

        public void WriteLong(long @long)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@long));
        }

        public void WriteUShort(ushort @ushort)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@ushort));
        }

        public void WriteUInt(uint @uint)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@uint));
        }

        public void WriteULong(ulong @ulong)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@ulong));
        }

        public void WriteByte(byte @byte)
        {
            this.m_writer.Write(@byte);
        }

        public void WriteSByte(sbyte @byte)
        {
            this.m_writer.Write(@byte);
        }

        public void WriteFloat(float @float)
        {
            this.m_writer.Write(@float);
        }

        public void WriteBoolean(bool @bool)
        {
            if (@bool)
            {
                this.m_writer.Write((byte)1);
            }
            else
            {
                this.m_writer.Write((byte)0);
            }
        }

        public void WriteChar(char @char)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@char));
        }

        public void WriteDouble(double @double)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(@double));
        }

        public void WriteSingle(float single)
        {
            this.WriteBigEndianBytes(BitConverter.GetBytes(single));
        }

        public void WriteUTF(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            ushort num = (ushort)bytes.Length;
            this.WriteUShort(num);
            for (int i = 0; i < (int)num; i++)
            {
                this.m_writer.Write(bytes[i]);
            }
        }

        public void WriteUTFBytes(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            int num = bytes.Length;
            for (int i = 0; i < num; i++)
            {
                this.m_writer.Write(bytes[i]);
            }
        }

        public void WriteBytes(byte[] data)
        {
            this.m_writer.Write(data);
        }

        public void Seek(int offset)
        {
            this.Seek(offset, SeekOrigin.Begin);
        }

        public void Seek(int offset, SeekOrigin seekOrigin)
        {
            this.m_writer.BaseStream.Seek((long)offset, seekOrigin);
        }

        public void Clear()
        {
            this.m_writer = new BinaryWriter(new MemoryStream(), Encoding.UTF8);
        }

        public void Dispose()
        {
            this.m_writer.Flush();
            this.m_writer.Dispose();
            this.m_writer = null;
        }
    }
}