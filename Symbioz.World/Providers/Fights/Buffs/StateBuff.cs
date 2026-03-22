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
    /// Buff d'état temporaire : applique un état (State) sur un combattant pendant N tours.
    /// Un état dans Dofus est une condition spéciale (ex : empoisonné, enraciné, Osamodas...).
    /// L'état est ajouté lors de Apply() et retiré lors de Dispell().
    /// </summary>
    public class StateBuff : Buff
    {
        // Les données de l'état à appliquer (Id, nom, effets visuels...)
        public SpellStateRecord StateRecord
        {
            get;
            private set;
        }

        /// <summary>
        /// Crée un buff d'état.
        /// </summary>
        /// <param name="stateRecord">L'état à appliquer (récupéré depuis la BDD des sorts)</param>
        public StateBuff(int id, Fighter target, Fighter caster, SpellLevelRecord level, EffectInstance effect,
            ushort spellId, bool critical, FightDispellableEnum dispelable, SpellStateRecord stateRecord)
            : base(id, target, caster, level, effect, spellId, critical, dispelable)
        {
            this.StateRecord = stateRecord;
        }

        // Ajoute l'état sur la cible (ex : rend le personnage "enraciné" ou "invisible")
        public override void Apply()
        {
            this.Target.AddState(StateRecord);
        }

        // Retire l'état de la cible quand le buff expire ou est dissipé
        public override void Dispell()
        {
            this.Target.RemoveState(StateRecord);
        }

        // Construit le message réseau à envoyer au client pour afficher le buff d'état dans l'interface
        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostStateEffect((uint)Id, Target.Id, Duration, (sbyte)Dispelable, SpellId,(uint) Effect.EffectUID, Effect.EffectUID, StateRecord.Id, StateRecord.Id);
        }
    }
}
