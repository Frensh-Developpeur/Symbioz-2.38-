using Symbioz.ORM;
using Symbioz.World.Models.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbioz.Core;
using System.Threading.Tasks;

namespace Symbioz.World.Records.Items
{
    /// <summary>
    /// Set d'équipement : groupe de plusieurs objets qui confèrent des bonus supplémentaires
    /// lorsqu'un personnage en porte plusieurs en même temps.
    /// Par exemple, le set "Tofumanceau" donne des bonus si on porte 2, 3 ou 4 pièces du set.
    /// Effects contient une entrée par palier (Effects[0] = bonus à 1 pièce, Effects[1] = bonus à 2 pièces, etc.).
    /// GetItemSet() permet de retrouver un set en donnant le GId d'un de ses objets membres.
    /// </summary>
    [Table("ItemSets", true)]
    public class ItemSetRecord : ITable
    {
        // Liste de tous les sets d'équipement chargés en mémoire au démarrage
        public static List<ItemSetRecord> ItemsSets = new List<ItemSetRecord>();

        [Primary]
        public int Id;          // Identifiant unique du set

        public string Name;     // Nom du set (ex: "Set Tofumanceau")

        public List<ushort> Items;      // Liste des GIds des objets membres de ce set

        public ItemSetEffects Effects;  // Bonus accordés par palier (1 pièce, 2 pièces, etc.)

        public ItemSetRecord(int id, string name, List<ushort> items, ItemSetEffects effects)
        {
            this.Id = id;
            this.Name = name;
            this.Items = items;
            this.Effects = effects;
        }

        // Retourne la liste des effets actifs pour ce set avec le nombre de pièces portées donné.
        // Si itemCount dépasse le nombre de paliers définis, retourne une liste vide.
        public List<Effect> GetSetEffects(int itemCount)
        {
            if (Effects.SetEffects.Count >= itemCount)
                return Effects.SetEffects[itemCount - 1].ConvertAll<Effect>(x => x.GenerateEffect());
            else
                return new List<Effect>();

        }

        // Retourne le set auquel appartient un objet donné (par son GId), ou null s'il n'appartient à aucun set
        public static ItemSetRecord GetItemSet(ushort itemGID)
        {
            return ItemsSets.Find(x => x.Items.Contains(itemGID));
        }
    }

    /// <summary>
    /// Structure auxiliaire stockant les effets d'un set d'équipement par palier.
    /// SetEffects est une liste de listes : chaque sous-liste correspond aux effets d'un palier.
    /// Ex: SetEffects[0] = effets à 1 pièce, SetEffects[1] = effets à 2 pièces...
    /// Deserialize() lit la chaîne stockée en BDD (format "effet1,effet2|effet3,effet4|...").
    /// </summary>
    public class ItemSetEffects
    {
        // Liste des paliers ; chaque palier est lui-même une liste d'effets (EffectInstance)
        public List<List<EffectInstance>> SetEffects = new List<List<EffectInstance>>();

        public static ItemSetEffects Deserialize(string str)
        {
            ItemSetEffects itemSetEffects = new ItemSetEffects();


            foreach (var item in str.Split('|'))
            {
                List<EffectInstance> effects = new List<EffectInstance>();
                foreach (var subItem in item.Split(','))
                {
                    if (subItem != string.Empty)
                        effects.Add(subItem.XMLDeserialize<EffectInstance>());
                }

                itemSetEffects.SetEffects.Add(effects);

            }
            itemSetEffects.SetEffects.Remove(itemSetEffects.SetEffects.Last());
            return itemSetEffects;
        }
    }
}
