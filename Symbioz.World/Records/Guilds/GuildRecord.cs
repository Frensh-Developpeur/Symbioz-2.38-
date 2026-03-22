using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using Symbioz.Core;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Guilds;
using Symbioz.Protocol.Types;
using Symbioz.World.Records.Characters;
using Symbioz.Core.DesignPattern.StartupEngine;

namespace Symbioz.World.Records.Guilds
{
    /// <summary>
    /// Données d'une guilde persistées en base de données.
    ///
    /// Une guilde est créée avec New() et comprend :
    ///   - Un blason unique (Emblem) : vérifié avant la création pour éviter les doublons
    ///   - Une liste de membres (Members) avec leurs rangs et permissions
    ///   - Un Message du Jour (Motd)
    ///   - Une expérience qui détermine le niveau de guilde (via ExperienceRecord.Guild)
    ///
    /// [Resettable] signifie que la table peut être réinitialisée lors d'un reset serveur.
    /// [Xml, Update] sur Members et Motd = sérialisés en XML et sauvegardés lors des mises à jour.
    /// </summary>
    [Table("Guilds"), Resettable]
    public class GuildRecord : ITable
    {
        // Liste de toutes les guildes chargées en mémoire
        public static List<GuildRecord> Guilds = new List<GuildRecord>();

        [Primary]
        public int Id;          // Identifiant unique de la guilde (auto-incrémenté)

        public string Name;     // Nom de la guilde (unique)

        [Xml]
        public ContextGuildEmblem Emblem;   // Blason (symbolique/couleurs, unique par guilde)

        [Update]
        public ulong Experience;            // XP totale de la guilde (détermine le niveau)

        public int MaxTaxCollectors;        // Nombre maximum de percepteurs autorisés

        [Xml, Update]
        public List<ContextGuildMember> Members; // Liste des membres avec leurs rangs

        [Xml, Update]
        public GuildMotd Motd;              // Message du Jour de la guilde

        public GuildRecord(int id, string name, ContextGuildEmblem emblem, ulong experience,
            int maxTaxCollectors, List<ContextGuildMember> members, GuildMotd motd)
        {
            this.Id = id;
            this.Name = name;
            this.Emblem = emblem;
            this.Experience = experience;
            this.MaxTaxCollectors = maxTaxCollectors;
            this.Members = members;
            this.Motd = motd;
        }
        // Vérifie si un blason identique existe déjà (pour éviter les doublons à la création)
        public static bool Exist(ContextGuildEmblem emblem)
        {
            return Guilds.FirstOrDefault(x => x.Emblem == emblem) != null;
        }

        // Vérifie si un nom de guilde est déjà pris
        public static bool Exist(string name)
        {
            return Guilds.FirstOrDefault(x => x.Name == name) != null;
        }

        // Retourne le membre de guilde correspondant à l'Id de personnage donné
        public ContextGuildMember GetContextGuildMember(long id)
        {
            return Members.FirstOrDefault(x => x.CharacterId == id);
        }

        // Retourne la guilde par son identifiant
        public static GuildRecord GetGuild(int id)
        {
            return Guilds.FirstOrDefault(x => x.Id == id);
        }

        // Crée une nouvelle guilde et l'insère en base.
        // DynamicPop génère automatiquement un Id libre en cherchant le premier trou dans la liste.
        public static GuildRecord New(string name, ContextGuildEmblem emblem, int maxTaxCollector)
        {
            return new GuildRecord(Guilds.DynamicPop(x => x.Id), name, emblem, 0, maxTaxCollector, new List<ContextGuildMember>(), new GuildMotd());
        }

        // Sérialise les informations de guilde pour le protocole client
        public GuildInformations GetGuildInformations()
        {
            return new GuildInformations((uint)Id, Name, (byte)ExperienceRecord.GetLevelFromGuildExperience(Experience), Emblem.ToGuildEmblem());
        }

        // Retire un personnage de la liste des membres de sa guilde et sauvegarde.
        // Appelé lors de la suppression de personnage ou du retrait de guilde.
        public static void RemoveWhereId(CharacterRecord record)
        {
            var guildRecord = GuildRecord.GetGuild(record.GuildId);

            if (guildRecord != null)
            {
                var member = guildRecord.GetContextGuildMember(record.Id);

                if (member != null)
                {
                    guildRecord.Members.Remove(member);
                    guildRecord.UpdateElement();
                }
            }
        }
    }
}
