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

namespace Symbioz.World.Providers.Fights.Effects.Marks
{
    /// <summary>
    /// Déclenche l'explosion des bombes (BombFighter) présentes dans la zone.
    /// N'agit que sur les BombFighter (filtre via OfType) — ignoré sur les autres combattants.
    /// Utilisé par le sort "Détonation" du Roublard pour exploser les bombes alliées.
    /// </summary>
    [SpellEffectHandler(EffectsEnum.Effect_Detonate)]
    public class Detonate : SpellEffectHandler
    {
        public Detonate(Fighter source, SpellLevelRecord level, EffectInstance effect,
         Fighter[] targets, MapPoint castPoint, bool critical)
            : base(source, level, effect, targets, castPoint, critical)
        {

        }
        public override bool Apply(Fighter[] targets)
        {
            // OfType<BombFighter> filtre pour n'agir que sur les bombes, pas les joueurs/monstres
            foreach (BombFighter target in targets.OfType<BombFighter>())
            {
                target.Detonate(Source);
            }
            return true;
        }
    }
}
