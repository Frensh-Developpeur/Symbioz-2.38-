using SSync.IO;
using System;
using System.IO;
using System.Text;

namespace Symbioz.Tools.IO
{
    /// <summary>
    /// Lecteur binaire haute performance en ordre Big Endian (octet de poids fort en premier).
    /// Utilise du code unsafe (pointeurs C) pour éviter les vérifications de bornes à chaque lecture,
    /// ce qui le rend beaucoup plus rapide que les lecteurs standards de .NET.
    /// "Big Endian" signifie que les entiers multi-octets sont stockés du plus grand au plus petit octet,
    /// contrairement au "Little Endian" utilisé par défaut sur les processeurs x86.
    /// </summary>
    public unsafe class FastBigEndianReader : IDataReader, IDisposable
    {
        // Position courante du curseur de lecture dans le buffer
        private long m_position;
        // Le tableau d'octets contenant les données brutes à lire
        private readonly byte[] m_buffer;
        // Position maximale autorisée (-1 = pas de limite fixée)
        private long m_maxPosition = -1L;

        /// <summary>
        /// Accès direct au buffer d'octets sous-jacent.
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return this.m_buffer;
            }
        }

        /// <summary>
        /// Position actuelle du curseur de lecture (en octets depuis le début).
        /// Lève une exception si on tente de dépasser la position maximale autorisée.
        /// </summary>
        public int Position
        {
            get
            {
                return (int)this.m_position;
            }
            set
            {
                // Vérification : on ne peut pas lire au-delà de la limite fixée
                if (this.m_maxPosition > 0L && value > this.m_maxPosition)
                {
                    throw new InvalidOperationException("Buffer overflow");
                }
                this.m_position = value;
            }
        }

        /// <summary>
        /// Limite maximale de lecture. Permet de restreindre la lecture à une zone du buffer.
        /// </summary>
        public long MaxPosition
        {
            get
            {
                return this.m_maxPosition;
            }
            set
            {
                this.m_maxPosition = value;
            }
        }

        /// <summary>
        /// Nombre d'octets encore disponibles à partir de la position courante.
        /// Tient compte de MaxPosition si elle est définie.
        /// </summary>
        public int BytesAvailable
        {
            get
            {
                // Si MaxPosition est définie, on l'utilise comme borne ; sinon on utilise la taille du buffer
                return (int)(((this.m_maxPosition > 0L) ? this.m_maxPosition : ((long)this.m_buffer.Length)) - this.Position);
            }
        }

        /// <summary>
        /// Initialise le lecteur avec un tableau d'octets existant.
        /// </summary>
        /// <param name="buffer">Les données binaires à lire.</param>
        public FastBigEndianReader(byte[] buffer)
        {
            this.m_buffer = buffer;
        }

        /// <summary>
        /// Lit un octet non signé (1 octet, valeur 0-255) et avance le curseur d'1 position.
        /// Utilise un pointeur direct pour éviter la vérification de bornes.
        /// </summary>
        public byte ReadByte()
        {
            fixed (byte* pbyte = &m_buffer[m_position++])
            {
                return *pbyte;
            }
        }

        /// <summary>
        /// Lit un octet signé (1 octet, valeur -128 à 127) et avance le curseur d'1 position.
        /// </summary>
        public sbyte ReadSByte()
        {
            fixed (byte* pbyte = &m_buffer[m_position++])
            {
                return (sbyte)*pbyte;
            }
        }

        /// <summary>
        /// Lit un entier court signé (2 octets) en ordre Big Endian et avance le curseur de 2.
        /// En Big Endian : premier octet = bits 8-15, deuxième octet = bits 0-7.
        /// </summary>
        public short ReadShort()
        {
            var position = m_position;
            m_position += 2;
            fixed (byte* pbyte = &m_buffer[position])
            {
                // Reconstitution de la valeur 16 bits : octet[0] décalé de 8 bits | octet[1]
                return (short)((*pbyte << 8) | (*(pbyte + 1)));
            }
        }

        /// <summary>
        /// Lit un entier signé de 32 bits (4 octets) en ordre Big Endian et avance le curseur de 4.
        /// </summary>
        public int ReadInt()
        {
            var position = m_position;
            m_position += 4;
            fixed (byte* pbyte = &m_buffer[position])
            {
                // Reconstitution : octet[0] = bits 24-31, octet[1] = bits 16-23, etc.
                return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
            }
        }

        /// <summary>
        /// Lit un entier signé de 64 bits (8 octets) en ordre Big Endian et avance le curseur de 8.
        /// </summary>
        public long ReadLong()
        {
            var position = m_position;
            m_position += 8;
            fixed (byte* pbyte = &m_buffer[position])
            {
                // On lit les 4 premiers octets (partie haute) et les 4 suivants (partie basse)
                int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                // Assemblage final : partie basse (uint) | partie haute décalée de 32 bits
                return (uint)i2 | ((long)i1 << 32);
            }
        }

        /// <summary>
        /// Lit un entier court non signé (2 octets) en Big Endian.
        /// </summary>
        public ushort ReadUShort()
        {
            return (ushort)ReadShort();
        }

        /// <summary>
        /// Lit un entier non signé de 32 bits (4 octets) en Big Endian.
        /// </summary>
        public uint ReadUInt()
        {
            return (uint)ReadInt();
        }

        /// <summary>
        /// Lit un entier non signé de 64 bits (8 octets) en Big Endian.
        /// </summary>
        public ulong ReadULong()
        {
            return (ulong)ReadLong();
        }

        /// <summary>
        /// Lit exactement n octets consécutifs et les retourne dans un nouveau tableau.
        /// Utilise une copie par blocs de 4 octets pour de meilleures performances.
        /// </summary>
        /// <param name="n">Nombre d'octets à lire.</param>
        public byte[] ReadBytes(int n)
        {
            if (this.BytesAvailable < (long)n)
            {
                throw new InvalidOperationException("Buffer overflow");
            }
            var dst = new byte[n];
            fixed (byte* pSrc = &m_buffer[m_position], pDst = dst)
            {
                byte* ps = pSrc;
                byte* pd = pDst;

                // Copie rapide par blocs de 4 octets (int = 4 bytes) pour maximiser la performance
                for (int i = 0; i < n / 4; i++)
                {
                    *((int*)pd) = *((int*)ps);
                    pd += 4;
                    ps += 4;
                }

                // Copie des octets restants (si n n'est pas un multiple de 4)
                for (int i = 0; i < n % 4; i++)
                {
                    *pd = *ps;
                    pd++;
                    ps++;
                }
            }

            m_position += n;

            return dst;
        }

        /// <summary>
        /// Lit un booléen (1 octet : 0 = false, tout autre valeur = true).
        /// </summary>
        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        /// <summary>
        /// Lit un caractère Unicode encodé sur 2 octets (Big Endian).
        /// </summary>
        public char ReadChar()
        {
            return (char)ReadShort();
        }

        /// <summary>
        /// Lit un nombre flottant 32 bits (float) en interprétant les 4 octets lus comme un float.
        /// Utilise un reinterpret cast via pointeur pour éviter toute conversion.
        /// </summary>
        public float ReadFloat()
        {
            int val = ReadInt();
            // Reinterprétation directe des bits : on lit un int mais on le traite comme un float
            return *(float*)&val;
        }

        /// <summary>
        /// Lit un nombre flottant 64 bits (double) en interprétant les 8 octets lus comme un double.
        /// </summary>
        public double ReadDouble()
        {
            long val = ReadLong();
            // Reinterprétation directe des bits : on lit un long mais on le traite comme un double
            return *(double*)&val;
        }

        /// <summary>
        /// Lit une chaîne de caractères UTF-8 précédée de sa longueur sur 2 octets (format ActionScript/Flash).
        /// Format : [ushort longueur] [octets UTF-8]
        /// </summary>
        public string ReadUTF()
        {
            // La longueur de la chaîne est encodée sur 2 octets avant les données texte
            ushort length = ReadUShort();

            byte[] bytes = ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Lit une chaîne UTF-8 de longueur explicitement fournie (sans lire de préfixe de longueur).
        /// </summary>
        /// <param name="len">Nombre d'octets à lire pour la chaîne.</param>
        public string ReadUTFBytes(ushort len)
        {
            byte[] bytes = ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Déplace le curseur de lecture selon une origine (début, fin ou position courante).
        /// </summary>
        /// <param name="offset">Déplacement en octets.</param>
        /// <param name="seekOrigin">Origine du déplacement (Begin, End, Current).</param>
        public void Seek(int offset, SeekOrigin seekOrigin)
        {
            if (seekOrigin == SeekOrigin.Begin)
            {
                // Déplacement depuis le début du buffer
                this.Position = (int)offset;
            }
            else
            {
                if (seekOrigin == SeekOrigin.End)
                {
                    // Déplacement depuis la fin du buffer (offset négatif pour reculer)
                    this.Position = (int)(this.m_buffer.Length + offset);
                }
                else
                {
                    if (seekOrigin == SeekOrigin.Current)
                    {
                        // Déplacement relatif depuis la position courante
                        this.Position += (int)offset;
                    }
                }
            }
        }

        /// <summary>
        /// Surcharge de Seek acceptant un offset long (pour les fichiers très volumineux).
        /// </summary>
        public void Seek(long offset, SeekOrigin seekOrigin)
        {
            if (seekOrigin == SeekOrigin.Begin)
            {
                this.Position = (int)offset;
            }
            else
            {
                if (seekOrigin == SeekOrigin.End)
                {
                    this.Position = (int)(this.m_buffer.Length + offset);
                }
                else
                {
                    if (seekOrigin == SeekOrigin.Current)
                    {
                        this.Position += (int)offset;
                    }
                }
            }
        }

        /// <summary>
        /// Saute n octets en avançant simplement le curseur (sans les lire).
        /// </summary>
        /// <param name="n">Nombre d'octets à ignorer.</param>
        public void SkipBytes(int n)
        {
            this.Position += n;
        }

        /// <summary>
        /// Libère les ressources. Ce lecteur travaillant sur un tableau en mémoire,
        /// il n'y a rien à libérer explicitement.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Alias de ReadFloat() pour compatibilité avec l'interface IDataReader.
        /// </summary>
        public float ReadSingle()
        {
            return ReadFloat();
        }
    }
}
