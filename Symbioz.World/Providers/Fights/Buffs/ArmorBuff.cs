using Symbioz.Protocol.Enums;
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
    /// Buff d'Armure : réduit les dégâts reçus par le combattant pendant N tours.
    /// La valeur brute (Value) est transformée en réduction effective (Delta)
    /// via CalculateArmorValue, qui tient compte des stats du combattant (Sagesse...).
    /// GlobalDamageReduction est la statistique de réduction fixe de dégâts.
    /// </summary>
    public class ArmorBuff : Buff
    {
        // Valeur brute de l'armure issue de l'effet du sort
        public short Value
        {
            get;
            set;
        }

        // Valeur réelle de réduction de dégâts après calcul (prend en compte les stats de la cible)
        public short Delta
        {
            get;
            set;
        }

        // Augmente la réduction de dégâts globale du combattant
        public override void Apply()
        {
            Target.Stats.GlobalDamageReduction += Delta;
        }

        // Retire la réduction de dégâts quand le buff expire ou est dissipé
        public override void Dispell()
        {
            Target.Stats.GlobalDamageReduction -= Delta;
        }

        /// <summary>
        /// Crée le buff d'armure. Delta est calculé immédiatement selon les stats de la cible.
        /// </summary>
        public ArmorBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect, ushort spellId, short value, bool critical, FightDispellableEnum dispelable)
            : base(id, target, caster, level, effect, spellId, critical, dispelable)
        {
            this.Value = value;
            // CalculateArmorValue convertit la valeur brute en réduction effective (formule du jeu)
            this.Delta = (short)target.CalculateArmorValue(Value);
        }

        // Construit le paquet réseau pour informer le client du buff d'armure
        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect((uint)base.Id, base.Target.Id, (short)base.Duration, (sbyte)Dispelable, this.SpellId, 0, 0, (short)Math.Abs(this.Value));
        }
    }
}
