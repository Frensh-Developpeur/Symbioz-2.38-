using System;
using System.IO;
using System.Text;

namespace SSync.IO
{
    /// <summary>
    /// Lecteur de données en "Big Endian" (octet de poids fort en premier).
    /// Le protocole réseau de Dofus utilise le format Big Endian pour tous les types numériques.
    /// Cette classe lit des nombres (short, int, long, etc.) depuis un flux binaire en inversant
    /// l'ordre des octets par rapport au format natif Windows (Little Endian).
    /// Implémente IDisposable pour libérer proprement le flux sous-jacent.
    /// </summary>
    public class BigEndianReader : IDataReader, IDisposable
    {
        private BinaryReader m_reader;

        // Nombre d'octets disponibles à lire (longueur totale - position actuelle)
        public int BytesAvailable
        {
            get
            {
                return (int)(this.m_reader.BaseStream.Length - this.m_reader.BaseStream.Position);
            }
        }
        // Position actuelle dans le flux de lecture
        public int Position
        {
            get
            {
                return (int)this.m_reader.BaseStream.Position;
            }
        }
        public Stream BaseStream
        {
            get
            {
                return this.m_reader.BaseStream;
            }
        }

        public BigEndianReader()
        {
            this.m_reader = new BinaryReader(new MemoryStream(), Encoding.UTF8);
        }

        public BigEndianReader(Stream stream)
        {
            this.m_reader = new BinaryReader(stream, Encoding.UTF8);
        }

        public BigEndianReader(byte[] tab)
        {
            this.m_reader = new BinaryReader(new MemoryStream(tab), Encoding.UTF8);
        }

        /// <summary>
        /// Lit N octets dans l'ordre inversé (Big Endian → Little Endian).
        /// Le premier octet lu devient le dernier dans le tableau retourné,
        /// ce qui permet à BitConverter (Little Endian) de les interpréter correctement.
        /// </summary>
        private byte[] ReadBigEndianBytes(int count)
        {
            byte[] array = new byte[count];
            // Lit les octets dans l'ordre inverse pour convertir Big Endian → Little Endian
            for (int i = count - 1; i >= 0; i--)
            {
                array[i] = (byte)this.BaseStream.ReadByte();
            }
            return array;
        }

        public short ReadShort()
        {
            return BitConverter.ToInt16(this.ReadBigEndianBytes(2), 0);
        }

        public int ReadInt()
        {
            return BitConverter.ToInt32(this.ReadBigEndianBytes(4), 0);
        }

        public long ReadLong()
        {
            return BitConverter.ToInt64(this.ReadBigEndianBytes(8), 0);
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(this.ReadBigEndianBytes(4), 0);
        }

        public ushort ReadUShort()
        {
            return BitConverter.ToUInt16(this.ReadBigEndianBytes(2), 0);
        }

        public uint ReadUInt()
        {
            return BitConverter.ToUInt32(this.ReadBigEndianBytes(4), 0);
        }

        public ulong ReadULong()
        {
            return BitConverter.ToUInt64(this.ReadBigEndianBytes(8), 0);
        }

        public byte ReadByte()
        {
            return this.m_reader.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return this.m_reader.ReadSByte();
        }

        public byte[] ReadBytes(int n)
        {
            return this.m_reader.ReadBytes(n);
        }

        public BigEndianReader ReadBytesInNewBigEndianReader(int n)
        {
            return new BigEndianReader(this.m_reader.ReadBytes(n));
        }

        public bool ReadBoolean()
        {
            return this.m_reader.ReadByte() == 1;
        }

        public char ReadChar()
        {
            return (char)this.ReadUShort();
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(this.ReadBigEndianBytes(8), 0);
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(this.ReadBigEndianBytes(4), 0);
        }

        public string ReadUTF()
        {
            ushort n = this.ReadUShort();
            byte[] bytes = this.ReadBytes((int)n);
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadUTF7BitLength()
        {
            int n = this.ReadInt();
            byte[] bytes = this.ReadBytes(n);
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadUTFBytes(ushort len)
        {
            byte[] bytes = this.ReadBytes((int)len);
            return Encoding.UTF8.GetString(bytes);
        }

        public void SkipBytes(int n)
        {
            for (int i = 0; i < n; i++)
            {
                this.m_reader.ReadByte();
            }
        }

        public void Seek(int offset, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            this.m_reader.BaseStream.Seek((long)offset, seekOrigin);
        }

        public void Add(byte[] data, int offset, int count)
        {
            long position = this.m_reader.BaseStream.Position;
            this.m_reader.BaseStream.Position = this.m_reader.BaseStream.Length;
            this.m_reader.BaseStream.Write(data, offset, count);
            this.m_reader.BaseStream.Position = position;
        }

        public void Dispose()
        {
            this.m_reader.Dispose();
            this.m_reader = null;
        }
    }
}