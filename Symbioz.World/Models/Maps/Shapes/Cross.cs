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
    /// Zone en croix (forme +) centrée sur la cellule cible.
    /// Couvre Radius cases dans chaque direction orthogonale (4 branches).
    ///
    /// Options avancées :
    ///   - Diagonal = true  : croix en X (branches diagonales plutôt qu'orthogonales)
    ///   - AllDirections    : croix en étoile (8 branches, orthogonales + diagonales)
    ///   - OnlyPerpendicular : ne garde que les 2 branches perpendiculaires à la direction du lanceur
    ///   - DisabledDirections : exclut certaines directions de la croix
    ///   - MinRadius : anneau creux (exclut les cellules à moins de MinRadius cases du centre)
    ///
    /// Surface = Radius × 4 + 1 (les 4 branches + le centre si MinRadius == 0).
    /// </summary>
    public class Cross : IShape
    {
        // true = branches diagonales (croix X) au lieu d'orthogonales (croix +)
        public bool Diagonal
        {
            get;
            set;
        }

        // Liste des directions à exclure de la croix (branches désactivées)
        public List<DirectionsEnum> DisabledDirections
        {
            get;
            set;
        }

        // true = ne conserve que les 2 directions perpendiculaires à la direction du lanceur
        public bool OnlyPerpendicular
        {
            get;
            set;
        }

        // true = croix à 8 branches (orthogonales + diagonales simultanément)
        public bool AllDirections
        {
            get;
            set;
        }

        // Nombre de cellules couvertes (Radius × 4 branches + 1 centre si MinRadius=0)
        public uint Surface
        {
            get
            {
                return (uint)(this.Radius * 4 + 1);
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
        public Cross(byte minRadius, byte radius)
        {
            this.MinRadius = minRadius;
            this.Radius = radius;
            this.DisabledDirections = new System.Collections.Generic.List<DirectionsEnum>();
        }
        public short[] GetCells(short centerCell, MapRecord map)
        {
            List<short> list = new List<short>();
            // Inclure le centre seulement si MinRadius = 0
            if (this.MinRadius == 0)
            {
                list.Add(centerCell);
            }
            System.Collections.Generic.List<DirectionsEnum> list2 = this.DisabledDirections.ToList<DirectionsEnum>();
            // Si OnlyPerpendicular, ajouter les 2 directions parallèles à la direction du lanceur
            // pour qu'elles soient ensuite ignorées (ne garder que les perpendiculaires)
            if (this.OnlyPerpendicular)
            {
                switch (this.Direction)
                {
                    case DirectionsEnum.DIRECTION_EAST:
                    case DirectionsEnum.DIRECTION_WEST:
                        list2.Add(DirectionsEnum.DIRECTION_EAST);
                        list2.Add(DirectionsEnum.DIRECTION_WEST);
                        break;
                    case DirectionsEnum.DIRECTION_SOUTH_EAST:
                    case DirectionsEnum.DIRECTION_NORTH_WEST:
                        list2.Add(DirectionsEnum.DIRECTION_SOUTH_EAST);
                        list2.Add(DirectionsEnum.DIRECTION_NORTH_WEST);
                        break;
                    case DirectionsEnum.DIRECTION_SOUTH:
                    case DirectionsEnum.DIRECTION_NORTH:
                        list2.Add(DirectionsEnum.DIRECTION_SOUTH);
                        list2.Add(DirectionsEnum.DIRECTION_NORTH);
                        break;
                    case DirectionsEnum.DIRECTION_SOUTH_WEST:
                    case DirectionsEnum.DIRECTION_NORTH_EAST:
                        list2.Add(DirectionsEnum.DIRECTION_NORTH_EAST);
                        list2.Add(DirectionsEnum.DIRECTION_SOUTH_WEST);
                        break;
                }
            }
            MapPoint mapPoint = new MapPoint(centerCell);
            for (int i = (int)this.Radius; i > 0; i--)
            {
                if (i >= (int)this.MinRadius)
                {
                    if (!this.Diagonal)
                    {
                        if (!list2.Contains(DirectionsEnum.DIRECTION_SOUTH_EAST))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X + i, mapPoint.Y, map, list);
                        }
                        if (!list2.Contains(DirectionsEnum.DIRECTION_NORTH_WEST))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X - i, mapPoint.Y, map, list);
                        }
                        if (!list2.Contains(DirectionsEnum.DIRECTION_NORTH_EAST))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X, mapPoint.Y + i, map, list);
                        }
                        if (!list2.Contains(DirectionsEnum.DIRECTION_SOUTH_WEST))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X, mapPoint.Y - i, map, list);
                        }
                    }
                    if (this.Diagonal || this.AllDirections)
                    {
                        if (!list2.Contains(DirectionsEnum.DIRECTION_SOUTH))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X + i, mapPoint.Y - i, map, list);
                        }
                        if (!list2.Contains(DirectionsEnum.DIRECTION_NORTH))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X - i, mapPoint.Y + i, map, list);
                        }
                        if (!list2.Contains(DirectionsEnum.DIRECTION_EAST))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X + i, mapPoint.Y + i, map, list);
                        }
                        if (!list2.Contains(DirectionsEnum.DIRECTION_WEST))
                        {
                            MapPoint.AddCellIfValid(mapPoint.X - i, mapPoint.Y - i, map, list);
                        }
                    }
                }
            }
            return list.ToArray();
        }

    }
}
