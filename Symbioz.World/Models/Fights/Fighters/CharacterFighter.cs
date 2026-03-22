using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Fights.Results;
using Symbioz.World.Models.Fights.Spells;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Items;
using Symbioz.World.Records.Characters;
using Symbioz.World.Records.Items;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Records.Idols;

namespace Symbioz.World.Models.Fights.Fighters
{
    /// <summary>
    /// Représente un joueur (personnage) dans un combat.
    /// Hérite de PlayableFighter (combattant pouvant jouer activement).
    /// Gère les actions spécifiques aux joueurs : lancer des sorts, utiliser une arme,
    /// quitter le combat, se synchroniser avec le client, etc.
    /// </summary>
    public class CharacterFighter : PlayableFighter
    {
        // Déclenché quand le joueur quitte pendant la phase de placement
        public event Action OnLeavePreFightEvt;

        // Déclenché quand le joueur utilise son arme (corps-à-corps)
        public event Action<Fighter> OnWeaponUsedEvt;

        // Référence au personnage réel (données, inventaire, statistiques...)
        public Character Character
        {
            get;
            set;
        }

        public override string Name
        {
            get
            {
                return Character.Name;
            }
        }

        public override ushort Level
        {
            get
            {
                return Character.Level;
            }
        }

        public override bool Sex
        {
            get
            {
                return Character.Record.Sex;
            }
        }
        private SpellLevelRecord WeaponLevel
        {
            get;
            set;
        }
        private WeaponRecord WeaponTemplate
        {
            get;
            set;
        }
        private bool HasWeaponEquiped
        {
            get
            {
                return WeaponTemplate != null;
            }
        }
        public CharacterFighter(Character character, FightTeam team, ushort mapPosition)
            : base(team, mapPosition)
        {
            this.Character = character;
        }
        /// <summary>
        /// Initialise les données de combat du personnage :
        /// copie l'apparence, crée les statistiques de combat,
        /// et charge l'arme équipée si le personnage en a une.
        /// </summary>
        public override void Initialize()
        {
            this.Id = (int)Character.Id;
            this.Look = Character.Look.Clone();
            // Crée les stats de combat à partir des stats du personnage (avec équipement)
            this.Stats = new FighterStats(Character);

            // Si le personnage a une arme équipée, charge son template et son niveau de sort
            if (Character.Inventory.HasWeaponEquiped)
            {
                this.WeaponTemplate = WeaponRecord.GetWeapon(Character.Inventory.GetWeapon().GId);
                this.WeaponLevel = WeaponManager.Instance.GetWeaponSpellLevel(WeaponTemplate);
            }

            base.Initialize();
        }
        public override void OnTurnStarted()
        {
         
            SendFighterStatsList();
            base.OnTurnStarted();
        }
        public override void OnJoined()
        {
            this.Fight.SendGameFightJoinMessage(this);
            ShowPlacementCells();
            this.Fight.ShowFighters(this);
            this.ShowReadyFighters();
            base.OnJoined();
        }
        public void ShowReadyFighters()
        {
            foreach (var fighter in Fight.GetFighters<CharacterFighter>().FindAll(x => x.IsReady))
            {
                Character.Client.Send(new GameFightHumanReadyStateMessage((ulong)fighter.Id, true));
            }
        }
        public void ToggleReady(bool isReady)
        {
            this.IsReady = isReady;
            Fight.OnSetReady(this, IsReady);
        }
        public void SendFighterStatsList()
        {
            Character.Client.Send(new FighterStatsListMessage(Stats.GetCharacterCharacteristics(Character)));
        }

        public IdolRecord[] GetIdols()
        {
            return Character.Record.Idols.ConvertAll(x => IdolRecord.GetIdol(x)).ToArray();
        }

        public override void Move(List<short> movementKeys)
        {
            base.Move(movementKeys.ToList());
        }
        public override void OnTurnEnded()
        {
            base.OnTurnEnded();
        }
        public void OnDisconnected()
        {
            Leave(true);
        }

        /// <summary>
        /// Gère le départ d'un joueur du combat.
        /// - Si le combat n'a pas encore commencé : retire simplement le combattant de l'équipe.
        /// - Si le combat est en cours : met ses PV à 0 (mort), attend la synchronisation
        ///   des clients via un Synchronizer personnel, puis le fait quitter proprement.
        /// </summary>
        public void Leave(bool teleportToSpawn)
        {
            if (!Fight.Started)
            {
                Team.RemoveFighter(this);

                if (OnLeavePreFightEvt != null)
                    OnLeavePreFightEvt();


                if (!Fight.CheckFightEnd())
                {
                    Fight.CheckFightStart();
                }

                if (teleportToSpawn)
                    Character.RejoinMap(Fight.FightType, false, Fight.SpawnJoin);
                else
                    Character.RejoinMap(Fight.FightType, false, false);

            }
            else
            {

                if (!Left)
                {
                    if (Alive)
                    {
                        this.Stats.CurrentLifePoints = 0;
                        this.Fight.CheckDeads();
                    }
                    if (!Fight.Ended)
                    {
                        Synchronizer sync = new Synchronizer(this.Fight, new PlayableFighter[]
                    {
                        this
                    });
                        sync.Success += delegate (Synchronizer obj)
                        {
                            this.OnPlayerReadyToLeave();
                        };
                        sync.Timeout += delegate (Synchronizer obj, PlayableFighter[] laggers)
                        {
                            this.OnPlayerReadyToLeave();
                        };
                        this.PersonalSynchronizer = sync;
                        sync.Start();
                    }
                    //if (Fight.SequencesManager.SequencesCount == 0)
                    //    OnPlayerReadyToLeave();
                    //else
                    //{
                    //    this.WaitAcknolegement = true;
                    //    this.OnSequencesAcknowleged = OnPlayerReadyToLeave;
                    //}

                    this.Left = true;
                }
            }
        }
        public void ToggleSyncReady(bool isReady)
        {
            if (this.PersonalSynchronizer != null)
            {
                this.PersonalSynchronizer.ToggleReady(this, isReady);
            }
            else
            {
                if (base.Fight.Synchronizer != null)
                {
                    base.Fight.Synchronizer.ToggleReady(this, isReady);
                }
            }
        }
        public override void Kick()
        {
            Leave(false);
        }
        public void ShowPlacementCells()
        {
            this.Send(new GameFightPlacementPossiblePositionsMessage(Fight.RedTeam.GetPlacements(), Fight.BlueTeam.GetPlacements(), Team.Id));
        }

        public override GameFightFighterInformations GetFightFighterInformations()
        {
            return new GameFightCharacterInformations(Id, Look.ToEntityLook(), new EntityDispositionInformations((short)CellId,
                (sbyte)Direction), Team.Id, 0, Alive, Stats.GetFightMinimalStats(), new ushort[0], Character.Name, Character.GetPlayerStatus(),
                (byte)Character.Level, Character.Record.Alignment.GetActorAlignmentInformations(),
                Character.Record.BreedId, Character.Record.Sex);
        }


        public override void AddCooldownOnSpell(Fighter source, ushort spellId, short value)
        {
            base.AddCooldownOnSpell(source, spellId, value);
            this.Character.Client.Send(new GameActionFightSpellCooldownVariationMessage((ushort)ActionsEnum.ACTION_CHARACTER_ADD_SPELL_COOLDOWN,
                        source.Id, Id, spellId, value));
        }
        /// <summary>
        /// Lance un sort. Si le sort est "coup de poing" (sort d'arme de base),
        /// redirige vers le système de corps-à-corps (CloseCombat) avec l'arme équipée
        /// ou les poings nus. Sinon, délègue au comportement standard (PlayableFighter).
        /// </summary>
        public override bool CastSpell(SpellRecord spell, sbyte grade, short cellId, int targetId = 0, bool verif = true)
        {
            if (spell.Id == WeaponManager.PunchSpellId)
            {
                MapPoint castPoint = new MapPoint(cellId);

                if (HasWeaponEquiped)
                {
                    // Utilise la zone de l'arme équipée (ex: épée = ligne, arc = distance...)
                    string rawZone = WeaponManager.Instance.GetRawZone(WeaponTemplate.TypeEnum);

                    return CloseCombat(WeaponLevel, castPoint, rawZone, WeaponTemplate.Id);

                }
                else
                {
                    // Pas d'arme : utilise le sort "poing" par défaut
                    SpellLevelRecord level = GetSpell(WeaponManager.PunchSpellId).Template.GetLastLevel();
                    return CloseCombat(level, castPoint, WeaponManager.PunchRawZone);
                }
            }
            else
            {
                return base.CastSpell(spell, grade, cellId, targetId, verif);
            }
        }
        private bool CloseCombat(SpellLevelRecord level, MapPoint castPoint, string rawZone, ushort weaponGId = 0)
        {
            SpellCastResultEnum canCast = CanCastSpell(level,CellId,castPoint.CellId);

            if (canCast != SpellCastResultEnum.Ok)
            {
                OnSpellCastFailed(canCast, level);
                return false;
            }

            if (OnWeaponUsedEvt != null)
                OnWeaponUsedEvt(this);

            Fight.SequencesManager.StartSequence(SequenceTypeEnum.SEQUENCE_WEAPON);

            FightSpellCastCriticalEnum fightSpellCastCriticalEnum = this.RollCriticalDice(level);
            bool criticalHit = fightSpellCastCriticalEnum == FightSpellCastCriticalEnum.CRITICAL_HIT;

            EffectInstance[] effects = (criticalHit ? level.CriticalEffects : level.Effects).ToArray();



            Fight.Send(new GameActionFightCloseCombatMessage((ushort)ActionsEnum.ACTION_FIGHT_CLOSE_COMBAT,
           Id, false, false, 0, castPoint.CellId, (sbyte)fightSpellCastCriticalEnum, weaponGId));

            SpellEffectsManager.Instance.HandleEffects(this, effects, level, castPoint, rawZone,
                WeaponManager.WeaponTargetMask, criticalHit);



            this.UseAp(level.ApCost);

            this.OnSpellCasted(level, CellId, fightSpellCastCriticalEnum);
            Fight.SequencesManager.EndSequence(SequenceTypeEnum.SEQUENCE_WEAPON);
            Fight.CheckDeads();
            Fight.CheckFightEnd();
            return true;
        }

        public override FightTeamMemberInformations GetFightTeamMemberInformations()
        {
            return new FightTeamMemberCharacterInformations((double)Id, Character.Name, (byte)Character.Level);
        }

        public void OnPlayerReadyToLeave()
        {
            this.PersonalSynchronizer = null;

            if (this.Fight != null && !this.Fight.CheckFightEnd()) // ??? Fight != null.??
            {
                this.Team.RemoveFighter(this);
                this.Team.AddLeaver(this);

                if (IsFighterTurn)
                {
                    this.Fight.StopTurn();
                }

                //  fighter.ResetFightProperties();
                this.Character.RejoinMap(Fight.FightType, false, Fight.SpawnJoin);
            }
        }
        public override CharacterSpell GetSpell(ushort spellId)
        {
            return Character.GetSpell(spellId);
        }
        public override IFightResult GetFightResult()
        {
            return new FightPlayerResult(this, base.GetFighterOutcome(), base.Loot);
        }



        public override void Send(Message message)
        {
            Character.Client.Send(message);
        }
        public override Character GetCharacterPlaying()
        {
            return Character;
        }
    }
}
