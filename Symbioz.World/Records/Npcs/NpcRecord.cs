using Symbioz.ORM;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Entities.Look;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Npcs
{
    /// <summary>
    /// Template d'un PNJ : contient les données statiques partagées entre tous les spawns de ce PNJ.
    ///
    /// Un même NpcRecord peut être placé sur plusieurs maps via plusieurs NpcSpawnRecord.
    /// Le template contient :
    ///   - Look : apparence visuelle (classe, couleurs)
    ///   - Messages / Replies : textes de dialogue encodés en CSV
    ///   - ActionTypes : liste des actions disponibles (parler, vendre, soigner...) encodées en sbyte
    ///   - ActionTypesEnum : version enum calculée à la construction
    /// </summary>
    [Table("Npcs", true, 2)]
    public class NpcRecord : ITable
    {
        // Liste de tous les templates de PNJ chargés en mémoire
        public static List<NpcRecord> Npcs = new List<NpcRecord>();

        [Primary]
        public ushort Id;           // Identifiant unique du PNJ

        public string Name;         // Nom affiché du PNJ

        public CSVDoubleArray Messages; // Messages de dialogue du PNJ

        public CSVDoubleArray Replies;  // Réponses disponibles dans les dialogues

        public List<sbyte> ActionTypes;     // Types d'actions disponibles (valeurs numériques)

        [Ignore]
        public List<NpcActionTypeEnum> ActionTypesEnum; // Types d'actions convertis en enum

        public ContextActorLook Look;   // Apparence visuelle du PNJ

        public NpcRecord(ushort id, string name, CSVDoubleArray messages, CSVDoubleArray replies, List<sbyte> actionTypes,
            ContextActorLook look)
        {
            this.Id = id;
            this.Name = name;
            this.Messages = messages;
            this.Replies = replies;
            this.ActionTypes = actionTypes;
            this.ActionTypesEnum = actionTypes.ConvertAll<NpcActionTypeEnum>(x => (NpcActionTypeEnum)x);
            this.Look = look;
            this.Look.SetColors(ContextActorLook.GetConvertedColors(this.Look.Colors.ToArray()));
        }

        public static NpcRecord GetNpc(ushort id)
        {
            return NpcRecord.Npcs.Find(x => x.Id == id);
        }
    }
}
