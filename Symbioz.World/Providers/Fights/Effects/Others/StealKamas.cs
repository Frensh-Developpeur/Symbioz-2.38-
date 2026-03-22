using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Effects.Others
{
    /// <summary>
    /// Donne des kamas au lanceur (si c'est un personnage joueur).
    /// N'a aucun effet si le lanceur est une invocation ou un monstre (pas de CharacterFighter).
    /// La valeur est tirée aléatoirement entre DiceMin et DiceMax.
    /// OnKamasGained notifie le client (mise à jour de l'affichage des kamas).
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_StealKamas)]
    class StealKamas : SpellEffectHandler
    {
        public StealKamas(Fighter source, SpellLevelRecord level, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // Cast pour vérifier que le lanceur est bien un joueur (pas un monstre)
            CharacterFighter source = Source as CharacterFighter;

            if (source != null)
            {
                ushort value = Effect.RandomizeMinMax();
                source.Character.AddKamas(value);
                source.Character.OnKamasGained(value);
            }

            return true;
        }
    }
}
