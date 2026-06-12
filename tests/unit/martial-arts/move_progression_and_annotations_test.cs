using FengZhi.Foundation.Data;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.unit.martial_arts;

public sealed class MoveProgressionAndAnnotationsTest
{
    private readonly MoveProgressionService _service;
    private readonly MoveDefinitionTable _moveTable;

    public MoveProgressionAndAnnotationsTest()
    {
        var loader = new MartialArtsConfigLoader();
        var moves = loader.LoadList<MoveDefinition>(TestMovesYaml, "test");
        _moveTable = new MoveDefinitionTable(moves);
        _service = new MoveProgressionService(_moveTable);
    }

    // === AC-1：普通武学习得即满 ===

    [Fact]
    public void LearnBasicMove_Success_CompletionIsOne()
    {
        var result = _service.LearnBasicMove("luo_han_quan");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("luo_han_quan");
        Assert.NotNull(entry);
        Assert.Equal(MoveProgressionStage.Mastered, entry!.Stage);
        Assert.Equal(1.00f, entry.Completion);
    }

    [Fact]
    public void LearnBasicMove_Duplicate_ReturnsAlreadyLearned()
    {
        _service.LearnBasicMove("luo_han_quan");

        var result = _service.LearnBasicMove("luo_han_quan");

        Assert.Equal(ProgressionResult.AlreadyLearned, result);
    }

    [Fact]
    public void LearnBasicMove_AdvancedCategory_ReturnsInvalidStage()
    {
        var result = _service.LearnBasicMove("qian_ying_zhang");

        Assert.Equal(ProgressionResult.InvalidStageForOperation, result);
    }

    [Fact]
    public void LearnBasicMove_NotFound_ReturnsMoveNotFound()
    {
        var result = _service.LearnBasicMove("nonexistent");

        Assert.Equal(ProgressionResult.MoveNotFound, result);
    }

    // === AC-2：残卷到拓本 ===

    [Fact]
    public void AddFragment_First_CreatesFragmentState()
    {
        var result = _service.AddFragment("qian_ying_zhang");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("qian_ying_zhang");
        Assert.NotNull(entry);
        Assert.Equal(MoveProgressionStage.Fragment, entry!.Stage);
        Assert.Equal(0.55f, entry.Completion);
        Assert.Equal(1, entry.FragmentCount);
    }

    [Fact]
    public void AddFragment_ThreeFragments_CanSynthesizeToManuscript()
    {
        _service.AddFragment("qian_ying_zhang");
        _service.AddFragment("qian_ying_zhang");
        _service.AddFragment("qian_ying_zhang");

        var result = _service.SynthesizeToManuscript("qian_ying_zhang");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("qian_ying_zhang")!;
        Assert.Equal(MoveProgressionStage.Manuscript, entry.Stage);
        Assert.Equal(0.72f, entry.Completion);
    }

    [Fact]
    public void SynthesizeToManuscript_TwoFragments_ReturnsInsufficientFragments()
    {
        _service.AddFragment("qian_ying_zhang");
        _service.AddFragment("qian_ying_zhang");

        var result = _service.SynthesizeToManuscript("qian_ying_zhang");

        Assert.Equal(ProgressionResult.InsufficientFragments, result);
    }

    [Fact]
    public void AddFragment_FourthFragment_ReturnsExcessFragment()
    {
        _service.AddFragment("tian_gang_lie_po");
        _service.AddFragment("tian_gang_lie_po");
        _service.AddFragment("tian_gang_lie_po");

        var result = _service.AddFragment("tian_gang_lie_po");

        Assert.Equal(ProgressionResult.ExcessFragment, result);
        var entry = _service.GetEntry("tian_gang_lie_po")!;
        Assert.Equal(1, entry.ExcessFragments);
    }

    [Fact]
    public void AddFragment_AlreadyManuscript_ReturnsExcessFragment()
    {
        _service.AddFragment("qian_ying_zhang");
        _service.AddFragment("qian_ying_zhang");
        _service.AddFragment("qian_ying_zhang");
        _service.SynthesizeToManuscript("qian_ying_zhang");

        var result = _service.AddFragment("qian_ying_zhang");

        Assert.Equal(ProgressionResult.ExcessFragment, result);
    }

    [Fact]
    public void AddFragment_BasicMove_ReturnsInvalidStage()
    {
        var result = _service.AddFragment("luo_han_quan");

        Assert.Equal(ProgressionResult.InvalidStageForOperation, result);
    }

    // === 完本/真传升级 ===

    [Fact]
    public void UpgradeToComplete_FromManuscript_UnlocksEffectV1()
    {
        SetupManuscript("qian_ying_zhang");

        var result = _service.UpgradeToComplete("qian_ying_zhang");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("qian_ying_zhang")!;
        Assert.Equal(MoveProgressionStage.Complete, entry.Stage);
        Assert.Equal(0.88f, entry.Completion);
        Assert.True(entry.UnlockedEffects.HasFlag(EffectUnlockFlags.EffectV1));
        Assert.False(entry.UnlockedEffects.HasFlag(EffectUnlockFlags.EffectV2));
    }

    [Fact]
    public void UpgradeToMastered_FromComplete_UnlocksEffectV2()
    {
        SetupComplete("tian_gang_lie_po");

        var result = _service.UpgradeToMastered("tian_gang_lie_po");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("tian_gang_lie_po")!;
        Assert.Equal(MoveProgressionStage.Mastered, entry.Stage);
        Assert.Equal(1.00f, entry.Completion);
        Assert.True(entry.UnlockedEffects.HasFlag(EffectUnlockFlags.EffectV1));
        Assert.True(entry.UnlockedEffects.HasFlag(EffectUnlockFlags.EffectV2));
    }

    [Fact]
    public void UpgradeToComplete_FromFragment_ReturnsInvalidStage()
    {
        _service.AddFragment("qian_ying_zhang");

        var result = _service.UpgradeToComplete("qian_ying_zhang");

        Assert.Equal(ProgressionResult.InvalidStageForOperation, result);
    }

    [Fact]
    public void UpgradeToMastered_FromManuscript_ReturnsInvalidStage()
    {
        SetupManuscript("qian_ying_zhang");

        var result = _service.UpgradeToMastered("qian_ying_zhang");

        Assert.Equal(ProgressionResult.InvalidStageForOperation, result);
    }

    // === AC-3：批注版覆盖 ===

    [Fact]
    public void ApplyAnnotation_FromComplete_OverridesToAnnotated()
    {
        SetupComplete("tian_gang_lie_po");

        var result = _service.ApplyAnnotation("tian_gang_lie_po");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("tian_gang_lie_po")!;
        Assert.Equal(MoveProgressionStage.Annotated, entry.Stage);
        Assert.Equal(1.00f, entry.Completion);
        Assert.True(entry.UnlockedEffects.HasFlag(EffectUnlockFlags.EffectV1));
        Assert.True(entry.UnlockedEffects.HasFlag(EffectUnlockFlags.EffectV2));
    }

    [Fact]
    public void ApplyAnnotation_NotLearned_CreatesAnnotatedDirectly()
    {
        var result = _service.ApplyAnnotation("luo_han_quan");

        Assert.Equal(ProgressionResult.Success, result);
        var entry = _service.GetEntry("luo_han_quan")!;
        Assert.Equal(MoveProgressionStage.Annotated, entry.Stage);
        Assert.Equal(1.00f, entry.Completion);
    }

    [Fact]
    public void ApplyAnnotation_NotAnnotatable_ReturnsNotAnnotatable()
    {
        _service.AddFragment("qian_ying_zhang");

        var result = _service.ApplyAnnotation("qian_ying_zhang");

        Assert.Equal(ProgressionResult.NotAnnotatable, result);
    }

    [Fact]
    public void ApplyAnnotation_AlreadyAnnotated_ReturnsAlreadyTerminal()
    {
        _service.ApplyAnnotation("luo_han_quan");

        var result = _service.ApplyAnnotation("luo_han_quan");

        Assert.Equal(ProgressionResult.AlreadyTerminal, result);
    }

    [Fact]
    public void ApplyAnnotation_MoveNotFound_ReturnsMoveNotFound()
    {
        var result = _service.ApplyAnnotation("nonexistent");

        Assert.Equal(ProgressionResult.MoveNotFound, result);
    }

    // === 静态配置验证 ===

    [Fact]
    public void AnnotationDefinition_LoadedFromYaml_HasCorrectComponents()
    {
        var luohan = _moveTable.Get("luo_han_quan")!;

        Assert.True(luohan.Annotatable);
        Assert.NotNull(luohan.Annotation);
        Assert.Equal("first_counter_hit", luohan.Annotation!.ConditionOverride);
        Assert.Equal("flaw_expose_3", luohan.Annotation.EffectOverride);
        Assert.Equal(1.8f, luohan.Annotation.AnnotationMult, precision: 2);
    }

    [Fact]
    public void AnnotationDefinition_NotAnnotatable_HasNoAnnotation()
    {
        var qianying = _moveTable.Get("qian_ying_zhang")!;

        Assert.False(qianying.Annotatable);
        Assert.Null(qianying.Annotation);
    }

    [Fact]
    public void EffectiveBaseMultiplier_AnnotatedMove_EqualsBaseTimesAnnotationMult()
    {
        var luohan = _moveTable.Get("luo_han_quan")!;

        float effective = luohan.BaseMultiplier * luohan.Annotation!.AnnotationMult;

        Assert.Equal(1.26f, effective, precision: 2);
    }

    // === Helpers ===

    private void SetupManuscript(string moveId)
    {
        _service.AddFragment(moveId);
        _service.AddFragment(moveId);
        _service.AddFragment(moveId);
        _service.SynthesizeToManuscript(moveId);
    }

    private void SetupComplete(string moveId)
    {
        SetupManuscript(moveId);
        _service.UpgradeToComplete(moveId);
    }

    // === Test Data ===

    private const string TestMovesYaml = """
    - id: luo_han_quan
      name: 罗汉拳
      type: gang
      category: basic
      neixi_cost: 2
      base_multiplier: 0.70
      trigger_conditions: [always]
      special_effects: [flaw_expose]
      tags: [direct, martial]
      annotatable: true
      annotation:
        condition_override: first_counter_hit
        effect_override: flaw_expose_3
        annotation_mult: 1.8

    - id: qian_ying_zhang
      name: 千影掌
      type: qiao
      category: advanced
      neixi_cost: 5
      base_multiplier: 1.10
      trigger_conditions: [second_counter_hit]
      special_effects: [multi_strike]
      tags: [combo, precise]
      annotatable: false

    - id: tian_gang_lie_po
      name: 天罡裂魄
      type: gang
      category: ultimate
      neixi_cost: 8
      base_multiplier: 1.40
      trigger_conditions: [no_condition]
      special_effects: [shatter_guard, devastate]
      tags: [direct, finishing]
      annotatable: true
      annotation:
        condition_override: no_condition
        effect_override: shatter_guard_enhanced
        annotation_mult: 1.2
    """;
}
