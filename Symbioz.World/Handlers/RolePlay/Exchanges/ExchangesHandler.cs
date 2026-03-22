using SSync.Messages;
using Symbioz.Protocol.Enums;
using Symbioz.Protocol.Messages;
using Symbioz.World.Models.Dialogs.DialogBox;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Exchanges;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay.Exchanges
{
    /// <summary>
    /// Gère tous les messages liés aux échanges dans Dofus :
    ///   - Craft (artisanat) : choix de la recette et du nombre d'items à fabriquer
    ///   - Hôtel des ventes (Bidhouse) : recherche, achat, mise en vente, modification de prix
    ///   - Échange entre joueurs : demande, déplacement d'items et de kamas, validation
    ///   - Boutique PNJ : acheter ou vendre des objets à un marchand PNJ
    ///
    /// Pattern commun : chaque handler vérifie d'abord le type d'échange actif du personnage
    /// via IsInExchange(ExchangeTypeEnum.X) avant de déléguer à l'objet d'échange correspondant.
    /// </summary>
    public class ExchangesHandler
    {
        /// <summary>
        /// Reçu quand le joueur choisit combien d'items fabriquer pendant un craft.
        /// Délègue au dialogue de craft actif (AbstractCraftExchange).
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeCraftCount(ExchangeCraftCountRequestMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.CRAFT))
            {
                client.Character.GetDialog<AbstractCraftExchange>().SetCount(message.count);
            }
        }

        /// <summary>
        /// Reçu quand le joueur choisit une recette dans l'interface de craft.
        /// objectGID = identifiant générique de l'objet à fabriquer (ex. l'ID du pain Bwork).
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeSetCraftRecipe(ExchangeSetCraftRecipeMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.CRAFT))
            {
                client.Character.GetDialog<CraftExchange>().SetRecipe(message.objectGID);
            }
        }

        /// <summary>
        /// Reçu quand le joueur effectue une recherche dans l'hôtel des ventes (acheteur).
        /// genId = identifiant générique de l'item recherché.
        /// </summary>
        [MessageHandler]
        public static void HandleBidHouseSearch(ExchangeBidHouseSearchMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.BIDHOUSE_BUY))
            {
                client.Character.GetDialog<BuyExchange>().ShowList(message.genId);
            }
        }

        /// <summary>
        /// Reçu quand le joueur confirme un achat dans l'hôtel des ventes.
        /// uid = identifiant unique de l'annonce, qty = quantité, price = prix total.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeBidHouseBuy(ExchangeBidHouseBuyMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.BIDHOUSE_BUY))
            {
                client.Character.GetDialog<BuyExchange>().Buy(message.uid, message.qty, message.price);
            }
        }

        /// <summary>
        /// Reçu quand le joueur clique sur un item dans la liste de l'hôtel des ventes.
        /// Affiche les différentes annonces disponibles pour cet item.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeBidhouseList(ExchangeBidHouseListMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.BIDHOUSE_BUY))
            {
                client.Character.GetDialog<BuyExchange>().ShowList(message.id);
            }
        }

        /// <summary>
        /// Reçu quand le joueur filtre par type d'objet dans l'hôtel des ventes.
        /// Affiche les catégories disponibles (armes, équipements, consommables...).
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeBidhouseTypes(ExchangeBidHouseTypeMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.BIDHOUSE_BUY))
            {
                client.Character.GetDialog<BuyExchange>().ShowTypes(message.type);
            }
        }

        /// <summary>
        /// Reçu quand le vendeur modifie le prix d'une annonce déjà posée dans l'hôtel des ventes.
        /// objectUID = identifiant unique de l'item mis en vente.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeObjectModifyPriced(ExchangeObjectModifyPricedMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.BIDHOUSE_SELL))
            {
                client.Character.GetDialog<SellExchange>().ModifyItemPriced(message.objectUID, message.quantity, message.price);
            }
        }

        /// <summary>
        /// Reçu quand le vendeur dépose un item dans l'hôtel des ventes avec un prix.
        /// Crée une nouvelle annonce dans la SellExchange.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeObjectMovePriced(ExchangeObjectMovePricedMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.BIDHOUSE_SELL))
            {
                client.Character.GetDialog<SellExchange>().MoveItemPriced(message.objectUID, message.quantity, message.price);
            }
        }

        /// <summary>
        /// Reçu quand un joueur déplace un item dans la fenêtre d'échange entre joueurs.
        /// Peut être un ajout ou un retrait selon la quantité (positive = ajout, négative = retrait).
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeObjectMove(ExchangeObjectMoveMessage message, WorldClient client)
        {
            client.Character.GetDialog<Exchange>().MoveItem(message.objectUID, message.quantity);
        }

        /// <summary>
        /// Reçu quand un joueur clique sur "Valider" ou "Annuler" dans l'échange entre joueurs.
        /// step = étape de l'échange (1 = prêt, 2 = confirmation finale).
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeReady(ExchangeReadyMessage message, WorldClient client)
        {
            if (client.Character.GetDialog<Exchange>() != null)
                client.Character.GetDialog<Exchange>().Ready(message.ready, message.step);
        }

        /// <summary>
        /// Reçu quand un joueur déplace des kamas dans l'échange entre joueurs.
        /// Vérifie que le joueur possède bien la somme avant de la déplacer.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeObjectMoveKamas(ExchangeObjectMoveKamaMessage message, WorldClient client)
        {
            // Vérifie que le joueur a suffisamment de kamas avant de les mettre dans l'échange
            if (client.Character.Record.Kamas >= message.quantity && client.Character.GetDialog<Exchange>() != null)
            {
                client.Character.GetDialog<Exchange>().MoveKamas(message.quantity);
            }
        }

        /// <summary>
        /// Reçu quand un joueur demande à initier un échange avec un autre joueur.
        /// Effectue plusieurs validations (cible présente, pas occupée, même map, échanges autorisés)
        /// avant d'ouvrir la boîte de dialogue de demande d'échange.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangePlayerRequest(ExchangePlayerRequestMessage message, WorldClient client)
        {
            // Vérifie que la cible est bien sur la map du joueur
            Character target = client.Character.Map.Instance.GetEntity<Character>((long)message.target);

            if (target == null)
            {
                client.Character.OnExchangeError(ExchangeErrorEnum.REQUEST_IMPOSSIBLE);
                return;
            }
            // Vérifie que la cible n'est pas déjà en train d'échanger ou en combat
            if (target.Busy)
            {
                client.Character.OnExchangeError(ExchangeErrorEnum.REQUEST_CHARACTER_OCCUPIED);
                return;
            }
            // Vérifie que les deux joueurs sont bien sur la même carte
            if (target.Map == null || target.Record.MapId != client.Character.Record.MapId)
            {
                client.Character.OnExchangeError(ExchangeErrorEnum.REQUEST_IMPOSSIBLE);
                return;
            }
            // Vérifie que la sous-zone autorise les échanges entre joueurs
            if (!target.Map.Position.AllowExchangesBetweenPlayers)
            {
                client.Character.OnExchangeError(ExchangeErrorEnum.REQUEST_IMPOSSIBLE);
                return;
            }

            switch ((ExchangeTypeEnum)message.exchangeType)
            {
                case ExchangeTypeEnum.PLAYER_TRADE:
                    // Ouvre une boîte de dialogue sur la cible pour qu'elle accepte ou refuse
                    target.OpenRequestBox(new PlayerTradeRequest(client.Character, target));
                    break;
                default:
                    client.Send(new ExchangeErrorMessage((sbyte)ExchangeErrorEnum.REQUEST_IMPOSSIBLE));
                    break;

            }
        }

        /// <summary>
        /// Reçu quand la cible accepte une demande d'échange entre joueurs.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeAccept(ExchangeAcceptMessage message, WorldClient client)
        {
            if (client.Character.RequestBox is PlayerTradeRequest)
                client.Character.RequestBox.Accept();
        }

        /// <summary>
        /// Reçu quand le joueur achète un item dans la boutique d'un PNJ marchand.
        /// objectToBuyId = GID de l'item à acheter, quantity = quantité souhaitée.
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeBuy(ExchangeBuyMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.NPC_SHOP))
            {
                client.Character.GetDialog<NpcShopExchange>().Buy((ushort)message.objectToBuyId, message.quantity);
            }
        }

        /// <summary>
        /// Reçu quand le joueur vend un item à un PNJ marchand.
        /// objectToSellId = UID de l'item à vendre (instance concrète dans l'inventaire).
        /// </summary>
        [MessageHandler]
        public static void HandleExchangeSell(ExchangeSellMessage message, WorldClient client)
        {
            if (client.Character.IsInExchange(ExchangeTypeEnum.NPC_SHOP))
            {
                client.Character.GetDialog<NpcShopExchange>().Sell(message.objectToSellId, message.quantity);
            }
        }
    }
}
