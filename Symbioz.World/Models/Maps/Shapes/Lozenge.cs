using Symbioz.Protocol.Enums;
using Symbioz.World.Records;
using Symbioz.World.Records.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Maps.Shapes
{
    /// <summary>
    /// Zone en losange (forme diamant) centrée sur la cellule cible.
    ///
    /// Le losange couvre toutes les cellules à une distance de Manhattan ≤ Radius du centre.
    /// Si MinRadius > 0, les cellules à moins de MinRadius cases sont exclues (anneau creux).
    ///
    /// Surface = (Radius+1)² + Radius² (nombre total de cellules dans un losange de rayon R).
    ///
    /// Utilisé pour les sorts à zone carrée/losange (ex. Lait de Dragodinde, zones larges)
    /// et pour les déplacements aléatoires des groupes de monstres (RandomMapMove).
    /// </summary>
    public class Lozenge : IShape
    {
        // Nombre de cellules dans un losange de rayon R = (R+1)² + R²
        public uint Surface
        {
            get
            {
                return (uint)((this.Radius + 1) * (this.Radius + 1) + this.Radius * this.Radius);
            }
        }
        public byte MinRadius
        {
            get;
            set;
        }
        public DirectionsEnum Direction
        {
            get;
            set;
        }
        public byte Radius
        {
            get;
            set;
        }
        public Lozenge(byte minRadius, byte radius)
        {
            this.MinRadius = minRadius;
            this.Radius = radius;
        }

        // Génère les cellules du losange en parcourant toutes les colonnes (de X-R à X+R).
        // Pour chaque colonne, la hauteur de la tranche augmente puis diminue symétriquement.
        public short[] GetCells(short centerCell, MapRecord map)
        {
            MapPoint mapPoint = new MapPoint(centerCell);
            List<short> list = new List<short>();

            short[] result;

            if (this.Radius == 0)
            {
                // Cas dégénéré : losange de rayon 0 = cellule centrale seulement
                if (this.MinRadius == 0)
                {
                    list.Add(centerCell);
                }
                result = list.ToArray();
            }
            else
            {
                int i = mapPoint.X - (int)this.Radius;
                int num = 0;
                int num2 = 1;
                while (i <= mapPoint.X + (int)this.Radius)
                {
                    for (int j = -num; j <= num; j++)
                    {
                        // Filtre les cellules trop proches du centre si MinRadius > 0
                        if (this.MinRadius == 0 || System.Math.Abs(mapPoint.X - i) + System.Math.Abs(j) >= (int)this.MinRadius)
                        {
                            MapPoint.AddCellIfValid(i, j + mapPoint.Y, map, list);
                        }
                    }
                    // Quand on dépasse le centre, la hauteur commence à décroître
                    if (num == (int)this.Radius)
                    {
                        num2 = -num2;
                    }
                    num += num2;
                    i++;
                }
                result = list.ToArray();
            }
            return result;
        }
    }
}
