using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Symbioz.World.Records.Maps
{
    /// <summary>
    /// Position géographique d'une map dans le monde de Dofus.
    /// Contient les coordonnées (X, Y) sur la carte du monde, le nom de la zone,
    /// et un champ Capabilities (masque de bits) qui détermine ce qui est autorisé sur cette map.
    /// Chaque bit de Capabilities correspond à une permission (défi, agression, échange, taverne...).
    /// Utilisé par MapRecord.Position pour afficher les coordonnées et vérifier les droits.
    /// </summary>
    [Table("MapPositions")]
    public class MapPositionRecord : ITable
    {
        // Liste de toutes les positions de maps chargées en mémoire au démarrage
        public static List<MapPositionRecord> MapPositions = new List<MapPositionRecord>();

        public int Id;          // ID de la map correspondante (même valeur que MapRecord.Id)

        public int X;           // Coordonnée horizontale sur la carte du monde

        public int Y;           // Coordonnée verticale sur la carte du monde

        public string Name;     // Nom de la zone (ex: "Plaine des Scarafeuilles [3,-5]")

        public bool Outdoor;    // True = map en extérieur (peut être utilisée pour RandomOutdoorMap)

        // Masque de bits des capacités/permissions de cette map.
        // Chaque propriété ci-dessous lit un bit spécifique de cet entier.
        public int Capabilities;

        [Ignore]
        public Point Point      // Coordonnées sous forme d'objet Point (pour calculs géométriques)
        {
            get
            {
                return new Point(X, Y);
            }
        }

        [Ignore]
        public bool AllowChallenge  // Bit 0 : autoriser les défis de combat sur cette map
        {
            get
            {
                return (this.Capabilities & 1) != 0;
            }
        }
        [Ignore]
        public bool AllowAggression        // Bit 1 : autoriser les agressions PvP
        {
            get
            {
                return (this.Capabilities & 2) != 0;
            }
        }
        [Ignore]
        public bool AllowTeleportTo        // Bit 2 : autoriser la téléportation vers cette map
        {
            get
            {
                return (this.Capabilities & 4) != 0;
            }
        }
        [Ignore]
        public bool AllowTeleportFrom      // Bit 3 : autoriser la téléportation depuis cette map
        {
            get
            {
                return (this.Capabilities & 8) != 0;
            }
        }
        [Ignore]
        public bool AllowExchangesBetweenPlayers  // Bit 4 : autoriser les échanges entre joueurs
        {
            get
            {
                return (this.Capabilities & 16) != 0;
            }
        }
        [Ignore]
        public bool AllowHumanVendor       // Bit 5 : autoriser les vendeurs joueurs
        {
            get
            {
                return (this.Capabilities & 32) != 0;
            }
        }
        [Ignore]
        public bool AllowCollector         // Bit 6 : autoriser les percepteurs de guilde
        {
            get
            {
                return (this.Capabilities & 64) != 0;
            }
        }
        [Ignore]
        public bool AllowSoulCapture       // Bit 7 : autoriser la capture d'âme de monstre
        {
            get
            {
                return (this.Capabilities & 128) != 0;
            }
        }
        [Ignore]
        public bool AllowSoulSummon        // Bit 8 : autoriser l'invocation via pierre d'âme
        {
            get
            {
                return (this.Capabilities & 256) != 0;
            }
        }
        [Ignore]
        public bool AllowTavernRegen       // Bit 9 : régénération de points de vie en taverne
        {
            get
            {
                return (this.Capabilities & 512) != 0;
            }
        }
        [Ignore]
        public bool AllowTombMode          // Bit 10 : autoriser le mode tombe (personnage décédé visible)
        {
            get
            {
                return (this.Capabilities & 1024) != 0;
            }
        }
        [Ignore]
        public bool AllowTeleportEverywhere // Bit 11 : téléportation libre partout sur cette map
        {
            get
            {
                return (this.Capabilities & 2048) != 0;
            }
        }
        [Ignore]
        public bool AllowFightChallenges   // Bit 12 : autoriser les défis de combat
        {
            get
            {
                return (this.Capabilities & 4096) != 0;
            }
        }

        public MapPositionRecord(int id, int x, int y, string name, bool outdoor, int capabilities)
        {
            this.Id = id;
            this.X = x;
            this.Y = y;
            this.Name = name;
            this.Outdoor = outdoor;
            this.Capabilities = capabilities;
        }

        public static MapPositionRecord GetMapPosition(int mapId)
        {
            return MapPositions.Find(x => x.Id == mapId);
        }
        public override string ToString()
        {
            return string.Format("[{0},{1}]", X, Y);
        }

    }
}
