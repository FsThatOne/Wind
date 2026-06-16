using FengZhi.Foundation.Mindset;
using Xunit;

namespace FengZhi.Tests.Foundation.Mindset;

public class MindsetPresentationServiceTest
{
    [Fact]
    public void GetResolveDescription_UsesLiteraryTextWithoutNumbers()
    {
        var state = MindsetState.FromValues(20, 0, 0, 0);

        var description = MindsetPresentationService.GetResolveDescription(state);

        Assert.Contains("往事", description);
        Assert.DoesNotContain("20", description);
        Assert.DoesNotContain("+", description);
    }

    [Fact]
    public void GetWorldlyDescription_UsesLiteraryTextWithoutNumbers()
    {
        var state = MindsetState.FromValues(0, -20, 0, 0);

        var description = MindsetPresentationService.GetWorldlyDescription(state);

        Assert.Contains("人间", description);
        Assert.DoesNotContain("-20", description);
        Assert.DoesNotContain("-", description);
    }

    [Fact]
    public void GetVisualParams_ReturnsWarmMutedParamsForDaYinYuShi()
    {
        var visual = MindsetPresentationService.GetVisualParams(MindsetZone.DaYinYuShi);

        Assert.Equal("warm-muted", visual.Tone);
        Assert.True(visual.Warmth > 0);
        Assert.True(visual.Saturation < 0);
    }

    [Fact]
    public void GetVisualParams_ReturnsColdSaturatedParamsForGuJianRuShi()
    {
        var visual = MindsetPresentationService.GetVisualParams(MindsetZone.GuJianRuShi);

        Assert.Equal("cold-saturated", visual.Tone);
        Assert.True(visual.Warmth < 0);
        Assert.True(visual.Saturation > 0);
    }
}
