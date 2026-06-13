using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.Items;

public enum AlchemyQuality
{
    Extreme,
    High,
    Middle,
    Low
}

public sealed record CraftedEffect(
    string Type,
    string Target,
    int Value);

public sealed record CraftingOperationResult(
    bool Success,
    string? ErrorCode,
    string? ItemId,
    int QuantityChanged,
    int SilverChanged,
    IReadOnlyList<CraftedEffect> Effects,
    InventoryEquipmentItem? Equipment)
{
    public static CraftingOperationResult Ok(
        string? itemId,
        int quantityChanged,
        int silverChanged,
        IReadOnlyList<CraftedEffect>? effects = null,
        InventoryEquipmentItem? equipment = null)
    {
        return new CraftingOperationResult(
            true,
            null,
            itemId,
            quantityChanged,
            silverChanged,
            effects ?? Array.Empty<CraftedEffect>(),
            equipment);
    }

    public static CraftingOperationResult Fail(string errorCode)
    {
        return new CraftingOperationResult(
            false,
            errorCode,
            null,
            0,
            0,
            Array.Empty<CraftedEffect>(),
            null);
    }
}

public sealed record RefinePlan(
    IReadOnlyDictionary<string, int> AttributeIncrements,
    int MaxRefineCount = CraftingService.DefaultRefineMaxCount,
    decimal CapRatio = CraftingService.DefaultRefineCapRatio);

/// <summary>
/// Runs alchemy, forging and equipment refining rules against inventory state.
/// </summary>
public sealed class CraftingService
{
    public const int DefaultRefineMaxCount = 3;
    public const decimal DefaultRefineCapRatio = 0.95m;

    private readonly IDataTable<RecipeDefinition> _recipes;
    private readonly IDataTable<ConsumableDefinition> _consumables;
    private readonly InventoryService _inventory;

    public CraftingService(DataRegistry registry, InventoryService inventory, int initialSilver = 0)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _recipes = registry.GetTable<RecipeDefinition>()
            ?? throw new ArgumentException("RecipeDefinition table is not registered.", nameof(registry));
        _consumables = registry.GetTable<ConsumableDefinition>()
            ?? throw new ArgumentException("ConsumableDefinition table is not registered.", nameof(registry));
        Silver = Math.Max(0, initialSilver);
    }

    public int Silver { get; private set; }

    public void AddSilver(int amount)
    {
        if (amount <= 0)
            return;

        Silver += amount;
    }

    public CraftingOperationResult CraftAlchemy(string recipeId, AlchemyQuality quality)
    {
        if (!Enum.IsDefined(quality))
            return CraftingOperationResult.Fail("unknown_alchemy_quality");

        var recipeResult = GetRecipe(recipeId, RecipeKind.Alchemy);
        if (!recipeResult.Success)
            return recipeResult;

        var recipe = _recipes.Get(recipeId)!;
        var precheck = CanPay(recipe);
        if (precheck != null)
            return CraftingOperationResult.Fail(precheck);

        var output = _consumables.Get(recipe.OutputItemId);
        if (output == null)
            return CraftingOperationResult.Fail("alchemy_output_not_consumable");

        Pay(recipe);
        _inventory.GrantItem(recipe.OutputItemId, recipe.OutputQuantity, ItemGrantSource.ExplorationPickup);
        var effects = output.Effects
            .Select(effect => new CraftedEffect(
                effect.Type,
                effect.Target,
                ScaleAlchemyEffect(effect.Value, quality)))
            .ToList();

        return CraftingOperationResult.Ok(
            recipe.OutputItemId,
            recipe.OutputQuantity,
            -recipe.SilverCost,
            effects);
    }

    public CraftingOperationResult Forge(string recipeId)
    {
        var recipeResult = GetRecipe(recipeId, RecipeKind.Forge);
        if (!recipeResult.Success)
            return recipeResult;

        var recipe = _recipes.Get(recipeId)!;
        var precheck = CanPay(recipe);
        if (precheck != null)
            return CraftingOperationResult.Fail(precheck);

        Pay(recipe);
        var before = _inventory.Equipment.Count;
        _inventory.GrantItem(recipe.OutputItemId, recipe.OutputQuantity, ItemGrantSource.ExplorationPickup);
        var equipment = _inventory.Equipment.Skip(before).LastOrDefault();

        return CraftingOperationResult.Ok(
            recipe.OutputItemId,
            recipe.OutputQuantity,
            -recipe.SilverCost,
            equipment: equipment);
    }

    public CraftingOperationResult RefineEquipment(string instanceId, string recipeId, RefinePlan plan)
    {
        var recipeResult = GetRecipe(recipeId, RecipeKind.Refine);
        if (!recipeResult.Success)
            return recipeResult;
        if (plan.AttributeIncrements.Count == 0)
            return CraftingOperationResult.Fail("refine_plan_empty");

        var equipment = _inventory.GetEquipment(instanceId);
        if (equipment == null)
            return CraftingOperationResult.Fail("equipment_instance_not_found");

        var instance = equipment.Instance;
        if (instance.RefineCount >= plan.MaxRefineCount)
            return CraftingOperationResult.Fail("refine_max_count_reached");

        var refinedAttributes = ApplyRefineCap(instance, plan, out var changed);
        if (!changed)
            return CraftingOperationResult.Fail("refine_cap_reached");

        var recipe = _recipes.Get(recipeId)!;
        var precheck = CanPay(recipe);
        if (precheck != null)
            return CraftingOperationResult.Fail(precheck);

        Pay(recipe);
        var refined = instance with
        {
            BaseAttributes = refinedAttributes,
            RefineCount = instance.RefineCount + 1
        };
        _inventory.ReplaceEquipmentInstance(refined);

        return CraftingOperationResult.Ok(
            instance.TemplateId,
            0,
            -recipe.SilverCost,
            equipment: _inventory.GetEquipment(instanceId));
    }

    public static int ScaleAlchemyEffect(int baseValue, AlchemyQuality quality)
    {
        var multiplier = quality switch
        {
            AlchemyQuality.Extreme => 1.2m,
            AlchemyQuality.High => 1.0m,
            AlchemyQuality.Middle => 0.8m,
            AlchemyQuality.Low => 0.6m,
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Unknown alchemy quality.")
        };

        return (int)Math.Round(baseValue * multiplier, MidpointRounding.AwayFromZero);
    }

    private CraftingOperationResult GetRecipe(string recipeId, RecipeKind expectedKind)
    {
        var recipe = _recipes.Get(recipeId);
        if (recipe == null)
            return CraftingOperationResult.Fail("recipe_not_found");
        if (recipe.Kind != expectedKind)
            return CraftingOperationResult.Fail("recipe_kind_mismatch");

        return CraftingOperationResult.Ok(recipe.OutputItemId, 0, 0);
    }

    private string? CanPay(RecipeDefinition recipe)
    {
        if (Silver < recipe.SilverCost)
            return "not_enough_silver";

        foreach (var ingredient in recipe.Ingredients)
        {
            if (_inventory.GetTotalQuantity(ingredient.ItemId) < ingredient.Quantity)
                return "not_enough_material";
        }

        return null;
    }

    private void Pay(RecipeDefinition recipe)
    {
        Silver -= recipe.SilverCost;
        foreach (var ingredient in recipe.Ingredients)
            _inventory.DiscardItem(ingredient.ItemId, ingredient.Quantity);
    }

    private static IReadOnlyDictionary<string, int> ApplyRefineCap(
        GeneratedEquipmentInstance instance,
        RefinePlan plan,
        out bool changed)
    {
        changed = false;
        var cap = (int)Math.Floor(EquipmentService.GetGradeAttributeCap(instance.Grade) * plan.CapRatio);
        var attributes = instance.BaseAttributes.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        foreach (var (stat, increment) in plan.AttributeIncrements)
        {
            if (increment <= 0)
                continue;

            var current = attributes.GetValueOrDefault(stat);
            var next = Math.Min(current + increment, cap);
            if (next > current)
                changed = true;
            attributes[stat] = next;
        }

        return attributes;
    }
}
