using SSync.Messages;
using Symbioz.Protocol.Messages;
using Symbioz.Protocol.Types;
using Symbioz.World.Models.Entities;
using Symbioz.World.Network;
using Symbioz.World.Records;
using System;
using Symbioz.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.World.Models.Maps.Shapes;
using Symbioz.World.Providers.Maps;
using Symbioz.World.Providers.Maps.Path;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Records.Maps;
using Symbioz.World.Models.Monsters;
using Symbioz.World.Records.Npcs;
using Symbioz.World.Providers.Maps.Monsters;
using Symbioz.World.Models.Fights;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Records.Interactives;
using Symbioz.Protocol.Selfmade.Enums;
using Symbioz.World.Models.Items;
using Symbioz.World.Records.Characters;

namespace Symbioz.World.Models.Maps.Instances
{
    /// <summary>
    /// Implémentation concrète d'AbstractMapInstance pour les maps de jeu de rôle standard.
    ///
    /// MapInstance surcharge GetMapComplementaryInformationsDataMessage pour assembler
    /// le message envoyé à chaque joueur qui entre sur la map. Ce message contient :
    ///   - Les acteurs présents (joueurs, monstres, PNJ, portails)
    ///   - Les éléments interactifs (arbres, minerais, zaaps) et leur état
    ///   - Les combats en cours visibles
    ///   - Les maisons, obstacles et indicateur de monstres agressifs
    /// </summary>
    public class MapInstance : AbstractMapInstance
    {
        public MapInstance(MapRecord record) : base(record)
        {
        }

        // Construit le message de chargement complet de la map pour un joueur donné.
        // Appelé à l'arrivée sur la map et lors d'un rechargement (Reload).
        public override MapComplementaryInformationsDataMessage GetMapComplementaryInformationsDataMessage(Character character)
        {
            return new MapComplementaryInformationsDataMessage(character.SubareaId, Record.Id, GetHousesInformations(), GetGameRolePlayActorsInformations(),
                GetInteractivesElements(character), GetStatedElements(), GetMapObstacles(), GetFightsCommonInformations(), HasAgressiveMonsters());
        }
    }
}
