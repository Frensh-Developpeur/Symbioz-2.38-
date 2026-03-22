using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.IO
{
    /// <summary>
    /// Interface d'écriture étendue spécifique au protocole Dofus.
    /// Hérite de IDataWriter (écriture standard) et ajoute l'écriture
    /// des types encodés en variable-length (VarInt, VarShort, VarLong).
    ///
    /// Symétrique de ICustomDataInput : chaque ReadVarXxx a son WriteVarXxx correspondant.
    /// Ces méthodes sont utilisées lors de la sérialisation des messages Dofus.
    /// </summary>
    public interface ICustomDataOutput : IDataWriter
    {
        /// <summary>Écrit un entier signé en variable-length (1 à 5 octets selon la valeur).</summary>
        void WriteVarInt(int value);

        /// <summary>Écrit un entier non signé en variable-length (1 à 5 octets).</summary>
        void WriteVarUhInt(uint value);

        /// <summary>Écrit un short signé en variable-length (1 à 3 octets).</summary>
        void WriteVarShort(short value);

        /// <summary>Écrit un short non signé en variable-length (1 à 3 octets).</summary>
        void WriteVarUhShort(ushort value);

        /// <summary>Écrit un long signé en variable-length (1 à 10 octets).</summary>
        void WriteVarLong(long value);

        /// <summary>Écrit un long non signé en variable-length (1 à 10 octets).</summary>
        void WriteVarUhLong(ulong value);
    }
}
