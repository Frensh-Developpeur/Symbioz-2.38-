
using Symbioz.Protocol.Enums;
using Symbioz.World.Records;
using Symbioz.World.Records.Maps;

namespace Symbioz.World.Models.Maps.Shapes
{
    /// <summary>
    /// Interface implémentée par toutes les formes de zone de sort dans Dofus.
    ///
    /// Une forme calcule l'ensemble des cellules touchées par un sort
    /// à partir d'une cellule centrale et d'une direction.
    ///
    /// Formes connues : Cross (croix), Lozenge (losange), Line (ligne), Cone (cône),
    /// Single (cellule unique), All (toute la map), HalfLozenge (demi-losange), Square (carré).
    ///
    /// GetCells() est la méthode principale à implémenter.
    /// </summary>
    public interface IShape
    {
        // Nombre de cellules couverte par la zone (surface de la forme)
        uint Surface
        {
            get;
        }

        // Rayon minimum de la zone (anneau creux : les cellules à moins de MinRadius sont exclues)
        byte MinRadius
        {
            get;
            set;
        }

        // Direction du lanceur (utilisée pour les formes orientées : Cone, Line, HalfLozenge...)
        DirectionsEnum Direction
        {
            get;
            set;
        }

        // Rayon maximum de la zone (portée de la forme)
        byte Radius
        {
            get;
            set;
        }

        // Calcule et retourne toutes les cellules touchées par la forme,
        // centrée sur centerCell avec la configuration Direction/Radius/MinRadius.
        short[] GetCells(short centerCell, MapRecord map);
    }
}
