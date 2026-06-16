using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.Items;

/// <summary>
/// Input row for black market barter offers.
/// </summary>
public sealed record BarterRequirement(string ItemId, int Quantity);

/// <summary>
/// Deterministic black market offer: exact items in, exact item out.
/// </summary>
public sealed record BlackMarketBarterOffer(
    string OfferId,
    IReadOnlyList<BarterRequirement> Requirements,
    string OutputItemId,
    int OutputQuantity);

/// <summary>
/// Runtime auction lot state.
/// </summary>
public sealed record AuctionLot(
    string LotId,
    string ItemId,
    int Quantity,
    int CurrentHighestBid,
    string? HighestBidderId,
    bool IsClosed);

/// <summary>
/// NPC auction decision result.
/// </summary>
public sealed record AuctionNpcBidDecision(bool WillFollow, int NextBid);

/// <summary>
/// Injectable strategy for deterministic auction tests and future NPC bidding rules.
/// </summary>
public interface IAuctionNpcBidStrategy
{
    AuctionNpcBidDecision Decide(AuctionLot lot, int playerBid);
}

/// <summary>
/// Default NPC auction strategy: follow with a small deterministic increment.
/// </summary>
public sealed class DefaultAuctionNpcBidStrategy : IAuctionNpcBidStrategy
{
    public AuctionNpcBidDecision Decide(AuctionLot lot, int playerBid)
    {
        return new AuctionNpcBidDecision(true, playerBid + Math.Max(1, playerBid / 10));
    }
}

/// <summary>
/// Structured result for economy operations.
/// </summary>
public sealed record EconomyOperationResult(
    bool Success,
    string? ErrorCode,
    string? ItemId,
    int QuantityChanged,
    int SilverChanged,
    AuctionLot? AuctionLot)
{
    public static EconomyOperationResult Ok(
        string? itemId = null,
        int quantityChanged = 0,
        int silverChanged = 0,
        AuctionLot? auctionLot = null)
    {
        return new EconomyOperationResult(true, null, itemId, quantityChanged, silverChanged, auctionLot);
    }

    public static EconomyOperationResult Fail(string errorCode, string? itemId = null, AuctionLot? auctionLot = null)
    {
        return new EconomyOperationResult(false, errorCode, itemId, 0, 0, auctionLot);
    }
}

/// <summary>
/// Handles silver, shop transactions, black market barter and deterministic auction settlement.
/// </summary>
public sealed class EconomyService
{
    public const decimal DefaultSellRatio = 0.3m;

    private const string PlayerBidderId = "player";

    private readonly IDataTable<ConsumableDefinition> _consumables;
    private readonly IDataTable<EquipmentTemplateDefinition> _equipmentTemplates;
    private readonly IDataTable<KeyItemDefinition> _keyItems;
    private readonly InventoryService _inventory;
    private readonly IAuctionNpcBidStrategy _auctionNpcBidStrategy;
    private readonly Dictionary<string, AuctionLot> _auctionLots = new(StringComparer.Ordinal);

    public EconomyService(
        DataRegistry registry,
        InventoryService inventory,
        int initialSilver = 0,
        IAuctionNpcBidStrategy? auctionNpcBidStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _consumables = registry.GetTable<ConsumableDefinition>()
            ?? throw new ArgumentException("ConsumableDefinition table is not registered.", nameof(registry));
        _equipmentTemplates = registry.GetTable<EquipmentTemplateDefinition>()
            ?? throw new ArgumentException("EquipmentTemplateDefinition table is not registered.", nameof(registry));
        _keyItems = registry.GetTable<KeyItemDefinition>()
            ?? throw new ArgumentException("KeyItemDefinition table is not registered.", nameof(registry));
        _auctionNpcBidStrategy = auctionNpcBidStrategy ?? new DefaultAuctionNpcBidStrategy();
        Silver = Math.Max(0, initialSilver);
    }

    public int Silver { get; private set; }

    public IReadOnlyCollection<AuctionLot> AuctionLots => _auctionLots.Values.ToList();

    public void AddSilver(int amount)
    {
        if (amount <= 0)
            return;

        Silver += amount;
    }

    public ItemEconomySaveData CreateSaveData()
    {
        return new ItemEconomySaveData { Silver = Silver };
    }

    public IReadOnlyList<ItemRestoreWarning> RestoreSaveData(ItemEconomySaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Silver = Math.Max(0, data.Silver);
        return data.Silver < 0
            ? new[] { new ItemRestoreWarning("negative_silver_clamped", "Restored silver was negative and has been clamped to zero.") }
            : Array.Empty<ItemRestoreWarning>();
    }

    public bool CanEnterAuction(string itemId)
    {
        return _inventory.CanEnterAuction(itemId);
    }

    public int? GetShopBuyPrice(string itemId)
    {
        return GetBasePrice(itemId);
    }

    public int? GetShopSellPrice(string itemId, decimal sellRatio = DefaultSellRatio)
    {
        var basePrice = GetBasePrice(itemId);
        if (basePrice == null)
            return null;

        var ratio = Math.Max(0m, sellRatio);
        return (int)Math.Floor(basePrice.Value * ratio);
    }

    public EconomyOperationResult BuyFromShop(string itemId, int quantity)
    {
        if (quantity <= 0)
            return EconomyOperationResult.Fail("quantity_must_be_positive", itemId);

        var unitPrice = GetShopBuyPrice(itemId);
        if (unitPrice == null)
            return EconomyOperationResult.Fail("item_not_shop_buyable", itemId);

        var totalPrice = unitPrice.Value * quantity;
        if (Silver < totalPrice)
            return EconomyOperationResult.Fail("not_enough_silver", itemId);

        var grant = _inventory.GrantItem(itemId, quantity, ItemGrantSource.ExplorationPickup);
        if (!grant.Success)
            return EconomyOperationResult.Fail(grant.ErrorCode ?? "inventory_grant_failed", itemId);

        Silver -= totalPrice;
        return EconomyOperationResult.Ok(itemId, quantity, -totalPrice);
    }

    public EconomyOperationResult SellToShop(string itemId, int quantity, decimal sellRatio = DefaultSellRatio)
    {
        if (quantity <= 0)
            return EconomyOperationResult.Fail("quantity_must_be_positive", itemId);
        if (_keyItems.Get(itemId) != null)
            return EconomyOperationResult.Fail("key_item_cannot_be_sold", itemId);
        if (_inventory.GetTotalQuantity(itemId) < quantity)
            return EconomyOperationResult.Fail("not_enough_quantity", itemId);

        var unitPrice = GetShopSellPrice(itemId, sellRatio);
        if (unitPrice == null)
            return EconomyOperationResult.Fail("item_not_shop_sellable", itemId);

        var sell = _inventory.SellItem(itemId, quantity);
        if (!sell.Success)
            return EconomyOperationResult.Fail(sell.ErrorCode ?? "inventory_sell_failed", itemId);

        var gained = unitPrice.Value * quantity;
        Silver += gained;
        return EconomyOperationResult.Ok(itemId, -quantity, gained);
    }

    public EconomyOperationResult ExecuteBlackMarketBarter(BlackMarketBarterOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);
        if (string.IsNullOrWhiteSpace(offer.OfferId))
            return EconomyOperationResult.Fail("barter_offer_id_required");
        if (offer.OutputQuantity <= 0)
            return EconomyOperationResult.Fail("quantity_must_be_positive", offer.OutputItemId);
        if (offer.Requirements.Count == 0)
            return EconomyOperationResult.Fail("barter_requirements_empty", offer.OutputItemId);
        // 输出物必须可被授予：限定为消耗品或装备模板，禁止未知物品和关键物品作为产出。
        if (_consumables.Get(offer.OutputItemId) == null
            && _equipmentTemplates.Get(offer.OutputItemId) == null)
            return EconomyOperationResult.Fail("unknown_item_id", offer.OutputItemId);

        foreach (var requirement in offer.Requirements)
        {
            if (requirement.Quantity <= 0)
                return EconomyOperationResult.Fail("quantity_must_be_positive", requirement.ItemId);
            if (_keyItems.Get(requirement.ItemId) != null)
                return EconomyOperationResult.Fail("key_item_cannot_be_bartered", requirement.ItemId);
            if (_inventory.GetTotalQuantity(requirement.ItemId) < requirement.Quantity)
                return EconomyOperationResult.Fail("not_enough_barter_items", requirement.ItemId);
        }

        // 预校验已保证下面的 Discard/Grant 必成功：任何失败均代表数据/契约破坏，立刻 fail-fast 暴露。
        foreach (var requirement in offer.Requirements)
        {
            var discard = _inventory.DiscardItem(requirement.ItemId, requirement.Quantity);
            if (!discard.Success)
                throw new InvalidOperationException(
                    $"Black market barter discard failed after pre-validation: requirement={requirement.ItemId}, code={discard.ErrorCode}");
        }

        var grant = _inventory.GrantItem(offer.OutputItemId, offer.OutputQuantity, ItemGrantSource.ExplorationPickup);
        if (!grant.Success)
            throw new InvalidOperationException(
                $"Black market barter grant failed after pre-validation: output={offer.OutputItemId}, code={grant.ErrorCode}");

        return EconomyOperationResult.Ok(offer.OutputItemId, offer.OutputQuantity);
    }

    public EconomyOperationResult OpenAuctionLot(string lotId, string itemId, int quantity, int startingBid)
    {
        if (string.IsNullOrWhiteSpace(lotId))
            return EconomyOperationResult.Fail("auction_lot_id_required", itemId);
        if (quantity <= 0)
            return EconomyOperationResult.Fail("quantity_must_be_positive", itemId);
        if (startingBid < 0)
            return EconomyOperationResult.Fail("auction_starting_bid_negative", itemId);
        if (_auctionLots.ContainsKey(lotId))
            return EconomyOperationResult.Fail("auction_lot_exists", itemId, _auctionLots[lotId]);
        if (GetBasePrice(itemId) == null)
            return EconomyOperationResult.Fail("unknown_item_id", itemId);

        var keyItem = _keyItems.Get(itemId);
        if (keyItem != null && !keyItem.Auctionable)
            return EconomyOperationResult.Fail("key_item_not_auctionable", itemId);

        var lot = new AuctionLot(lotId, itemId, quantity, startingBid, null, IsClosed: false);
        _auctionLots[lotId] = lot;
        return EconomyOperationResult.Ok(itemId, auctionLot: lot);
    }

    public AuctionLot? GetAuctionLot(string lotId)
    {
        return _auctionLots.GetValueOrDefault(lotId);
    }

    public EconomyOperationResult SubmitAuctionBid(string lotId, int bid)
    {
        if (!_auctionLots.TryGetValue(lotId, out var lot))
            return EconomyOperationResult.Fail("auction_lot_not_found");
        if (lot.IsClosed)
            return EconomyOperationResult.Fail("auction_lot_closed", lot.ItemId, lot);
        if (bid <= lot.CurrentHighestBid)
            return EconomyOperationResult.Fail("bid_must_exceed_current_highest", lot.ItemId, lot);
        if (Silver < bid)
            return EconomyOperationResult.Fail("not_enough_silver", lot.ItemId, lot);

        var npcDecision = _auctionNpcBidStrategy.Decide(lot, bid);
        if (npcDecision.WillFollow)
        {
            var npcBid = Math.Max(bid + 1, npcDecision.NextBid);
            var followed = lot with
            {
                CurrentHighestBid = npcBid,
                HighestBidderId = "npc"
            };
            _auctionLots[lotId] = followed;
            return EconomyOperationResult.Ok(lot.ItemId, auctionLot: followed);
        }

        var grant = _inventory.GrantItem(lot.ItemId, lot.Quantity, ItemGrantSource.NarrativeReward);
        if (!grant.Success)
            return EconomyOperationResult.Fail(grant.ErrorCode ?? "inventory_grant_failed", lot.ItemId, lot);

        Silver -= bid;
        var closed = lot with
        {
            CurrentHighestBid = bid,
            HighestBidderId = PlayerBidderId,
            IsClosed = true
        };
        _auctionLots[lotId] = closed;
        return EconomyOperationResult.Ok(lot.ItemId, lot.Quantity, -bid, closed);
    }

    private int? GetBasePrice(string itemId)
    {
        var consumable = _consumables.Get(itemId);
        if (consumable != null)
            return consumable.BasePrice;

        var equipment = _equipmentTemplates.Get(itemId);
        if (equipment != null)
            return equipment.BasePrice;

        return null;
    }
}
