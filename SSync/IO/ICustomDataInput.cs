using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.IO
{
    /// <summary>
    /// Interface de lecture étendue spécifique au protocole Dofus.
    /// Hérite de IDataReader (lecture standard) et ajoute la lecture
    /// des types encodés en variable-length (VarInt, VarShort, VarLong).
    ///
    /// Les types "Var" utilisent un encodage sur 7 bits par octet :
    /// si le bit de poids fort (bit 7) est à 1, il y a un autre octet à lire.
    /// Cela permet d'économiser de la bande passante pour les petites valeurs.
    /// </summary>
    public interface ICustomDataInput : IDataReader
    {
        /// <summary>Lit un entier signé encodé en variable-length (1 à 5 octets).</summary>
        int ReadVarInt();

        /// <summary>Lit un entier non signé encodé en variable-length (1 à 5 octets).</summary>
        uint ReadVarUhInt();

        /// <summary>Lit un short signé encodé en variable-length (1 à 3 octets).</summary>
        short ReadVarShort();

        /// <summary>Lit un short non signé encodé en variable-length (1 à 3 octets).</summary>
        ushort ReadVarUhShort();

        /// <summary>Lit un long signé encodé en variable-length (1 à 10 octets).</summary>
        long ReadVarLong();

        /// <summary>Lit un long non signé encodé en variable-length (1 à 10 octets).</summary>
        ulong ReadVarUhLong();
    }
}
