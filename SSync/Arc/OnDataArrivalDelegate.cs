using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Arc
{
    /// <summary>
    /// Délégué appelé chaque fois que des données arrivent sur le socket TCP.
    /// Le paramètre <paramref name="datas"/> contient les octets bruts reçus.
    /// C'est ce délégué qui déclenche le parsing du protocole Dofus.
    /// </summary>
    public delegate void OnDataArrivalDelegate(byte[] datas);
}
