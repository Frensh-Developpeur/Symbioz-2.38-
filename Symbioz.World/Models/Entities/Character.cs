using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Handlers.RolePlay;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Network;
using Symbioz.World.Providers.Maps.Path;
using Symbioz.World.Records;
using System;
using System.Collections.Generic;
using Symbioz.World.Records.Almanach;
using System.Drawing;
using System.Linq;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Items;
using Symbioz.World.Providers;
using Symbioz.World.Models.Dialogs;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Exchanges;
using Symbioz.World.Providers.Items;
using Symbioz.World.Models.Dialogs.DialogBox;
using Symbioz.World.Records.Characters;
using Symbioz.World.Records.Maps;
using Symbioz.World.Records.Breeds;
using Symbioz.World.Records.Items;
using Symbioz.World.Records.Interactives;
using Symbioz.World.Records.Npcs;
using Symbioz.World.Models.Entities.Jobs;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Models.Maps;
using Symbioz.World.Providers.Maps.Interactives;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Entities.HumanOptions;
using Symbioz.World.Providers.Parties;
using Symbioz.World.Providers.Arena;
using Symbioz.World.Handlers.Approach;
using Symbioz.World.Records.Spells;
using Symbioz.World.Models.Parties;
using Symbioz.World.Records.Guilds;
using Symbioz.World.Providers.Maps.Cinematics;
using Symbioz.World.Models.Entities.Shortcuts;
using Symbioz.World.Models.Guilds;
using Symbioz.World.Providers.Guilds;
using Symbioz.World.Models.Fights;
using Symbioz.World.Modules;

namespace Symbioz.World.Models.Entities
{
    /// <summary>
    /// Représente un personnage joueur en jeu.
    /// C'est la classe centrale du serveur : elle hérite de Entity (visible sur la map)
    /// et regroupe toutes les données et actions d'un joueur connecté :
    /// - Inventaire, kamas, statistiques
    /// - Sorts, raccourcis, emotes, titres, ornements
    /// - Déplacements sur la map
    /// - Combat (Fighter), arène, groupe (Party), guilde (Guild)
    /// - Dialogues avec les PNJ, échanges, craft
    /// - Connexion/déconnexion propre
    /// Le Character est créé à la sélection du personnage et détruit à la déconnexion.
    /// </summary>
    public class Character : Entity
    {
        /// <summary>
        /// Le client réseau associé à ce personnage (connexion socket du joueur).
        /// Permet d'envoyer des messages directement à ce joueur.
        /// </summary>
        public WorldClient Client
        {
            get;
            set;
        }

        /// <summary>
        /// Les données persistantes du personnage en base de données
        /// (nom, stats, expérience, sorts, inventaire, position...).
        /// </summary>
        public CharacterRecord Record
        {
            get;
            private set;
        }

        /// <summary>
        /// Status du personnage
        /// Valeur par défaut ajouter car données pas stocké en BDD pour le moment
        /// </summary>
        public PlayerStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// Multiplicateur d'XP basé sur le nombre de personnages du compte
        /// qui sont de niveau supérieur à ce personnage.
        /// Plus vous avez de personnages haut niveau, plus ce personnage gagne d'XP vite.
        /// </summary>
        public ushort ExpMultiplicator
        {
            get
            {
                return (ushort)Client.Characters.Count(x => ExperienceRecord.GetCharacterLevel(x.Exp) > Level);
            }
        }

        /// <summary>
        /// La race (classe) du personnage (Iop, Crâ, Feca...) chargée depuis les records.
        /// </summary>
        public BreedRecord Breed
        {
            get
            {
                return BreedRecord.GetBreed(Record.BreedId);
            }
        }

        /// <summary>
        /// Indique si le personnage est actuellement monté sur une monture.
        /// </summary>
        public bool Riding
        {
            get
            {
                return Look.IsRiding;
            }
        }

        /// <summary>
        /// Indique si le personnage est en train de changer de map.
        /// Bloque les actions pendant la transition entre deux maps.
        /// </summary>
        public bool ChangeMap
        {
            get;
            set;
        }

        /// <summary>
        /// Barre de raccourcis des sorts du personnage.
        /// </summary>
        public SpellShortcutBar SpellShortcutBar
        {
            get;
            private set;
        }
        /// <summary>
        /// Barre de raccourcis généraux (items, emotes...) du personnage.
        /// </summary>
        public GeneralShortcutBar GeneralShortcutBar
        {
            get;
            private set;
        }
        /// <summary>
        /// Indique si le personnage est en train de récolter une ressource.
        /// </summary>
        public bool Collecting
        {
            get;
            set;
        }
        /// <summary>
        /// Le combattant actif contrôlé par le joueur (peut être un Pokéfus / mination).
        /// </summary>
        public PlayableFighter Fighter
        {
            get;
            private set;
        }
        /// <summary>
        /// Le combattant principal du joueur (toujours le personnage lui-même, pas un Pokéfus).
        /// </summary>
        public CharacterFighter FighterMaster
        {
            get;
            private set;
        }
        /// <summary>
        /// Indique si le personnage est actuellement en combat.
        /// </summary>
        public bool Fighting
        {
            get
            {
                return Fighter != null;
            }
        }
        /// <summary>
        /// Nombre total de combattants contrôlés par le joueur (personnage + Pokéfus équipés).
        /// </summary>
        public int FighterCount
        {
            get
            {
                return MinationCount() + 1; // + companion?
            }
        }
        /// <summary>
        /// Indique si le personnage est inscrit en arène PvP.
        /// </summary>
        public bool InArena
        {
            get
            {
                return ArenaMember != null;
            }
        }
        /// <summary>
        /// Instance du membre d'arène associé à ce personnage (null si pas en arène).
        /// </summary>
        public ArenaMember ArenaMember
        {
            get;
            private set;
        }
        /// <summary>
        /// Last Map before enter arena
        /// </summary>
        public int? PreviousRoleplayMapId
        {
            get;
            set;
        }
        /// <summary>
        /// Indique si le personnage peut s'inscrire en arène (doit être en roleplay et pas déjà inscrit).
        /// </summary>
        public bool CanRegisterArena
        {
            get
            {
                return InRoleplay && !InArena;
            }
        }
        /// <summary>
        /// Indique si le personnage appartient à une guilde.
        /// </summary>
        public bool HasGuild
        {
            get
            {
                return Record.GuildId != 0;
            }
        }
        /// <summary>
        /// L'instance de guilde à laquelle appartient le personnage.
        /// </summary>
        public GuildInstance Guild
        {
            get;
            set;
        }
        /// <summary>
        /// L'instance du membre de guilde correspondant à ce personnage.
        /// </summary>
        public GuildMemberInstance GuildMember
        {
            get
            {
                return Guild.GetMember(Id);
            }
        }
        /// <summary>
        /// L'inventaire du personnage (items, kamas, monture...).
        /// </summary>
        public Inventory Inventory
        {
            get;
            private set;
        }
        /// <summary>
        /// Le dialogue actif du personnage (PNJ, échange, banque...). Null si aucun dialogue ouvert.
        /// </summary>
        public Dialog Dialog
        {
            get;
            set;
        }
        /// <summary>
        /// La boîte de requête active (invitation de groupe, défi...). Null si aucune requête en cours.
        /// </summary>
        public RequestBox RequestBox
        {
            get;
            set;
        }

        /// <summary>Action exécutée une seule fois lors du prochain chargement de map (après un téléport).</summary>
        public Action OnNextEnterMap
        {
            get;
            set;
        }


        /// <summary>
        /// Liste des IDs de compétences de métier autorisées pour ce personnage selon son niveau de métier.
        /// </summary>
        public ushort[] SkillsAllowed
        {
            get;
            private set;
        }
        /// <summary>
        /// L'ornement actuellement actif sur le personnage (affiché sous le nom).
        /// </summary>
        private CharacterHumanOptionOrnament ActiveOrnament
        {
            get
            {
                return GetFirstHumanOption<CharacterHumanOptionOrnament>();
            }
        }
        /// <summary>
        /// Le titre actuellement actif sur le personnage (affiché au-dessus du nom).
        /// </summary>
        private CharacterHumanOptionTitle ActiveTitle
        {
            get
            {
                return GetFirstHumanOption<CharacterHumanOptionTitle>();
            }
        }

        /// <summary>
        /// Le groupe auquel appartient le personnage. Null si pas en groupe.
        /// </summary>
        public AbstractParty Party
        {
            get;
            set;
        }

        /// <summary>
        /// Retourne true si le personnage est dans un groupe actif.
        /// </summary>
        public bool HasParty()
        {
            if (this.Party != null && this.Party.Members.Contains(this))
                return true;
            return false;
        }
        /// <summary>
        /// Indique si le personnage a bloqué les invitations de groupe provenant d'autres groupes.
        /// </summary>
        public bool HadBlockOtherPartiesInvitations
        {
            get;
            set;
        }

        /// <summary>
        /// Liste des groupes dont le personnage a reçu une invitation en attente.
        /// </summary>
        public List<AbstractParty> GuestedParties
        {
            get;
            set;
        }

        /// <summary>
        /// Indique si le personnage est actuellement muet (ne peut pas parler en chat).
        /// </summary>
        public bool IsMute
        {
            get;
            set;
        }

        /// <summary>
        /// Retourne la boîte de requête active castée dans le type T.
        /// </summary>
        public T GetRequestBox<T>() where T : RequestBox
        {
            return (T)RequestBox;
        }
        /// <summary>
        /// Retourne le dialogue actif casté dans le type T.
        /// </summary>
        public T GetDialog<T>() where T : Dialog
        {
            return (T)Dialog;
        }
        /// <summary>
        /// Ouvre un dialogue pour le personnage. Si le personnage est occupé et force=false, affiche une erreur.
        /// </summary>
        public void OpenDialog(Dialog dialog, bool force = false)
        {
            if (!Busy || force)
            {
                try
                {
                    this.Dialog = dialog;
                    this.Dialog.Open();
                }
                catch
                {
                    ReplyError("Impossible d'éxecuter l'action.");
                    LeaveDialog();
                }
            }
            else
            {
                ReplyError("Unable to open dialog while busy...");
            }
        }
        /// <summary>
        /// Change le combattant actif (ex: lors de la prise de contrôle d'un Pokéfus).
        /// </summary>
        public void SwapFighter(PlayableFighter newFighter)
        {
            this.Fighter = newFighter;
        }
        /// <summary>
        /// Remet le combattant actif sur le personnage principal (annule le contrôle d'un Pokéfus).
        /// </summary>
        public void SwapFighterToMaster()
        {
            this.Fighter = FighterMaster;
        }
        /// <summary>
        /// Crée le combattant du personnage pour un combat. Retire le personnage de la map,
        /// crée le contexte de combat, envoie les messages de démarrage et gère les Pokéfus.
        /// </summary>
        public PlayableFighter CreateFighter(FightTeam team)
        {
            if (Look.RemoveAura())
                RefreshActorOnMap();

            this.MovementKeys = null;
            this.IsMoving = false;
            this.Map.Instance.RemoveEntity(this);
            this.DestroyContext();
            this.CreateContext(GameContextEnum.FIGHT);
            this.RefreshStats();
            this.Client.Send(new GameFightStartingMessage((sbyte)team.Fight.FightType, team.Fight.BlueTeam.Id, team.Fight.RedTeam.Id));
            this.FighterMaster = new CharacterFighter(this, team, CellId);
            this.Fighter = FighterMaster;

            if (team.Fight.MinationAllowed)
                this.ApplyMination(this.FighterMaster, team);

            return Fighter;
        }
        /// <summary>
        /// Ajoute les combattants Pokéfus (minations) de l'inventaire équipé dans l'équipe de combat.
        /// </summary>
        private void ApplyMination(CharacterFighter master, FightTeam team)
        {
            CharacterItemRecord[] items = Inventory.GetEquipedMinationItems();

            foreach (var item in items)
            {
                EffectMination effect = item.FirstEffect<EffectMination>();
                EffectMinationLevel effectLevel = item.FirstEffect<EffectMinationLevel>();

                if (effectLevel == null) // to remove (axiom)
                {
                    effectLevel = new EffectMinationLevel(1, 0, 0);
                    item.AddEffect(effectLevel);
                }
                var fighter = new MinationMonsterFighter(team, MonsterRecord.GetMonster(effect.MonsterId),
                      effect.GradeId, effectLevel.Level, master, team.GetPlacementCell());

                team.AddFighter(fighter);
                fighter.SetLife(effectLevel.Level * 20, true);

            }
        }
        /// <summary>
        /// Retourne le nombre de Pokéfus (minations) actuellement équipés.
        /// </summary>
        private int MinationCount()
        {
            return Array.FindAll(Inventory.GetEquipedItems(), x => x.HasEffect<EffectMination>()).Count();
        }
        /// <summary>
        /// Vérifie si le personnage peut défier un autre joueur en combat.
        /// Retourne la raison du refus ou FIGHTER_ACCEPTED si le défi est possible.
        /// </summary>
        public FighterRefusedReasonEnum CanRequestFight(Character target)
        {
            FighterRefusedReasonEnum result;
            if (target.Fighting || target.Busy)
            {
                result = FighterRefusedReasonEnum.OPPONENT_OCCUPIED;
            }
            else
            {
                if (this.Fighting || this.Busy)
                {
                    result = FighterRefusedReasonEnum.IM_OCCUPIED;
                }
                else
                {
                    if (target == this)
                    {
                        result = FighterRefusedReasonEnum.FIGHT_MYSELF;
                    }
                    else
                    {
                        if (this.ChangeMap || target.ChangeMap || target.Map != Map || !Map.Position.AllowFightChallenges || !Map.ValidForFight)
                        {
                            result = FighterRefusedReasonEnum.WRONG_MAP;
                        }
                        else
                        {
                            result = FighterRefusedReasonEnum.FIGHTER_ACCEPTED;
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Vérifie si le personnage peut agresser un autre joueur.
        /// Interdit le multi-compte, vérifie l'alignement, la map, et la différence de niveau max (20 niveaux).
        /// </summary>
        public FighterRefusedReasonEnum CanAgress(Character target)
        {
            if (target.Client.Ip == this.Client.Ip && Client.Account.Role <= ServerRoleEnum.Animator)
            {
                return FighterRefusedReasonEnum.MULTIACCOUNT_NOT_ALLOWED;
            }
            if (target.Busy)
            {
                return FighterRefusedReasonEnum.OPPONENT_OCCUPIED;
            }
            if (target == this)
            {
                return FighterRefusedReasonEnum.FIGHT_MYSELF;
            }
            if (this.Level - target.Level > 20)
            {
                return FighterRefusedReasonEnum.INSUFFICIENT_RIGHTS;
            }
            if (!Map.Position.AllowAggression)
            {
                return FighterRefusedReasonEnum.WRONG_MAP;
            }
            if (target.Record.Alignment.Side == this.Record.Alignment.Side)
            {
                return FighterRefusedReasonEnum.WRONG_ALIGNMENT;
            }
            if (Busy)
            {
                return FighterRefusedReasonEnum.IM_OCCUPIED;
            }
            if (!InRoleplay)
            {
                return FighterRefusedReasonEnum.TOO_LATE;
            }

            return FighterRefusedReasonEnum.FIGHTER_ACCEPTED;
        }
        /// <summary>
        /// Calcule l'XP à donner en récompense, basé sur un pourcentage de l'XP nécessaire pour le prochain niveau.
        /// </summary>
        public long GetRewardExperienceFromPercentage(int percentage)
        {
            long result = (long)(UpperBoundExperience * (percentage / 100d));
            result = result / Level;
            return result;
        }
        /// <summary>
        /// Vérifie si le personnage peut utiliser cet almanach aujourd'hui (pas déjà utilisé ce jour).
        /// </summary>
        public bool CanAlmanach(AlmanachRecord almanach)
        {
            return Record.LastAlmanachDay != almanach.Id;
        }
        /// <summary>
        /// Effectue l'échange d'almanach : retire l'item requis, donne la récompense et l'XP.
        /// Retourne true si l'échange a réussi, false si le joueur n'a pas l'item nécessaire.
        /// </summary>
        public bool DoAlmanach(AlmanachRecord almanach)
        {
            if (this.Inventory.Exist(almanach.ItemGId, almanach.Quantity))
            {

                this.Inventory.RemoveItem(this.Inventory.GetFirstItem(almanach.ItemGId, almanach.Quantity), almanach.Quantity);
                this.OnItemLost(almanach.ItemGId, almanach.Quantity);

                this.Inventory.AddItem((ushort)almanach.RewardItemGId, (uint)almanach.RewardItemQuantity);
                this.OnItemGained((ushort)almanach.RewardItemGId, (uint)almanach.RewardItemQuantity);
                long xp = GetRewardExperienceFromPercentage(almanach.XpRewardPercentage);

                if (Level < 200)
                {
                    this.AddExperience((ulong)xp);
                    this.OnExperienceGained(xp);
                    this.RefreshStats();
                }
                this.Record.LastAlmanachDay = almanach.Id;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie si le personnage est dans un dialogue du type spécifié.
        /// </summary>
        public bool IsInDialog(DialogTypeEnum type)
        {
            if (Dialog == null)
                return false;
            return Dialog.DialogType == type;
        }
        /// <summary>
        /// Vérifie si le personnage est dans un échange du type spécifié.
        /// </summary>
        public bool IsInExchange(ExchangeTypeEnum type)
        {
            var exchange = GetDialog<Exchange>();
            if (exchange != null)
                return exchange.ExchangeType == type;
            else
                return false;
        }
        /// <summary>
        /// Accepte la requête en attente si ce personnage en est la cible.
        /// </summary>
        public void AcceptRequest()
        {
            if (this.IsInRequest() && this.RequestBox.Target == this)
            {
                this.RequestBox.Accept();
            }
        }
        /// <summary>
        /// Refuse la requête en attente si ce personnage en est la cible.
        /// </summary>
        public void DenyRequest()
        {
            if (this.IsInRequest() && this.RequestBox.Target == this)
            {
                this.RequestBox.Deny();
            }
        }
        /// <summary>
        /// Annule la requête en cours (si source: annule, si cible: refuse).
        /// </summary>
        public void CancelRequest()
        {
            if (this.IsInRequest())
            {
                if (this.IsRequestSource())
                {
                    this.RequestBox.Cancel();
                }
                else
                {
                    if (this.IsRequestTarget())
                    {
                        this.DenyRequest();
                    }
                }
            }
        }
        /// <summary>Indique si une requête est en cours pour ce personnage.</summary>
        public bool IsInRequest()
        {
            return this.RequestBox != null;
        }
        /// <summary>Indique si ce personnage est l'initiateur de la requête en cours.</summary>
        public bool IsRequestSource()
        {
            return this.IsInRequest() && this.RequestBox.Source == this;
        }
        /// <summary>Indique si ce personnage est la cible de la requête en cours.</summary>
        public bool IsRequestTarget()
        {
            return this.IsInRequest() && this.RequestBox.Target == this;
        }
        /// <summary>
        /// Indique si le personnage est occupé (dialogue ouvert, requête en cours, changement de map ou interaction bloquée).
        /// </summary>
        public bool Busy
        {
            get
            {
                return Dialog != null || RequestBox != null || ChangeMap || !CanInteract;
            }
        }
        /// <summary>
        /// Indique si le personnage peut interagir avec le monde (false lors de cinématiques ou blocages spéciaux).
        /// </summary>
        public bool CanInteract
        {
            get;
            set;
        }
        /// <summary>Identifiant unique du personnage, provenant du record en base.</summary>
        public override long Id
        {
            get
            {
                return Record.Id;
            }
        }

        /// <summary>Nom du personnage, provenant du record en base.</summary>
        public override string Name
        {
            get
            {
                return Record.Name;
            }
        }
        /// <summary>Apparence visuelle du personnage (skin, couleurs, auras...).</summary>
        public override ContextActorLook Look
        {
            get
            {
                return Record.Look;
            }
            set
            {
                Record.Look = value;
            }
        }

        // Valeur interne du niveau, mise à jour via la propriété Level
        private ushort m_level;



        /// <summary>
        /// Niveau actuel du personnage. Le setter met automatiquement à jour les bornes d'XP (inférieure et supérieure).
        /// </summary>
        public ushort Level
        {
            get
            {
                return m_level;
            }

            private set
            {
                this.m_level = value;
                this.LowerBoundExperience = ExperienceRecord.GetExperienceForLevel(this.Level).Player;
                this.UpperBoundExperience = ExperienceRecord.GetExperienceForNextLevel(this.Level).Player;
            }
        }

        /// <summary>
        /// XP actuelle du personnage. Le setter détecte automatiquement les montées/descentes de niveau.
        /// </summary>
        public ulong Experience
        {
            get
            {
                return this.Record.Exp;
            }
            private set
            {
                this.Record.Exp = value;

                if (value >= this.UpperBoundExperience && this.Level < ExperienceRecord.MaxCharacterLevel || value < this.LowerBoundExperience)
                {
                    ushort level = this.Level;
                    this.Level = ExperienceRecord.GetCharacterLevel(this.Record.Exp);
                    int difference = (int)(this.Level - level);
                    this.OnLevelChanged(level, difference, true);
                }
            }
        }
        /// <summary>
        /// Envoie les statistiques actualisées au client (rafraîchit l'interface de stats du joueur).
        /// </summary>
        public void RefreshStats()
        {
            Client.Send(new CharacterStatsListMessage(Record.Stats.GetCharacterCharacteristics(this)));
        }
        /// <summary>
        /// Remet tous les points de stats à zéro et les redistribue. Si addStatPoints=true, redonne les points.
        /// </summary>
        public void Restat(bool addStatPoints = true)
        {
            this.Record.Restat(addStatPoints);
            this.RefreshStats();
        }
        /// <summary>
        /// Change la direction du personnage et envoie la mise à jour à tous les joueurs sur la map.
        /// </summary>
        public void SetDirection(DirectionsEnum direction)
        {
            Record.Direction = (sbyte)direction;
            SendMap(new GameMapChangeOrientationMessage(new ActorOrientation(Id, (sbyte)direction)));
        }
        /// <summary>
        /// Ajoute un suiveur visuel au personnage (ex: familier affiché sur la map).
        /// </summary>
        public void AddFollower(ContextActorLook look)
        {
            CharacterHumanOptionFollowers followers = GetFirstHumanOption<CharacterHumanOptionFollowers>();

            if (followers != null)
            {
                followers.AddFollower(look);
            }
            else
            {
                AddHumanOption(new CharacterHumanOptionFollowers(look));
            }
        }
        /// <summary>
        /// Retire un suiveur visuel du personnage.
        /// </summary>
        public void RemoveFollower(ContextActorLook look)
        {
            CharacterHumanOptionFollowers followers = GetFirstHumanOption<CharacterHumanOptionFollowers>();

            if (followers == null)
            {
                new Logger().Error("Error while removing follower!");
                return;
            }
            else
            {
                followers.RemoveFollower(look);

                if (followers.Looks.Count == 0)
                    RemoveHumanOption<CharacterHumanOptionFollowers>();
            }
        }

        /// <summary>
        /// Ajoute de l'XP au personnage. Peut déclencher une montée de niveau.
        /// </summary>
        public void AddExperience(ulong amount)
        {
            Experience += amount;
        }

        /// <summary>
        /// Fixe le niveau du personnage en ajustant son XP à la valeur exacte pour ce niveau.
        /// </summary>
        public void SetLevel(ushort newLevel)
        {
            if (newLevel > ExperienceRecord.MaxCharacterLevel)
            {
                Reply("New level must be < " + ExperienceRecord.MaxCharacterLevel);
            }
            else
            {
                Experience = ExperienceRecord.GetExperienceForLevel(newLevel).Player;
            }
        }
        /// <summary>
        /// Appelé lors d'un changement de niveau (montée ou descente).
        /// Gère les sorts, les PV, les points de stats, les ornements de niveau et le groupe.
        /// Si send=true, envoie les messages de niveau et rafraîchit la map.
        /// </summary>
        private void OnLevelChanged(ushort oldLevel, int amount, bool send)
        {
            if (send && Level > oldLevel)
            {
                this.SendMap(new CharacterLevelUpInformationMessage((byte)this.Level, Record.Name, (uint)Id));
                Client.Send(new CharacterLevelUpMessage((byte)this.Level));

            }
            CheckSpells();

            if (Level > oldLevel)
            {
                Record.Stats.LifePoints += (5 * amount);
                Record.Stats.MaxLifePoints += (5 * amount);
                Record.Stats.Energy += (ushort) (100 * amount);
                Record.Stats.MaxEnergyPoints += (ushort) (100 * amount);
                Record.SpellPoints += (ushort)(amount);
                Record.StatsPoints += (ushort)(5 * amount);

            }
            else if (Level < oldLevel)
            {
                Record.Stats.LifePoints += (5 * amount);
                Record.Stats.MaxLifePoints += (5 * amount);
                Record.Stats.Energy -= (ushort)(100 * Math.Abs(amount));
                Record.Stats.MaxEnergyPoints -= (ushort) (100 * Math.Abs(amount));
                Record.StatsPoints = (ushort)(Level * 5 - 5);
                CheckRemovedSpells();
                Inventory.UnequipAll();
            }

            if (oldLevel < 100 && this.Level >= 100)
            {
                LearnEmote((byte)EmotesEnum.PowerAura);
                Record.Stats.ActionPoints.Base += 1;
                LearnOrnament((ushort)OrnamentsEnum.Hundred, send);
            }
            if (oldLevel < 160 && this.Level >= 160)
            {
                LearnOrnament((ushort)OrnamentsEnum.HundredSixty, send);
            }
            if (oldLevel < 200 && this.Level == 200)
            {
                LearnOrnament((ushort)OrnamentsEnum.TwoHundred, send);
            }

            if (HasParty())
            {
                Party.UpdateMember(this);
            }
            if (send)
            {

                RefreshActorOnMap();
                RefreshStats();
            }
        }
        /// <summary>XP minimale pour le niveau actuel (borne inférieure).</summary>
        public ulong LowerBoundExperience
        {
            get;
            private set;
        }
        /// <summary>XP nécessaire pour passer au niveau suivant (borne supérieure).</summary>
        public ulong UpperBoundExperience
        {
            get;
            private set;
        }

        // Indique si le personnage vient d'être créé (pour déclencher la cinématique de début)
        private bool New
        {
            get;
            set;
        }

        // Contexte de jeu actuel en interne (nullable)
        private GameContextEnum? m_context
        {
            get; set;
        }

        /// <summary>
        /// Contexte de jeu actuel du personnage (ROLE_PLAY, FIGHT...). Null si aucun contexte créé.
        /// </summary>
        public GameContextEnum? Context
        {
            get
            {
                return m_context;
            }
        }

        /// <summary>
        /// Indique si le personnage est en contexte roleplay (sur la map, hors combat).
        /// </summary>
        public bool InRoleplay
        {
            get
            {
                return Context.HasValue && Context.Value == GameContextEnum.ROLE_PLAY;
            }
        }

        /// <summary>
        /// La cellule de destination lors d'un déplacement en cours sur la map.
        /// </summary>
        public ushort MovedCell
        {
            get;
            set;
        }

        /// <summary>
        /// L'ID de la sous-zone (subarea) de la map actuelle du personnage.
        /// </summary>
        public ushort SubareaId
        {
            get
            {
                if (Map != null)
                {
                    return Map.SubAreaId;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>Identifiant de la cellule actuelle du personnage sur la map.</summary>
        public override ushort CellId
        {
            get
            {
                return Record.CellId;
            }
            set
            {
                Record.CellId = value;
            }
        }

        /// <summary>Direction actuelle du personnage (N, NE, E, SE...).</summary>
        public override DirectionsEnum Direction
        {
            get
            {
                return (DirectionsEnum)Record.Direction;
            }
            set
            {
                Record.Direction = (sbyte)value;
            }
        }
        /// <summary>
        /// Indique si le personnage est en train de se déplacer sur la map.
        /// </summary>
        public bool IsMoving
        {
            get;
            private set;
        }

        /// <summary>
        /// Les clés de déplacement encodées envoyées au client pour animer le mouvement.
        /// </summary>
        public short[] MovementKeys
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructeur : initialise le personnage à partir du client réseau et du record en base.
        /// Charge l'inventaire, les barres de raccourcis, les compétences et le niveau.
        /// Si isNew=true, applique les bonus de départ selon le niveau initial.
        /// </summary>
        public Character(WorldClient client, CharacterRecord record, bool isNew)
        {
            this.Record = record;
            this.Client = client;
            this.New = isNew;
            this.GuestedParties = new List<AbstractParty>();
            this.Level = ExperienceRecord.GetCharacterLevel(record.Exp);
            this.Inventory = new Inventory(this);
            this.SpellShortcutBar = new SpellShortcutBar(this);
            this.GeneralShortcutBar = new GeneralShortcutBar(this);
            this.SkillsAllowed = SkillsProvider.Instance.GetAllowedSkills(this).ToArray();
            this.Party = null;
            this.CanInteract = true;

            if (isNew)
            {
                OnLevelChanged(1, Level - 1, false);
            }

        }
        /// <summary>
        /// Rafraîchit l'apparence du personnage pour tous les joueurs présents sur la même map.
        /// </summary>
        public void RefreshActorOnMap()
        {
            SendMap(new GameRolePlayShowActorMessage(GetActorInformations()));
        }
        /// <summary>
        /// Rafraîchit l'apparence du personnage uniquement pour lui-même.
        /// </summary>
        public void RefreshActor()
        {
            Client.Send(new GameRolePlayShowActorMessage(GetActorInformations()));
        }
        /// <summary>
        /// Vérifie les sorts disponibles selon le niveau et les ajoute si manquants. Rafraîchit la liste de sorts.
        /// </summary>
        public void CheckSpells()
        {
            foreach (var spell in Breed.GetSpellsForLevel(this.Level, Record.Spells))
            {
                LearnSpell(spell);
            }
            RefreshSpells();
        }
        /// <summary>
        /// Retire les sorts qui ne sont plus disponibles au niveau actuel (utilisé lors d'une perte de niveau).
        /// </summary>
        public void CheckRemovedSpells()
        {
            var spells2 = Breed.GetSpellsForLevel(200, new List<CharacterSpell>());
            var spells = Breed.GetSpellsForLevel(this.Level, new List<CharacterSpell>());


            foreach (var spell in Record.Spells.ToArray())
            {
                if (!spells.Contains(spell.SpellId) && spells2.Contains(spell.SpellId))
                {
                    RemoveSpell(spell.SpellId);
                }
            }

        }
        /// <summary>Vérifie si le personnage connaît le sort donné.</summary>
        public bool HasSpell(ushort spellId)
        {
            return Record.Spells.Find(x => x.SpellId == spellId) != null;
        }
        /// <summary>
        /// Apprend un sort au personnage, l'ajoute à la barre de raccourcis si possible et envoie une notification.
        /// </summary>
        public void LearnSpell(ushort spellId)
        {
            if (!HasSpell(spellId))
            {
                Record.Spells.Add(new CharacterSpell(spellId, 1));
                if (SpellShortcutBar.CanAdd())
                {
                    SpellShortcutBar.Add(spellId);
                    RefreshShortcuts();
                }
                RefreshSpells();

                TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 3, spellId);
            }
        }
        /// <summary>Retire un sort de la liste du personnage et de la barre de raccourcis.</summary>
        public void RemoveSpell(ushort spellId)
        {
            if (HasSpell(spellId))
            {
                Record.Spells.RemoveAll(x => x.SpellId == spellId);
                SpellShortcutBar.Remove(spellId);
                RefreshSpells();
                RefreshShortcuts();
            }
        }
        /// <summary>Retourne le sort du personnage par son ID, ou null s'il ne le connaît pas.</summary>
        public CharacterSpell GetSpell(ushort spellId)
        {
            return Record.Spells.Find(x => x.SpellId == spellId);
        }

        /// <summary>
        /// Monte ou descend le grade d'un sort en échangeant des points de sort.
        /// Envoie un message de succès ou d'échec selon la disponibilité des points.
        /// </summary>
        public void ModifySpell(ushort spellId, sbyte gradeId)
        {
            if (!Fighting)
            {
                CharacterSpell actualSpell = GetSpell(spellId);

                if (actualSpell == null)
                {
                    Client.Send(new SpellModifyFailureMessage());
                    return;
                }

                int cost = actualSpell.Grade < gradeId ? CharacterSpell.GetBoostCost(actualSpell.Grade, gradeId)
                    : CharacterSpell.GetBoostCost(gradeId, actualSpell.Grade);

                if (actualSpell.Grade < gradeId)
                {
                    if (cost <= Record.SpellPoints)
                    {
                        Record.SpellPoints -= (ushort)cost;
                    }
                    else
                    {
                        Client.Send(new SpellModifyFailureMessage());
                        return;
                    }
                }
                else
                {
                    if (actualSpell.Grade > gradeId)
                    {
                        Record.SpellPoints += (ushort)cost;
                    }
                    else
                    {
                        Client.Send(new SpellModifyFailureMessage());
                    }
                }

                actualSpell.SetGrade(gradeId);
                RefreshStats();
                Client.Send(new SpellModifySuccessMessage(spellId, gradeId));
            }
            else
            {
                Client.Send(new SpellModifyFailureMessage());
            }


        }

        /// <summary>Retourne la barre de raccourcis correspondant à l'enum (générale ou sorts).</summary>
        public ShortcutBar GetShortcutBar(ShortcutBarEnum barEnum)
        {
            switch (barEnum)
            {
                case ShortcutBarEnum.GENERAL_SHORTCUT_BAR:
                    return GeneralShortcutBar;
                case ShortcutBarEnum.SPELL_SHORTCUT_BAR:
                    return SpellShortcutBar;
            }

            throw new Exception("Unknown shortcut bar, " + barEnum);
        }
        /// <summary>Ajoute des points de sort et rafraîchit les stats.</summary>
        public void AddSpellPoints(ushort amount)
        {
            Record.SpellPoints += amount;
            RefreshStats();
        }
        /// <summary>Rafraîchit les deux barres de raccourcis (sorts et générale) côté client.</summary>
        public void RefreshShortcuts()
        {
            SpellShortcutBar.Refresh();
            GeneralShortcutBar.Refresh();

        }
        /// <summary>Envoie la liste complète des sorts au client.</summary>
        public void RefreshSpells()
        {
            Client.Send(new SpellListMessage(true, Record.Spells.ConvertAll<SpellItem>(x => x.GetSpellItem()).ToArray()));
        }
        /// <summary>Affiche un message système indiquant qu'un item a été obtenu.</summary>
        public void OnItemGained(ushort gid, uint quantity)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 21, new object[] { quantity, gid });
        }
        /// <summary>Affiche un message système indiquant l'XP gagnée.</summary>
        public void OnExperienceGained(long experience)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 8, new object[] { experience });
        }
        /// <summary>Affiche un message système indiquant qu'un item a été perdu/retiré.</summary>
        public void OnItemLost(ushort gid, uint quantity)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 22, new object[] { quantity, gid });
        }
        /// <summary>Affiche un message système indiquant qu'un item a été vendu et le prix reçu.</summary>
        public void OnItemSelled(ushort gid, uint quantity, uint price)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 65, new object[] { price, string.Empty, gid, quantity });
        }
        /// <summary>Affiche un message système indiquant des kamas gagnés.</summary>
        public void OnKamasGained(int amount)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 45, new object[] { amount });
        }
        /// <summary>Affiche un message système indiquant des kamas perdus.</summary>
        public void OnKamasLost(int amount)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 46, new object[] { amount });
        }
        /// <summary>
        /// Joue une émote. Si c'est une aura, l'ajoute au look du personnage. Sinon envoie l'animation à la map.
        /// </summary>
        public void PlayEmote(byte id)
        {
            EmoteRecord template = EmoteRecord.GetEmote(id);

            if (!Collecting && !ChangeMap)
            {
                if (Look.RemoveAura())
                    RefreshActorOnMap();

                if (template.IsAura)
                {
                    ushort bonesId = template.AuraBones;

                    if (template.Id == (byte)EmotesEnum.PowerAura)
                        bonesId = (ushort)(this.Level >= 100 && this.Level != 200 ? 169 : 170);

                    this.Look.AddAura(bonesId);
                    this.RefreshActorOnMap();
                }
                else
                {
                    this.SendMap(new EmotePlayMessage(id, 0, this.Id, this.Client.Account.Id));
                }
            }

        }
        /// <summary>Joue une émote de dialogue aléatoire parmi les émotes de parole disponibles.</summary>
        public void RandomTalkEmote()
        {
            byte[] talkEmotes = new byte[] { 49, 66, 9, 2, 10, 88 };
            PlayEmote(talkEmotes.Random());
        }
        /// <summary>Apprend une émote au personnage. Retourne true si réussi, false si déjà connue.</summary>
        public bool LearnEmote(byte id)
        {
            if (!Record.KnownEmotes.Contains(id))
            {
                Record.KnownEmotes.Add(id);
                Client.Send(new EmoteAddMessage(id));
                return true;
            }
            else
            {
                this.Reply("Vous connaissez déja cette émote.");
                return false;
            }
        }
        /// <summary>Vérifie si le personnage a atteint l'objectif donné.</summary>
        public bool HasReachObjective(short id)
        {
            return Record.DoneObjectives.Contains(id);
        }
        /// <summary>Marque un objectif comme atteint et notifie le joueur.</summary>
        public void ReachObjective(short id)
        {
            if (!Record.DoneObjectives.Contains(id))
            {
                Record.DoneObjectives.Add(id);
                this.Reply("Nouvel objectif atteint.");
            }
        }
        /// <summary>Appelé à la fin d'un combat. Gère le retour en map selon le type de combat (arène, PvM, etc.).</summary>
        public bool OnFightEnded(bool winner, FightTypeEnum type, int avgMonsterLevel)
        {
            this.Inventory.DecrementEtherals();

            if (type == FightTypeEnum.FIGHT_TYPE_PVP_ARENA)
            {
                this.Teleport(PreviousRoleplayMapId.Value);
                PreviousRoleplayMapId = null;
                return true;
            }
            else if (!winner && type == FightTypeEnum.FIGHT_TYPE_PvM)
            {
                if (Record.Stats.Energy - avgMonsterLevel * 10 <= 0)
                {
                    Record.Stats.Energy = 0;
                }
                else
                {
                    Record.Stats.Energy = (ushort)(Record.Stats.Energy - avgMonsterLevel * 10);
                }
            }
            else if (winner && type == FightTypeEnum.FIGHT_TYPE_PvM)
            {
                EndFightActionRecord endFightAction = EndFightActionRecord.GetEndFightAction(Map.Id);
                if (endFightAction != null)
                {
                    this.Teleport(endFightAction.TeleportMapId, endFightAction.TeleportCellId);
                    return true;
                }
            }
            return false;
        }
        /// <summary>Retire une émote du personnage. Retourne true 
        ///  si réussi, false si non possédée.</summary>
        public bool RemoveEmote(byte id)
        {
            if (Record.KnownEmotes.Contains(id))
            {
                Record.KnownEmotes.Remove(id);
                Client.Send(new EmoteRemoveMessage(id));
                return true;
            }
            else
            {
                this.Reply("Impossible de retirer l'émote.");
                return false;
            }
        }
        /// <summary>Envoie la liste complète des émotes connues au client.</summary>
        public void RefreshEmotes()
        {
            Client.Send(new EmoteListMessage(Record.KnownEmotes.ToArray()));
        }
        /// <summary>Appelé quand le personnage arrive sur une nouvelle map. Initialise l'entité, envoie les données de map et exécute le callback OnNextEnterMap si défini.</summary>
        public void OnEnterMap()
        {
            this.ChangeMap = false;
            if(OnNextEnterMap != null)
            {
                OnNextEnterMap();
                OnNextEnterMap = null;
            }
            this.UpdateServerExperience(Map.SubArea.ExperienceRate);

            if (this.Busy)
                this.LeaveDialog();

            if (!Fighting) // Teleport + Fight
            {
                lock (this.Map.Instance)
                {
                    this.Map.Instance.AddEntity(this);

                    this.Map.Instance.MapComplementary(Client);
                    this.Map.Instance.MapFightCount(Client);

                    foreach (Character current in this.Map.Instance.GetEntities<Character>())
                    {
                        if (current.IsMoving)
                        {
                            Client.Send(new GameMapMovementMessage(current.MovementKeys, current.Id));
                            Client.Send(new BasicNoOperationMessage());
                        }
                    }

                    Client.Send(new BasicNoOperationMessage());
                    Client.Send(new BasicTimeMessage(DateTime.Now.DateTimeToUnixTimestamp(), 1));
                }
            }
            if (HasParty())
            {
                Party.UpdateMember(this);
            }

        }
        /// <summary>Recharge les données de guilde du personnage depuis le provider. Nettoie la guilde si elle est invalide.</summary>
        public void RefreshGuild()
        {
            if (HasGuild)
            {

                Guild = GuildProvider.Instance.GetGuild(Record.GuildId);

                if (GuildMember == null || Guild == null)
                {
                    RemoveHumanOption<CharacterHumanOptionGuild>();
                    Record.GuildId = 0;
                    return;
                }
                GuildMember.OnConnected(this);
                SendGuildMembership();

                if (Guild.Record.Motd != null && Guild.Record.Motd.Content != null)
                {
                    Client.Send(new GuildMotdMessage(Guild.Record.Motd.Content, Guild.Record.Motd.Timestamp,
                        Guild.Record.Motd.MemberId, Guild.Record.Motd.MemberName));
                }
            }
        }
        /// <summary>Envoie les informations de guilde du personnage au client.</summary>
        public void SendGuildMembership()
        {
            Client.Send(new GuildMembershipMessage(Guild.Record.GetGuildInformations(), GuildMember.Record.Rights, true));
        }
        /// <summary>Crée un nouveau contexte de jeu (ROLE_PLAY, FIGHT...). Détruit l'ancien contexte s'il existait.</summary>
        public void CreateContext(GameContextEnum context)
        {
            if (Context.HasValue)
            {
                DestroyContext();
            }

            Client.Send(new GameContextCreateMessage((sbyte)context));
            m_context = context;
        }
        /// <summary>Détruit le contexte de jeu actuel et envoie le message de destruction au client.</summary>
        public void DestroyContext()
        {
            Client.Send(new GameContextDestroyMessage());
            this.m_context = null;
        }
        /// <summary>Envoie au client le modificateur d'XP du serveur selon le taux de la zone et le multiplicateur du compte.</summary>
        public void UpdateServerExperience(int rate)
        {
            ushort percent = (ushort)(100 * (rate + ExpMultiplicator));
            Client.Send(new ServerExperienceModificatorMessage(percent));
        }
        /// <summary>Envoie un message d'information textuelle au client (notification, erreur, message système...).</summary>
        public void TextInformation(TextInformationTypeEnum msgType, ushort msgId, params object[] parameters)
        {
            Client.Send(new TextInformationMessage((sbyte)msgType, msgId,
                (from entry in parameters
                 select entry.ToString()).ToArray()));

        }
        /// <summary>
        /// This is a wtf part, im not able to fix this weird bug (Module stop loading, ContextCreateMessage not received)
        /// </summary>
        public void SafeConnection()
        {
            ActionTimer timer = new ActionTimer(10000, CreateContextForced, false);
            timer.Start();
        }

        public void RegenCharacter()
        {
            if (!Fighting)
            {
                if(Record.Stats.LifePoints != Record.Stats.MaxLifePoints)
                {       
                    Record.Stats.LifePoints = Record.Stats.LifePoints + 1;
                    RefreshStats();
                    ActionTimer timer = new ActionTimer(250, RegenCharacter, false);
                    timer.Start();
                }
            }
        }

        /// <summary>Force la création du contexte si le client n'a pas reçu le message de création dans les 10 secondes.</summary>
        void CreateContextForced()
        {
            if (!this.Context.HasValue)
            {
                Logger.Write<Character>("Context Creation is Forced for " + Name, ConsoleColor.Green);
                ContextHandler.HandleCreateContextRequest(null, Client);
            }
        }
        /// <summary>Appelé à la connexion du personnage. Envoie le message de bienvenue, les notifications en attente et la monture équipée.</summary>
        public void OnConnected()
        {
            this.Client.Send(new AlmanachCalendarDateMessage(1));
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 89, new string[0]);
            this.Reply(WorldConfiguration.Instance.WelcomeMessage, Color.BlueViolet);
            RegenCharacter();

            foreach (var notifRecord in NotificationRecord.GetConnectionNotifications(Client.Account.Id))
            {
                this.Reply(notifRecord.Notification);
                notifRecord.RemoveElement();
            }
            if (Inventory.HasMountEquiped)
            {
                Client.Send(new MountSetMessage(Inventory.Mount.GetMountClientData()));
            }
        }

        /// <summary>Initialise le status du joueur pour la session</summary>
        public void InitStatus()
        {
             Status = new PlayerStatus(Record.StatusId);
             this.Client.Send(new PlayerStatusUpdateMessage(this.Client.Account.Id,(ulong)Id,Status));
        }

        /// <summary>Affiche une fenêtre popup au joueur avec un message signé par un auteur.</summary>
        public void OpenPopup(byte lockDuration, string author, string content)
        {
            Client.Send(new PopupWarningMessage(lockDuration, author, content));
        }
        /// <summary>Téléporte le personnage à son point de spawn (zaap de spawn enregistré).</summary>
        public void SpawnPoint(bool needComplementary = false)
        {
            TeleportZaap(Record.SpawnPointMapId, needComplementary);
        }
        /// <summary>
        /// Teleporte a une carte possédant un zaap
        /// </summary>
        /// <param name="mapId"></param>
        public void TeleportZaap(int mapId, bool needComplementary = false)
        {
            if (mapId != -1)
            {
                MapRecord destinationMap = MapRecord.GetMap(mapId);
                if (destinationMap.HasZaap())
                    Teleport(mapId, ZaapDialog.GetTeleporterCell(destinationMap, destinationMap.Zaap), needComplementary);
                else
                {
                    this.ReplyError("No zaap at here, aborting teleportation.");
                    return;
                }
            }
            else
            {
                this.Teleport(WorldConfiguration.Instance.StartMapId,
                    (WorldConfiguration.Instance.StartCellId), needComplementary);
            }
            Reply("Vous avez été téléporté.");
        }
        /// <summary>Envoie un message coloré au joueur dans la fenêtre de chat.</summary>
        public void Reply(object value, Color color, bool bold = false, bool underline = false)
        {
            value = ApplyPolice(value, bold, underline);
            Client.Send(new TextInformationMessage(0, 0, new string[] { string.Format("<font color=\"#{0}\">{1}</font>", color.ToArgb().ToString("X"), value) }));
        }
        /// <summary>Applique les balises HTML de mise en forme (gras, souligné) à la valeur donnée.</summary>
        object ApplyPolice(object value, bool bold, bool underline)
        {
            if (bold)
                value = "<b>" + value + "</b>";
            if (underline)
                value = "<u>" + value + "</u>";
            return value;
        }
        /// <summary>Envoie un message texte au joueur dans la fenêtre de chat.</summary>
        public void Reply(object value, bool bold = false, bool underline = false)
        {
            value = ApplyPolice(value, bold, underline);
            Client.Send(new TextInformationMessage(0, 0, new string[] { value.ToString() }));
        }
        /// <summary>Envoie un message d'erreur en rouge au joueur.</summary>
        public void ReplyError(object value)
        {
            Reply(value, Color.DarkRed, false, false);
        }
        /// <summary>Envoie une notification serveur au joueur.</summary>
        public void Notification(string message)
        {
            Client.Send(new NotificationByServerMessage(24, new string[] { message }, true));
        }
        /// <summary>Téléporte le personnage vers la map et la cellule indiquées. Ignore si en combat, en déplacement ou occupé (sauf force=true).</summary>
        public void Teleport(int mapId, ushort? cellid = null, bool needComplementary = false, bool force = false)
        {
            if (Fighting)
                return;
            if (ChangeMap)
                return;
            if (Busy && !force)
                return;

            if (Record.MapId != mapId)
                ChangeMap = true;

            var teleportMap = MapRecord.GetMap(mapId);

            if (teleportMap != null)
            {
                if (cellid < 0 || cellid > 560)
                    cellid = teleportMap.RandomWalkableCell();

                if (cellid.HasValue)
                {
                    if (!teleportMap.Walkable(cellid.Value))
                    {
                        cellid = teleportMap.RandomWalkableCell();
                    }
                }
                else
                {
                    if (!teleportMap.Walkable(this.CellId))
                    {
                        this.CellId = teleportMap.RandomWalkableCell();
                    }
                }

                if (Map != null && Map.Id == mapId && !needComplementary)
                {
                    if (cellid != null)
                    {
                        SendMap(new TeleportOnSameMapMessage(Id, (ushort)cellid));
                        Record.CellId = cellid.Value;
                    }
                    else
                        SendMap(new TeleportOnSameMapMessage(Id, (ushort)this.Record.CellId));
                    this.MovementKeys = null;
                    this.IsMoving = false;
                    return;
                }
                if (Map != null)
                    Map.Instance.RemoveEntity(this);

                this.MovementKeys = null;
                this.IsMoving = false;
                CurrentMapMessage(mapId);


                if (cellid != null)
                    this.Record.CellId = cellid.Value;
                this.Record.MapId = mapId;
            }
            else
            {
                Client.Character.ReplyError("The map dosent exist...");
            }
        }
        /// <summary>Ajoute de l'XP à tous les Pokéfus équipés après un combat. Déclenche la montée de niveau si nécessaire.</summary>
        public void AddMinationExperience(ulong experienceFightDelta)
        {
            foreach (var item in Inventory.GetEquipedMinationItems())
            {
                var effect = item.FirstEffect<EffectMinationLevel>();

                if (effect != null)
                {
                    var level = effect.Level;
                    effect.AddExperience(experienceFightDelta);

                    if (level != effect.Level)
                    {
                        OnMinationLevelUp(item.FirstEffect<EffectMination>(), effect.Level);
                    }
                    Inventory.OnItemModified(item);
                }
            }

        }
        /// <summary>Envoie au client le message de chargement de la map (déclenche le changement de map côté client).</summary>
        public void CurrentMapMessage(int mapId)
        {
            Client.Send(new CurrentMapMessage(mapId, WorldConfiguration.Instance.MapKey));

        }
        /// <summary>Déplace le personnage sur la map selon les cellules du chemin. Annule si le personnage est occupé ou si la cellule de départ ne correspond pas.</summary>
        public void MoveOnMap(short[] cells)
        {
            if (!Busy)
            {
                ushort clientCellId = (ushort)PathParser.ReadCell(cells.First());

                if (clientCellId == CellId)
                {

                    if (Look.RemoveAura())
                        RefreshActorOnMap();
                    sbyte direction = PathParser.GetDirection(cells.Last());
                    ushort cellid = (ushort)PathParser.ReadCell(cells.Last());

                    this.Record.Direction = direction;
                    this.MovedCell = cellid;
                    this.IsMoving = true;
                    this.MovementKeys = cells;
                    this.SendMap(new GameMapMovementMessage(cells, this.Id));
                }
                else
                {
                    this.NoMove();
                }
            }
            else
            {
                this.NoMove();
            }
        }
        /// <summary>Annule le déplacement en cours et retourne le personnage à sa position actuelle.</summary>
        public void NoMove()
        {
            this.Client.Send(new GameMapNoMovementMessage((short)Point.X, (short)Point.Y));
        }
        /// <summary>Ouvre une interface utilisateur spécifique déclenchée par l'utilisation d'un item.</summary>
        public void OpenUIByObject(sbyte id, uint itemUid)
        {
            Client.Send(new ClientUIOpenedByObjectMessage(id, itemUid));
        }
        /// <summary>Finalise le déplacement : met à jour la cellule du personnage et vérifie si un item est à ramasser.</summary>
        public void EndMove()
        {
            this.Record.CellId = this.MovedCell;
            this.MovedCell = 0;
            this.IsMoving = false;
            this.MovementKeys = null;

            DropItem item = Map.Instance.GetDroppedItem(this.Record.CellId);

            if (item != null)
            {
                item.OnPickUp(this);
            }

        }

        /// <summary>Quitte le dialogue ou la requête en cours. Annule la requête si active, ferme le dialogue sinon.</summary>
        public void LeaveDialog()
        {
            if (this.Dialog == null && !this.IsInRequest())
            {
                this.ReplyError("Unknown dialog...");
                return;
            }
            else
            {
                if (this.IsInRequest())
                {
                    this.CancelRequest();
                }
                if (this.Dialog != null)
                    this.Dialog.Close();
            }
        }
        /// <summary>Ouvre l'interface d'écurie (gestion des montures).</summary>
        public void OpenPaddock()
        {
            this.OpenDialog(new MountStableExchange(this));
        }
        /// <summary>Ouvre l'interface de banque du personnage.</summary>
        public void OpenBank()
        {
            this.OpenDialog(new BankExchange(this, BankItemRecord.GetBankItems(this.Client.Account.Id)));
        }
        /// <summary>Ouvre l'interface de création de guilde.</summary>
        public void OpenGuildCreationPanel()
        {
            this.OpenDialog(new GuildCreationDialog(this));
        }
        /// <summary>Ouvre la boutique d'un PNJ avec les items à vendre.</summary>
        public void OpenNpcShop(Npc npc, ItemRecord[] itemToSell, ushort tokenId, bool priceLevel)
        {
            this.OpenDialog(new NpcShopExchange(this, npc, itemToSell, tokenId, priceLevel));
        }
        /// <summary>Démarre un dialogue avec un PNJ selon l'action associée.</summary>
        public void TalkToNpc(Npc npc, NpcActionRecord action)
        {
            this.OpenDialog(new NpcTalkDialog(this, npc, action));
        }
        /// <summary>Ouvre l'interface du zaap (téléportation longue distance).</summary>
        public void OpenZaap(MapInteractiveElementSkill skill)
        {
            this.OpenDialog(new ZaapDialog(this, skill));
        }
        /// <summary>Ouvre l'interface du zaapi (téléportation courte distance).</summary>
        public void OpenZaapi(MapInteractiveElementSkill skill)
        {
            this.OpenDialog(new ZaapiDialog(this, skill));
        }
        /// <summary>Ouvre l'interface de vente de l'hôtel des ventes.</summary>
        public void OpenBidhouseSell(Npc npc, BidShopRecord bidshop, bool force)
        {
            this.OpenDialog(new SellExchange(this, npc, bidshop), force);
        }
        /// <summary>Ouvre l'interface d'achat de l'hôtel des ventes.</summary>
        public void OpenBidhouseBuy(Npc npc, BidShopRecord bidshop, bool force)
        {
            this.OpenDialog(new BuyExchange(this, npc, bidshop), force);
        }
        /// <summary>Ouvre l'interface de craft pour un métier donné.</summary>
        public void OpenCraftPanel(uint skillId, JobsTypeEnum jobType)
        {
            this.OpenDialog(new CraftExchange(this, skillId, jobType));
        }
        /// <summary>Ouvre l'interface de smithmagic (modification des effets d'équipement).</summary>
        public void OpenSmithMagicPanel(uint skillId, JobsTypeEnum jobType)
        {
            this.OpenDialog(new SmithMagicExchange(this, skillId, jobType));
        }
        /// <summary>Ouvre un livre ou document lisible en jeu.</summary>
        public void ReadDocument(ushort documentId)
        {
            this.OpenDialog(new BookDialog(this, documentId));
        }

        /// <summary>Ajoute des kamas au personnage. Plafonne au maximum autorisé. Retourne true si réussi.</summary>
        public bool AddKamas(int value)
        {
            if (value <= int.MaxValue)
            {
                if (Record.Kamas + value >= Inventory.MaxKamas)
                {
                    Record.Kamas = Inventory.MaxKamas;
                }
                else
                    Record.Kamas += value;
                Inventory.RefreshKamas();
                return true;
            }
            return false;
        }
        /// <summary>Inscrit le personnage en arène PvP.</summary>
        public void RegisterArena()
        {
            this.ArenaMember = ArenaProvider.Instance.Register(this);
            this.ArenaMember.UpdateStep(true, PvpArenaStepEnum.ARENA_STEP_REGISTRED);
        }
        /// <summary>Notifie le joueur qu'un item a été déséquipé lors d'un combat d'arène.</summary>
        public void OnItemUnequipedArena()
        {
            TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 298);
        }
        /// <summary>Désinscrit le personnage de l'arène PvP.</summary>
        public void UnregisterArena()
        {
            if (InArena)
            {
                ArenaProvider.Instance.Unregister(this);
                this.ArenaMember.UpdateStep(false, PvpArenaStepEnum.ARENA_STEP_UNREGISTER);
                this.ArenaMember = null;
            }
            else
            {
                Logger.Write<Character>("Try to unregister arena while not in arena...", ConsoleColor.Red);
            }
        }
        /// <summary>Répond à une invitation d'arène (accepter ou refuser).</summary>
        public void AnwserArena(bool accept)
        {
            if (InArena)
            {
                ArenaMember.Anwser(accept);
            }
            else
            {
                Logger.Write<Character>("Unable to answer arena while not in arena...", ConsoleColor.Red);
            }
        }
        /// <summary>Envoie les informations d'arène mises à jour au client (rang, statistiques).</summary>
        public void RefreshArenaInfos()
        {
            Client.Send(new GameRolePlayArenaUpdatePlayerInfosMessage(Record.ArenaRank.GetArenaRankInfos()));
        }
        /// <summary>Retourne le statut du joueur (disponible, absent, occupé...).</summary>
        public PlayerStatus GetPlayerStatus()
        {
            return Status;
        }
        /// <summary>Retourne les données du métier demandé pour ce personnage.</summary>
        public CharacterJob GetJob(JobsTypeEnum job)
        {
            return Record.Jobs.Find(x => x.JobType == job);
        }
        /// <summary>Envoie au client la liste complète des métiers et leur XP.</summary>
        public void RefreshJobs()
        {
            Client.Send(new JobCrafterDirectorySettingsMessage(Record.Jobs.ConvertAll<JobCrafterDirectorySettings>(x => x.GetDirectorySettings()).ToArray()));
            Client.Send(new JobDescriptionMessage(Record.Jobs.ConvertAll<JobDescription>(x => x.GetJobDescription()).ToArray()));
            Client.Send(new JobExperienceMultiUpdateMessage(Record.Jobs.ConvertAll<JobExperience>(x => x.GetJobExperience()).ToArray()));
        }
        /// <summary>Ajoute de l'XP à un métier. Gère la montée de niveau du métier et la mise à jour des compétences autorisées.</summary>
        public void AddJobExp(JobsTypeEnum jobType, ulong amount)
        {
            CharacterJob job = GetJob(jobType);
            ushort currentLevel = job.Level;
            ulong highest = ExperienceRecord.HighestExperience().Job;

            if (job.Experience + amount > highest)
                job.Experience = highest;
            else
                job.Experience += amount;

            Client.Send(new JobExperienceUpdateMessage(job.GetJobExperience()));

            if (currentLevel != job.Level)
            {
                Client.Send(new JobLevelUpMessage((byte)job.Level, job.GetJobDescription()));
                this.SkillsAllowed = SkillsProvider.Instance.GetAllowedSkills(this).ToArray();
            }

        }
        /// <summary>Définit l'alignement du personnage (Bontarien, Brakmairien, neutre...) et rafraîchit l'affichage.</summary>
        public void SetSide(AlignmentSideEnum side)
        {
            this.Record.Alignment.Side = side;
            this.RefreshStats();
            this.RefreshActorOnMap();
        }
        /// <summary>Ajoute des points d'honneur (alignement) au personnage, plafonnés au maximum.</summary>
        public void AddHonor(ushort amount)
        {
            ushort highest = (ushort)ExperienceRecord.HighestHonorExperience().Honor;

            if (Record.Alignment.Honor + amount > highest)
                Record.Alignment.Honor = highest;
            else
                Record.Alignment.Honor += amount;


            RefreshActorOnMap();
            RefreshStats();
        }
        /// <summary>Retire des points d'honneur au personnage, sans descendre sous zéro.</summary>
        public void RemoveHonor(ushort amount)
        {
            if (Record.Alignment.Honor - amount < 0)
            {
                Record.Alignment.Honor = 0;
            }
            else
            {
                Record.Alignment.Honor -= amount;
            }
            RefreshActorOnMap();
            RefreshStats();
        }
        /// <summary>Active ou désactive le mode PvP du personnage. Retourne le nouveau statut d'agressabilité.</summary>
        public AggressableStatusEnum TogglePvP()
        {
            if (Record.Alignment.Agressable == AggressableStatusEnum.NON_AGGRESSABLE)
            {
                Record.Alignment.Agressable = AggressableStatusEnum.PvP_ENABLED_AGGRESSABLE;
            }
            else if (Record.Alignment.Agressable == AggressableStatusEnum.PvP_ENABLED_AGGRESSABLE)
            {
                Record.Alignment.Agressable = AggressableStatusEnum.NON_AGGRESSABLE;
            }


            RefreshActorOnMap();
            RefreshStats();

            return Record.Alignment.Agressable;
        }
        /// <summary>Envoie la mise à jour du rang d'alignement au client.</summary>
        public void RefreshAlignment()
        {
            Client.Send(new AlignmentRankUpdateMessage(Record.Alignment.Value, false));
        }
        /// <summary>Retire des kamas au personnage. Retourne true si le joueur avait assez de kamas, false sinon.</summary>
        public bool RemoveKamas(int value)
        {
            if (Record.Kamas >= value)
            {
                Record.Kamas -= value;
                Inventory.RefreshKamas();
                return true;
            }
            else
            {
                return false;
            }

        }
        /// <summary>Ajoute une option humaine (ornement, titre, familier...) et rafraîchit l'apparence sur la map.</summary>
        private void AddHumanOption(CharacterHumanOption option)
        {
            Record.HumanOptions.Add(option);
            RefreshActorOnMap();
        }
        /// <summary>Retire une option humaine spécifique et rafraîchit l'apparence sur la map.</summary>
        private void RemoveHumanOption(CharacterHumanOption option)
        {
            Record.HumanOptions.Remove(option);
            RefreshActorOnMap();
        }
        /// <summary>Retire toutes les options humaines du type T et rafraîchit l'apparence sur la map.</summary>
        public void RemoveHumanOption<T>() where T : CharacterHumanOption
        {
            Record.HumanOptions.RemoveAll(x => x is T);
            RefreshActorOnMap();
        }
        /// <summary>Retourne la première option humaine du type T, ou null si aucune.</summary>
        private T GetFirstHumanOption<T>() where T : CharacterHumanOption
        {
            return Record.HumanOptions.OfType<T>().ToArray().FirstOrDefault();
        }

        /// <summary>Envoie au client la liste de tous les titres et ornements connus, avec ceux actuellement actifs.</summary>
        public void SendTitlesAndOrnamentsList()
        {
            Client.Send(new TitlesAndOrnamentsListMessage(Record.KnownTitles.ToArray(), Record.KnownOrnaments.ToArray(),
                (ushort)(ActiveTitle != null ? ActiveTitle.TitleId : 0), (ushort)(ActiveOrnament != null ? ActiveOrnament.OrnamentId : 0)));
        }

        /// <summary>Apprend un ornement au personnage. Retourne true si réussi, false si déjà connu.</summary>
        public bool LearnOrnament(ushort id, bool send)
        {
            if (!Record.KnownOrnaments.Contains(id))
            {
                Record.KnownOrnaments.Add(id);
                if (send)
                    Client.Send(new OrnamentGainedMessage((short)id));
                return true;
            }
            else
            {
                return false;
            }

        }
        /// <summary>Utilise un item de l'inventaire par son UID. Le retire si consommable. Rafraîchit les stats si send=true.</summary>
        public void UseItem(uint uid, bool send)
        {
            var item = this.Inventory.GetItem(uid);

            if (item != null)
            {
                if (ItemUseProvider.Handle(Client.Character, item))
                    this.Inventory.RemoveItem(item.UId, 1);

                if (send)
                {
                    this.RefreshStats();
                }
            }


        }
        /// <summary>Retire un ornement de la liste des ornements connus. Désactive l'ornement actif si c'est celui-là.</summary>
        public bool ForgetOrnament(ushort id)
        {
            if (Record.KnownOrnaments.Contains(id))
            {
                Record.KnownOrnaments.Remove(id);
                if (ActiveOrnament.OrnamentId == id)
                {
                    RemoveHumanOption<CharacterHumanOptionOrnament>();
                    RefreshActorOnMap();

                }
                return true;
            }
            return false;

        }
        /// <summary>Mute le personnage pendant un nombre de secondes donné. Retourne false si déjà muté.</summary>
        public bool Mute(int seconds)
        {
            if (!Record.Muted)
            {
                Record.Muted = true;

                ActionTimer timer = new ActionTimer(seconds * 1000, new Action(() =>
                   {
                       Record.Muted = false;
                   }), false);
                timer.Start();

                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>Retourne true si le personnage possède l'ornement donné.</summary>
        public bool HasOrnament(ushort id)
        {
            return Record.KnownOrnaments.Contains(id) ? true : false;

        }
        /// <summary>Ouvre une boîte de requête entre la source et la cible (défi, échange, invitation de guilde...).</summary>
        public void OpenRequestBox(RequestBox box)
        {
            box.Source.RequestBox = box;
            this.RequestBox = box;
            box.Open();
        }
        /// <summary>Envoie un message d'erreur d'échange au client.</summary>
        public void OnExchangeError(ExchangeErrorEnum error)
        {
            this.Client.Send(new ExchangeErrorMessage((sbyte)error));
        }
        /// <summary>Envoie un message d'erreur d'invitation de groupe au client.</summary>
        public void OnPartyJoinError(PartyJoinErrorEnum error, int partyId = 0)
        {
            this.Client.Send(new PartyCannotJoinErrorMessage((uint)partyId, (sbyte)error));
        }
        /// <summary>
        /// Conditions & EnterParty (Stump)
        /// </summary>
        /// <param name="character"></param>
        public void InviteParty(Character character)
        {
            if (!this.HasParty())
            {
                AbstractParty party = PartyProvider.Instance.CreateParty(this);
                PartyProvider.Instance.Parties.Add(party);
                party.Create(this, character);
            }
            else
            {
                if (!Party.IsFull)
                    AbstractParty.SendPartyInvitationMessage(character, this, this.Party);
            }
        }
        /// <summary>Active l'ornement donné sur le personnage. Désactive l'ancien ornement si présent. Retourne false si déjà actif.</summary>
        public bool SetOrnament(ushort id)
        {
            if (id == 0)
            {
                RemoveHumanOption<CharacterHumanOptionOrnament>();
                Client.Send(new OrnamentSelectedMessage(id));
                return true;
            }

            if (HasOrnament(id))
            {
                if (ActiveOrnament != null && ActiveOrnament.OrnamentId == id)
                    return false;

                RemoveHumanOption<CharacterHumanOptionOrnament>();
                AddHumanOption(new CharacterHumanOptionOrnament(id));
                Client.Send(new OrnamentSelectedMessage(id));
                return true;

            }
            return false;
        }

        /// <summary>Apprend un titre au personnage. Retourne true si réussi, false si déjà connu.</summary>
        public bool LearnTitle(ushort id)
        {
            if (!Record.KnownTitles.Contains(id))
            {
                Record.KnownTitles.Add(id);
                Client.Send(new TitleGainedMessage(id));
                return true;

            }
            return false;
        }

        /// <summary>Retire un titre de la liste des titres connus. Retourne true si réussi.</summary>
        public bool ForgetTitle(ushort id)
        {

            if (Record.KnownTitles.Contains(id))
            {
                Record.KnownTitles.Remove(id);
                Client.Send(new TitleLostMessage(id));

                return true;

            }
            return false;

        }
        /// <summary>Retourne true si le personnage possède le titre donné.</summary>
        public bool HasTitle(ushort id)
        {
            return Record.KnownTitles.Contains(id) ? true : false;
        }

        /// <summary>Active le titre donné sur le personnage. Désactive l'ancien titre si présent. Retourne false si déjà actif.</summary>
        public bool SelectTitle(ushort id)
        {
            if (id == 0)
            {
                RemoveHumanOption<CharacterHumanOptionTitle>();
                Client.Send(new TitleSelectedMessage(id));
                return true;

            }
            if (HasTitle(id))
            {
                if (ActiveTitle != null && ActiveTitle.TitleId == id)
                    return false;

                RemoveHumanOption<CharacterHumanOptionTitle>();
                AddHumanOption(new CharacterHumanOptionTitle(id, string.Empty));
                Client.Send(new TitleSelectedMessage(id));
                return true;

            }
            return false;

        }
        /// <summary>Lance une cinématique côté client par son identifiant.</summary>
        public void PlayCinematic(ushort id)
        {
            Client.Send(new CinematicMessage(id));
        }
        /// <summary>Appelé après la création du contexte. Lance la cinématique de départ si c'est un nouveau personnage.</summary>
        public void OnContextCreated()
        {
            if (this.New && WorldConfiguration.Instance.PlayDefaultCinematic)
            {
                PlayCinematic(10);
                New = false;
            }
        }
        /// <summary>Retourne les informations complètes de l'acteur pour l'affichage sur la map (apparence, position, alignement...).</summary>
        public override GameRolePlayActorInformations GetActorInformations()
        {
            return new GameRolePlayCharacterInformations(Id, Look.ToEntityLook(),
                new EntityDispositionInformations((short)CellId, (sbyte)Direction),
                Name, new HumanInformations(new ActorRestrictionsInformations(), Record.Sex,
                    Record.HumanOptions.ConvertAll<HumanOption>(x => x.GetHumanOption()).ToArray())
                , Client.Account.Id, Record.Alignment.GetActorAlignmentInformations());
        }
        /// <summary>Retourne les informations minimales du personnage (ID, nom, niveau) utilisées dans les listes de groupe.</summary>
        public CharacterMinimalInformations GetCharacterMinimalInformations()
        {
            return new CharacterMinimalInformations((ulong)Id, Name, (byte)Level);
        }
        /// <summary>Envoie un message d'erreur de chat au client (muet, canal interdit...).</summary>
        public void OnChatError(ChatErrorEnum error)
        {
            Client.Send(new ChatErrorMessage((sbyte)error));
        }
        /// <summary>Envoie les restrictions du personnage au client (peut se déplacer, attaquer, etc.).</summary>
        public void SetRestrictions()
        {
            Client.Send(new SetCharacterRestrictionsMessage(this.Id,
                new ActorRestrictionsInformations(false, false, false, false, false, false, false, false, false, false, false,
                    false, false, true, true, false, false, false, false, false, false)));
        }
        /// <summary>Retourne le personnage en roleplay après un combat. Si perdant et spawnJoin=true, retourne au point de spawn.</summary>
        public void RejoinMap(FightTypeEnum fightType, bool winner, bool spawnJoin)
        {
            if (winner && this.Fighter.Stats.CurrentLifePoints > Record.Stats.MaxLifePoints / 100 * 33)
            {
                    Record.Stats.LifePoints = this.Fighter.Stats.CurrentLifePoints;
            }
            else
            {
                Record.Stats.LifePoints = Record.Stats.MaxLifePoints / 100 * 33;
            }
            int avgMonsterLevel = (int)this.Fighter.OposedTeam().GetFighters<Fighter>(false).Average(x => x.Level);
            DestroyContext();
            CreateContext(GameContextEnum.ROLE_PLAY);
            this.Fighter = null;
            this.FighterMaster = null;
            RegenCharacter();
            if (spawnJoin && !winner && Client.Account.Role != ServerRoleEnum.Fondator)
            {
                SpawnPoint(true);
            }
            else
            {
                if (!OnFightEnded(winner, fightType, avgMonsterLevel))
                    CurrentMapMessage(Record.MapId);
            }

        }
        /// <summary>Appelé à la déconnexion. Ferme les dialogues, quitte le combat/groupe/guilde/arène, et sauvegarde le record.</summary>
        public void OnDisconnected()
        {
            if (Dialog != null)
                Dialog.Close();
            if (IsInRequest())
                CancelRequest();

            if (InArena)
                UnregisterArena();

            if (Fighting)
                FighterMaster.OnDisconnected();

            if (HasParty())
                Party.Leave(this);

            if (HasGuild)
                GuildMember.OnDisconnected();

            DeclineAllPartyInvitations();

            Look.RemoveAura();
            if (Map != null)
                Map.Instance.RemoveEntity(this);

            Record.UpdateInstantElement();

        }

        /// <summary>Refuse toutes les invitations de groupe en attente lors de la déconnexion.</summary>
        private void DeclineAllPartyInvitations()
        {
            foreach (var party in new List<AbstractParty>(GuestedParties))
            {
                party.RefuseInvation(this);
            }
        }


        /// <summary>Appelé après la création d'une guilde. Envoie le résultat et ferme le dialogue.</summary>
        public void OnGuildCreated(GuildCreationResultEnum result)
        {
            Client.Send(new GuildCreationResultMessage((sbyte)result));
            Dialog.Close();
        }


        /// <summary>Retourne une représentation textuelle du personnage pour le débogage.</summary>
        public override string ToString()
        {
            return "Character: (" + Name + ")";
        }

        /// <summary>Appelé quand le personnage rejoint une guilde. Initialise les données de guilde et envoie les informations au client.</summary>
        public void OnGuildJoined(GuildInstance guild, GuildMemberInstance member)
        {
            this.Guild = guild;
            this.Record.GuildId = Guild.Id;
            this.AddHumanOption(new CharacterHumanOptionGuild(Guild.Record.GetGuildInformations()));
            Client.Send(new GuildJoinedMessage(Guild.Record.GetGuildInformations(),
                member.Record.Rights, true));
        }
        /// <summary>Appelé quand un Pokéfus monte de niveau. Affiche une popup de félicitations au joueur.</summary>
        public void OnMinationLevelUp(EffectMination minationEffect, ushort newLevel)
        {
            OpenPopup(0, "Félicitation", "Votre Pokéfus " + minationEffect.MonsterName + " vient de passer niveau " + newLevel + "!");
        }

    }

}
