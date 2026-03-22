using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Spells
{
    /// <summary>
    /// Données d'un sort de bombe (utilisé par la classe Roublard).
    /// Associe un sort de pose de bombe à ses sorts d'explosion, de mur et d'explosion sur cible.
    /// Lorsqu'une bombe Roublard explose, le serveur recherche ce record pour déclencher le bon sort.
    /// </summary>
    [Table("SpellsBombs")]
    public class SpellBombRecord : ITable
    {
        // Liste de tous les sorts de bombes chargés en mémoire au démarrage
        public static List<SpellBombRecord> SpellsBombs = new List<SpellBombRecord>();

        public ushort SpellId;                  // ID du sort de pose de bombe (sort lancé par le Roublard)

        public ushort ExplosionSpellId;         // ID du sort déclenché lors de l'explosion de la bombe

        public int WallColor;                   // Couleur du mur créé par les bombes (encodée en entier)

        public ushort WallSpellId;              // ID du sort déclenché quand deux bombes forment un mur

        public ushort CibleExplosionSpellId;    // ID du sort appliqué sur la cible lors de l'explosion

        public SpellBombRecord(ushort spellId, ushort explosionSpellId, int wallColor, ushort wallSpellId, ushort cibleExplosionSpellId)
        {
            this.SpellId = spellId;
            this.ExplosionSpellId = explosionSpellId;
            this.WallColor = wallColor;
            this.WallSpellId = wallSpellId;
            this.CibleExplosionSpellId = cibleExplosionSpellId;
        }

        public static SpellBombRecord GetSpellBombRecord(ushort spellId)
        {
            return SpellsBombs.FirstOrDefault(x => x.SpellId == spellId);
        }
    }
}
