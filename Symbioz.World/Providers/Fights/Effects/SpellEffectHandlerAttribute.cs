using Symbioz.Protocol.Selfmade.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects
{
    /// <summary>
    /// Attribut qui associe un SpellEffectHandler à un ou plusieurs effets de sort (EffectsEnum).
    /// Placé sur les classes héritant de SpellEffectHandler, il permet au SpellEffectsProvider
    /// de savoir quel handler instancier pour chaque type d'effet.
    /// AllowMultiple = true : un handler peut gérer plusieurs effets (ex: DirectDamage gère
    /// les 5 éléments : feu, eau, terre, air, neutre).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SpellEffectHandlerAttribute : Attribute
    {
        // L'effet de sort que ce handler est capable de traiter
        public EffectsEnum Effect
        {
            get;
            set;
        }

        public SpellEffectHandlerAttribute(EffectsEnum effect)
        {
            this.Effect = effect;
        }
    }
}
