using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbioz.Core;
using Symbioz.Protocol.Enums;
using System.Threading.Tasks;
using Symbioz.Protocol.Types;
using Symbioz.World.Records;
using YAXLib.Attributes;

namespace Symbioz.World.Models.Entities.Alignment
{
    /// <summary>
    /// Alignement d'un personnage dans le système de PvP/PvA de Dofus.
    ///
    /// Un alignement possède :
    ///   - Side : faction choisie (Bontarien, Brâkmarien, Mercenaire, Neutre)
    ///   - Value : niveau d'alignement (0 à 100), montre l'investissement PvP
    ///   - Honor : points d'honneur accumulés, utilisés pour calculer le Grade (1-10)
    ///   - Agressable : indique si le joueur est en mode PvP (peut être attaqué)
    ///
    /// Grade et les seuils d'honneur sont calculés dynamiquement depuis ExperienceRecord.
    /// Les propriétés [YAXDontSerialize] ne sont pas persistées en base de données (calculées à la volée).
    /// </summary>
    public class CharacterAlignment
    {
        // Faction d'alignement choisie (ALIGNMENT_NEUTRAL = 0 par défaut)
        public AlignmentSideEnum Side
        {
            get;
            set;
        }

        // Niveau d'alignement (profondeur d'investissement PvP, 0-100)
        public sbyte Value
        {
            get;
            set;
        }

        // Grade d'alignement calculé depuis l'honneur (1-10, via ExperienceRecord.GetGrade)
        [YAXDontSerialize]
        public sbyte Grade
        {
            get
            {
                return ExperienceRecord.GetGrade(Honor);
            }
        }

        // Puissance du personnage liée à l'alignement (utilisée dans les stats PvP)
        public double CharacterPower
        {
            get;
            set;
        }

        // Points d'honneur accumulés (déterminant le Grade)
        public ushort Honor
        {
            get;
            set;
        }

        // Points d'honneur requis pour atteindre le Grade actuel (seuil bas du grade courant)
        [YAXDontSerialize]
        public ushort HonorGradeFloor
        {
            get
            {
                return ExperienceRecord.GetHonorForGrade(Grade);
            }
        }

        // Points d'honneur requis pour le grade suivant (seuil haut = objectif à atteindre)
        [YAXDontSerialize]
        public ushort HonorGradeNextFloor
        {
            get
            {
                return ExperienceRecord.GetHonorNextGrade(Grade);
            }
        }

        // Statut d'agressabilité PvP (le joueur peut-il être attaqué par un joueur adverse ?)
        public AggressableStatusEnum Agressable
        {
            get;
            set;
        }

        // Sérialise l'alignement complet pour la fiche de personnage (toutes les infos)
        public ActorExtendedAlignmentInformations GetActorExtendedAlignement()
        {
            return new ActorExtendedAlignmentInformations((sbyte)Side, Value, Grade, CharacterPower, Honor,
                HonorGradeFloor, HonorGradeNextFloor, (sbyte)Agressable);
        }

        // Sérialise l'alignement pour les autres joueurs (visible seulement si PvP activé)
        public ActorAlignmentInformations GetActorAlignmentInformations()
        {
            return Agressable == AggressableStatusEnum.PvP_ENABLED_AGGRESSABLE ? new ActorAlignmentInformations((sbyte)Side, Value, Grade, CharacterPower)
                : new ActorAlignmentInformations(0, 0, 0, 0);
        }

        // Crée un alignement neutre par défaut pour un nouveau personnage
        public static CharacterAlignment New()
        {
            return new CharacterAlignment()
            {
                Agressable = AggressableStatusEnum.NON_AGGRESSABLE,
                CharacterPower = 0,
                Honor = 0,
                Side = AlignmentSideEnum.ALIGNMENT_NEUTRAL,
                Value = 0
            };
        }
    }
}
