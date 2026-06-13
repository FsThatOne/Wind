using FengZhi.Foundation.SceneManagement;
using FengZhi.Foundation.TimeSystem;
using Xunit;

namespace Foundation.Tests.SceneManagement;

public class ColorToneTests
{
    private const float Tolerance = 0.001f;

    // --- AC1: ChapterTone data class ---

    [Fact]
    public void ChapterTone_StoresHSV()
    {
        var tone = new ChapterTone(0.5f, 0.6f, 0.7f);
        Assert.Equal(0.5f, tone.H);
        Assert.Equal(0.6f, tone.S);
        Assert.Equal(0.7f, tone.V);
    }

    // --- AC2: SeasonOffset by sensitivity ---

    [Fact]
    public void SeasonOffset_None_ReturnsZero()
    {
        var offset = ColorToneCalculator.GetSeasonOffset(Season.Spring, SeasonSensitivity.None);
        Assert.Equal(0f, offset.H);
        Assert.Equal(0f, offset.S);
        Assert.Equal(0f, offset.V);
    }

    [Fact]
    public void SeasonOffset_Low_Spring_PositiveValues()
    {
        var offset = ColorToneCalculator.GetSeasonOffset(Season.Spring, SeasonSensitivity.Low);
        Assert.Equal(5f / 360f, offset.H, Tolerance);
        Assert.Equal(0.03f, offset.S, Tolerance);
        Assert.Equal(0.02f, offset.V, Tolerance);
    }

    [Fact]
    public void SeasonOffset_High_Winter_NegativeValues()
    {
        var offset = ColorToneCalculator.GetSeasonOffset(Season.Winter, SeasonSensitivity.High);
        Assert.Equal(-30f / 360f, offset.H, Tolerance);
        Assert.Equal(-0.15f, offset.S, Tolerance);
        Assert.Equal(-0.10f, offset.V, Tolerance);
    }

    [Fact]
    public void SeasonOffset_Medium_Autumn_NegativeValues()
    {
        var offset = ColorToneCalculator.GetSeasonOffset(Season.Autumn, SeasonSensitivity.Medium);
        Assert.Equal(-15f / 360f, offset.H, Tolerance);
        Assert.Equal(-0.08f, offset.S, Tolerance);
        Assert.Equal(-0.05f, offset.V, Tolerance);
    }

    // --- AC3: LightOffset by phase ---

    [Fact]
    public void LightOffset_Day_Zero()
    {
        var lo = ColorToneCalculator.GetLightOffset(LightPhase.Day);
        Assert.Equal(0f, lo.S);
        Assert.Equal(0f, lo.V);
    }

    [Fact]
    public void LightOffset_Dawn_Correct()
    {
        var lo = ColorToneCalculator.GetLightOffset(LightPhase.Dawn);
        Assert.Equal(-0.03f, lo.S, Tolerance);
        Assert.Equal(-0.08f, lo.V, Tolerance);
    }

    [Fact]
    public void LightOffset_Dusk_Correct()
    {
        var lo = ColorToneCalculator.GetLightOffset(LightPhase.Dusk);
        Assert.Equal(-0.03f, lo.S, Tolerance);
        Assert.Equal(-0.12f, lo.V, Tolerance);
    }

    [Fact]
    public void LightOffset_Night_UsesNightDarken()
    {
        var lo = ColorToneCalculator.GetLightOffset(LightPhase.Night, 0.25f);
        Assert.Equal(-0.05f, lo.S, Tolerance);
        Assert.Equal(-0.25f, lo.V, Tolerance);
    }

    // --- AC4: CalcFinalTone three-layer composite ---

    [Fact]
    public void CalcFinalTone_Day_NoSeason_ReturnsBase()
    {
        var chapter = new ChapterTone(0.5f, 0.6f, 0.7f);
        var result = ColorToneCalculator.CalcFinalTone(chapter, Season.Spring, SeasonSensitivity.None, LightPhase.Day);
        Assert.Equal(0.5f, result.H, Tolerance);
        Assert.Equal(0.6f, result.S, Tolerance);
        Assert.Equal(0.7f, result.V, Tolerance);
    }

    [Fact]
    public void CalcFinalTone_ClampsS_ToZero()
    {
        var chapter = new ChapterTone(0f, 0.05f, 0.5f);
        // Winter + High => S offset = -0.15, Light Night => S offset = -0.05
        // 0.05 - 0.15 - 0.05 = -0.15 -> clamped to 0
        var result = ColorToneCalculator.CalcFinalTone(chapter, Season.Winter, SeasonSensitivity.High, LightPhase.Night);
        Assert.Equal(0f, result.S, Tolerance);
    }

    [Fact]
    public void CalcFinalTone_ClampsV_ToZero()
    {
        var chapter = new ChapterTone(0f, 0.5f, 0.1f);
        // Winter + High => V offset = -0.10, Light Night (0.20) => V offset = -0.20
        // 0.1 - 0.10 - 0.20 = -0.20 -> clamped to 0
        var result = ColorToneCalculator.CalcFinalTone(chapter, Season.Winter, SeasonSensitivity.High, LightPhase.Night);
        Assert.Equal(0f, result.V, Tolerance);
    }

    [Fact]
    public void CalcFinalTone_ClampsS_ToOne()
    {
        var chapter = new ChapterTone(0f, 0.95f, 0.5f);
        // Spring + High => S offset = +0.15
        // 0.95 + 0.15 = 1.10 -> clamped to 1
        var result = ColorToneCalculator.CalcFinalTone(chapter, Season.Spring, SeasonSensitivity.High, LightPhase.Day);
        Assert.Equal(1f, result.S, Tolerance);
    }

    [Fact]
    public void CalcFinalTone_HNotClamped()
    {
        var chapter = new ChapterTone(350f / 360f, 0.5f, 0.5f);
        // Spring + High => H offset = +30/360
        // 350/360 + 30/360 = 380/360 > 1 -> not clamped (color wheel wraps)
        var result = ColorToneCalculator.CalcFinalTone(chapter, Season.Spring, SeasonSensitivity.High, LightPhase.Day);
        Assert.True(result.H > 1f); // H can exceed 1 (wraps on color wheel)
    }

    // --- AC5: Finale tone by mindset zone ---

    [Theory]
    [InlineData(MindsetZone.Release_Social)]
    [InlineData(MindsetZone.Obsession_Social)]
    [InlineData(MindsetZone.Release_Hermit)]
    [InlineData(MindsetZone.Obsession_Hermit)]
    [InlineData(MindsetZone.Demonic)]
    public void GetFinaleTone_ReturnsValidTone(MindsetZone zone)
    {
        var tone = ColorToneCalculator.GetFinaleTone(zone);
        Assert.True(tone.S >= 0 && tone.S <= 1);
        Assert.True(tone.V >= 0 && tone.V <= 1);
    }

    [Fact]
    public void GetFinaleTone_Demonic_DarkAndRed()
    {
        var tone = ColorToneCalculator.GetFinaleTone(MindsetZone.Demonic);
        Assert.True(tone.V < 0.2f); // very dark
        Assert.True(tone.S > 0.8f); // highly saturated
    }

    // --- AC6: night_darken parameter ---

    [Theory]
    [InlineData(0.15f)]
    [InlineData(0.20f)]
    [InlineData(0.25f)]
    public void NightDarken_AffectsVOffset(float nightDarken)
    {
        var chapter = new ChapterTone(0f, 0.5f, 0.8f);
        var result = ColorToneCalculator.CalcFinalTone(chapter, Season.Spring, SeasonSensitivity.None, LightPhase.Night, nightDarken);
        float expectedV = 0.8f - nightDarken;
        Assert.Equal(expectedV, result.V, Tolerance);
    }
}
