using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Providers.Fights.Challenges;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Models.Fights.Fighters
{
    /// <summary>
    /// Classe abstraite représentant un combattant capable de jouer activement.
    /// Ajoute la capacité d'envoyer des messages réseau et d'avoir un synchroniseur personnel.
    /// Héritée par CharacterFighter (joueurs humains).
    /// Gère aussi les messages d'erreur lors de l'échec d'un sort.
    /// </summary>
    public abstract class PlayableFighter : Fighter
    {
        public PlayableFighter(FightTeam team, ushort mapCellId)
            : base(team, mapCellId)
        {
        }

        // Synchroniseur personnel : utilisé quand CE joueur spécifiquement doit être synchronisé
        // (ex: quand il quitte un combat en cours)
        public Synchronizer PersonalSynchronizer
        {
            get;
            set;
        }

        // Méthode d'envoi de message réseau (implémentée différemment selon le type de client)
        public abstract void Send(Message message);

        // Récupère un sort du personnage par son ID
        public abstract CharacterSpell GetSpell(ushort spellId);

        /// <summary>
        /// Notifie le client qu'un mouvement est impossible à la position actuelle.
        /// </summary>
        public void NoMove()
        {
            this.Send(new GameMapNoMovementMessage((short)Point.X, (short)Point.Y));

        }
        public override void PassTurn()
        {
            base.PassTurn();
        }

        public override void OnMoveFailed()
        {
            this.NoMove();
        }

        internal void ShowChallengeTargetsList(ushort challengeId)
        {
            Challenge challenge = Fight.GetChallenge(challengeId);

            if (challenge != null)
            {
                challenge.ShowTargetsList(this);
            }
        }
        public abstract Character GetCharacterPlaying();

        /// <summary>
        /// Appelé quand un sort ne peut pas être lancé.
        /// Envoie au client le message d'erreur correspondant à la raison de l'échec
        /// (pas assez de PA, hors de portée, pas de ligne de vue, cooldown, etc.)
        /// puis envoie GameActionFightNoSpellCastMessage pour annuler l'animation côté client.
        /// </summary>
        public override void OnSpellCastFailed(SpellCastResultEnum result, SpellLevelRecord level)
        {
            Character character = GetCharacterPlaying();

            switch (result)
            {
                case SpellCastResultEnum.NotEnoughAp:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 170, new object[]
                          {
                            Stats.ActionPoints.TotalInContext(),
                             level.ApCost,
                          });
                    break;
                case SpellCastResultEnum.CantBeSeen:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 174);
                    break;
                case SpellCastResultEnum.HistoryError:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 175);
                    break;
                case SpellCastResultEnum.FightEnded:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 175);
                    break;
                case SpellCastResultEnum.NotPlaying:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 175);
                    break;
                case SpellCastResultEnum.StateForbidden:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 175);
                    break;
                case SpellCastResultEnum.StateRequired:
                    character.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 175);
                    break;
            }
            character.Client.Send(new GameActionFightNoSpellCastMessage((uint)level.Id));
            base.OnSpellCastFailed(result, level);
        }
    }
}
