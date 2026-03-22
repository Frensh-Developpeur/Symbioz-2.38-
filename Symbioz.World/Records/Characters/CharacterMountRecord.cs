using Symbioz.ORM;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Records.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Providers.Items;
using Symbioz.World.Models.Entities;

namespace Symbioz.World.Records.Characters
{
    /// <summary>
    /// Instance d'une monture appartenant à un personnage.
    /// Stocke les données propres à cette monture (nom, sexe, état, effets de statistiques).
    /// Le template commun (apparence, GId de certificat) est dans MountRecord.
    ///
    /// Setted = la monture est actuellement attachée à l'équipement du personnage.
    /// Toggled = la monture est "activée" (le personnage est monté dessus).
    /// CreateCertificate() génère un item de certificat échangeable depuis cette monture.
    /// </summary>
    [Table("CharactersMounts"), Resettable]
    public class CharacterMountRecord : ITable
    {
        // Nom par défaut donné à une nouvelle monture avant que le joueur la renomme
        public const string DefaultMountName = "SansNom";

        // Niveau affiché de la monture côté client (fixé à 100 sur ce serveur)
        public const sbyte DisplayedMountLevel = 100;

        // Liste de toutes les montures des personnages chargées en mémoire
        public static List<CharacterMountRecord> CharactersMounts = new List<CharacterMountRecord>();

        [Primary]
        public long UId;            // Identifiant unique de cette monture

        [Update]
        public long CharacterId;    // ID du personnage propriétaire de cette monture

        public bool Sex;            // Sexe de la monture (false=mâle, true=femelle)

        public bool IsRideable;     // Si true, le personnage peut monter dessus

        public bool IsWild;         // Si true, la monture est sauvage (non apprivoisée)

        public bool IsFecondationReady; // Si true, la monture peut se reproduire

        public int ModelId;         // ID du modèle de monture (référence vers MountRecord.Id)

        [Update]
        public string Name;         // Nom personnalisé de la monture (modifiable par le joueur)

        [Xml, Update]
        public List<EffectInteger> Effects; // Statistiques de la monture (force, vitalité, etc.)

        [Ignore]
        public MountRecord Template // Template de la monture (chargé depuis MountRecord, non persisté)
        {
            get
            {
                return MountRecord.GetMount(ModelId);
            }
        }
        [Update]
        public bool Setted;

        [Update]
        public bool Toggled;

        public CharacterMountRecord(long uid, long characterId, bool sex, bool isRideable, bool isWild, bool isFecondationReady, int modelId,
            string name, List<EffectInteger> effects, bool setted, bool toggled)
        {
            this.UId = uid;
            this.CharacterId = characterId;
            this.Sex = sex;
            this.IsRideable = isRideable;
            this.IsWild = isWild;
            this.IsFecondationReady = isFecondationReady;
            this.ModelId = modelId;
            this.Name = name;
            this.Effects = effects;
            this.Setted = setted;
            this.Toggled = toggled;
        }
        public MountClientData GetMountClientData()
        {
            return new MountClientData()
            {
                aggressivityMax = 10,
                ancestor = new int[0],
                maturityForAdult = 10,
                behaviors = new int[0],
                boostLimiter = 10,
                boostMax = 10,
                effectList = Effects.ConvertAll<ObjectEffectInteger>(x => x.GetObjectEffect() as ObjectEffectInteger).ToArray(),
                energy = 10,
                energyMax = 100,
                experience = 1,
                experienceForLevel = 2,
                experienceForNextLevel = 3,
                fecondationTime = 1,
                id = UId,
                isFecondationReady = IsFecondationReady,
                isRideable = IsRideable,
                isWild = IsWild,
                level = DisplayedMountLevel,
                love = 1,
                loveMax = 2,
                maturity = 1,
                maxPods = 222,
                model = (uint)ModelId,
                name = Name,
                ownerId = (int)CharacterId,
                reproductionCount = 4,
                reproductionCountMax = 4,
                serenity = 1,
                serenityMax = 2,
                sex = Sex,
                stamina = 1,
                staminaMax = 2,
            };
        }
        public CharacterItemRecord CreateCertificate(Character character)
        {
            ItemRecord template = ItemRecord.GetItem(Template.ItemGId);
            var item = template.GetCharacterItem(CharacterId, 1);
            item.Effects.AddRange(ItemGenerationProvider.GetCertificateEffects(Name, character.Name, (ushort)DisplayedMountLevel,
                (int)UId, (ushort)ModelId));
            return item;
        }
        public static CharacterMountRecord New(CharacterItemRecord item)
        {
            MountRecord template = MountRecord.GetMount(item.GId);
            long uid = CharactersMounts.DynamicPop(x => x.UId);

            return new CharacterMountRecord(uid, item.CharacterId, false, true, false, false,
                template.Id, DefaultMountName, template.Effects.ConvertAll<EffectInteger>(x => x.GenerateEffect() as EffectInteger), false, false);
        }

        public static List<CharacterMountRecord> GetCharacterMounts(long id)
        {
            return CharactersMounts.FindAll(x => x.CharacterId == id);
        }

        [RemoveWhereId]
        public static List<CharacterMountRecord> RemoveAll(long id)
        {
            return CharactersMounts.FindAll(x => x.CharacterId == id);
        }
    }
}
