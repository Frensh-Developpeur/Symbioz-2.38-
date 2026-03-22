using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.Marks.Shapes;
using Symbioz.World.Models.Fights.Spells;
using Symbioz.World.Models.Maps;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Marks
{
    /// <summary>
    /// Piège : marque au sol posée par le Roublard ou d'autres classes.
    /// Un piège est invisible pour l'équipe ennemie (IsVisibleFor retourne false pour les ennemis).
    /// Quand un adversaire marche dessus, il se déclenche (Trigger), applique ses effets
    /// sur une zone autour du centre du piège, puis disparaît immédiatement (RemoveMark).
    /// BreakMove = true : le déplacement du combattant est interrompu à l'entrée dans le piège.
    /// </summary>
    public class Trap : Mark
    {
        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.TRAP;
            }
        }
        // Le piège interrompt le mouvement dès qu'un combattant entre dans sa zone
        public override bool BreakMove
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// Obtenir le MarkCellsTypeEnum en fonction de l'effet => C : lozenge, G : Cross, sinon Square cells par cells
        /// </summary>
        /// <param name="id"></param>
        /// <param name="source"></param>
        /// <param name="spellLevel"></param>
        /// <param name="effect"></param>
        /// <param name="centerPoint"></param>
        /// <param name="size"></param>
        public Trap(short id, Fighter source, SpellLevelRecord spellLevel, EffectInstance effect,
            MapPoint centerPoint, Zone zone, Color color)
            : base(id, source, spellLevel, effect, centerPoint, zone, color, MarkTriggerTypeEnum.AFTER_MOVE)
        {

        }


        // Retourne une version cachée du piège pour les ennemis (position -1, aucune cellule visible)
        // Cela empêche les adversaires de voir où le piège a été posé
        public override GameActionMark GetHiddenGameActionMark()
        {
            return new GameActionMark(Source.Id, Source.Team.Id, 1, 1, Id, (sbyte)Type,
               -1, new GameActionMarkedCell[0], true);
        }

        // Seuls les alliés du poseur peuvent voir le piège ; les ennemis le reçoivent en version cachée
        public override bool IsVisibleFor(Fighter fighter)
        {
            return fighter.IsFriendly(Source);
        }
        // Déclenché quand un combattant marche sur le piège :
        // 1. Supprime le piège du terrain (il ne se déclenche qu'une fois)
        // 2. Applique les effets du sort de déclenchement centrés sur le piège
        public override void Trigger(Fighter source, MarkTriggerTypeEnum type, object token)
        {
            SpellLevelRecord triggerLevel = TriggerSpell.GetLevel(SpellLevel.Grade);
            this.Fight.RemoveMark(source, this);
            SpellEffectsManager.Instance.HandleEffects(Source, triggerLevel, CenterPoint, false);
        }


    }
}
