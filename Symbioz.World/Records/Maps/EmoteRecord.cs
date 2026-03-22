using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Maps
{
    /// <summary>
    /// Emote (animation/geste) qu'un personnage peut exécuter.
    /// Les emotes s'achètent ou se débloquent et permettent au personnage d'effectuer
    /// des animations (saluer, danser, rire...). Certaines sont des "auras" (effets visuels permanents).
    /// Un personnage stocke ses emotes débloquées dans CharacterRecord.KnownEmotes.
    /// </summary>
    [Table("Emotes")]
    public class EmoteRecord : ITable
    {
        // Liste de toutes les emotes chargées en mémoire au démarrage
        public static List<EmoteRecord> Emotes = new List<EmoteRecord>();

        [Primary]
        public byte Id;         // Identifiant unique de l'emote

        public string Name;     // Nom de l'emote (ex: "Saluer", "Danser")

        public bool IsAura;     // True si l'emote est une aura (effet visuel permanent autour du personnage)

        public ushort AuraBones; // ID des bones/animations utilisés pour l'aura (0 si non-aura)

        public EmoteRecord(byte id,string name,bool isAura,ushort auraBones)
        {
            this.Id = id;
            this.Name = name;
            this.IsAura = isAura;
            this.AuraBones = auraBones;
        }

        // Retourne une emote par son identifiant
        public static EmoteRecord GetEmote(byte id)
        {
            return Emotes.Find(x => x.Id == id);
        }
    }
}
