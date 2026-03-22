using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Fights.Buffs
{
    /// <summary>
    /// Buff de type Bouclier : ajoute des points de bouclier au combattant.
    /// Le bouclier absorbe les dégâts avant les points de vie.
    /// Il est retiré (RemoveShield) quand le buff expire ou est dissipé.
    /// </summary>
    public class ShieldBuff : Buff
    {
        // Identifiant de l'action envoyée au client (affiche l'icône bouclier dans l'interface)
        public const ActionsEnum ActionId = ActionsEnum.ACTION_CHARACTER_BOOST_SHIELD;

        // Nombre de points de bouclier accordés par ce buff
        public short Value
        {
            get;
            private set;
        }

        /// <summary>
        /// Crée un buff bouclier avec la valeur de protection indiquée.
        /// </summary>
        public ShieldBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, short value, bool critical, FightDispellableEnum dispelable)
            : base(id, target, caster, level, effect, spellId, critical, dispelable, (short)ActionId)
        {
            this.Value = value;
        }

        // Ajoute les points de bouclier aux stats du combattant
        public override void Apply()
        {
            Target.Stats.AddShield(Value);
        }

        // Retire les points de bouclier quand le buff se termine
        public override void Dispell()
        {
            Target.Stats.RemoveShield(Value);
        }

        // Construit le paquet réseau pour notifier le client de l'ajout du bouclier
        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect((uint)base.Id, base.Target.Id, (short)base.Duration, (sbyte)Dispelable, this.SpellId, 0, (uint)this.Value, (short)this.Value);
        }
    }
}
