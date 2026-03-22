using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities.Stats;
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
    /// Buff de statistique temporaire : modifie une Characteristic du combattant pendant N tours.
    /// Par exemple, un buff de Force +50 pendant 3 tours ajoute 50 au contexte de la stat Force,
    /// et le retire à la dissipation. La stat Characteristic.Context représente le bonus en combat.
    /// </summary>
    public class StatBuff : Buff
    {
        // Valeur du bonus (positive = buff, négative = debuff)
        public short Value
        {
            get;
            private set;
        }
        // La statistique du combattant modifiée par ce buff (Force, Agilité, PA, PM...)
        public Characteristic Caracteristic
        {
            get;
            set;
        }
        public short Delta { get; private set; }

        public StatBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, short value, Characteristic caracteristic, bool critical, FightDispellableEnum dispelable)
            : base(id, target, caster, level, effect, spellId, critical, dispelable)
        {
            this.Value = value;
            this.Caracteristic = caracteristic;
        }
        public StatBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, short value, Characteristic caracteristic, bool critical, FightDispellableEnum dispelable, short customActionId)
            : base(id, target, caster, level, effect, spellId, critical, dispelable, customActionId)
        {
            this.Value = value;
            this.Caracteristic = caracteristic;
        }
        // Applique le buff : ajoute la valeur au contexte de la caractéristique
        public override void Apply()
        {
            this.Caracteristic.Context += this.Value;
        }
        // Dissipe le buff : retire la valeur du contexte de la caractéristique
        public override void Dispell()
        {
            this.Caracteristic.Context -= this.Value;
        }
        // Sérialise le buff pour l'envoi au client (affichage dans l'interface de combat)
        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect((uint)base.Id, base.Target.Id, (short)base.Duration, (sbyte)Dispelable, this.SpellId, 0, 0, (short)Math.Abs(this.Value));
        }
    }
}
