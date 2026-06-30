using FengZhi.Foundation.Misunderstanding;
using Xunit;

namespace FengZhi.Tests.Unit.Misunderstanding;

public class MisunderstandingDataModelTest
{
    [Fact]
    public void Instance_FullFieldAssignment_ReadsBackCorrectly()
    {
        var inst = new MisunderstandingInstance
        {
            Id = "mis_test_01",
            TargetNpc = "bailing",
            SourceType = SourceType.JianghuEvent,
            Severity = Severity.Minor,
            Transparency = Transparency.Hidden,
            InitialWindow = 7,
            WindowRemaining = 7,
            EscalationCount = 0,
            CreatedChapter = 0,
            CreatedDay = 5,
            ResolutionConditions = ["visit_bailing", "bring_evidence"],
        };

        Assert.Equal("mis_test_01", inst.Id);
        Assert.Equal("bailing", inst.TargetNpc);
        Assert.Equal(SourceType.JianghuEvent, inst.SourceType);
        Assert.Equal(Severity.Minor, inst.Severity);
        Assert.Equal(Transparency.Hidden, inst.Transparency);
        Assert.Equal(7, inst.InitialWindow);
        Assert.Equal(7, inst.WindowRemaining);
        Assert.Equal(0, inst.EscalationCount);
        Assert.Equal(0, inst.CreatedChapter);
        Assert.Equal(5, inst.CreatedDay);
        Assert.Equal(MisunderstandingState.Dormant, inst.State);
        Assert.Equal(2, inst.ResolutionConditions.Count);
    }

    [Theory]
    [InlineData(Severity.Minor, -1)]
    [InlineData(Severity.Moderate, -2)]
    [InlineData(Severity.Severe, -2)]
    public void SeverityToMod_MapsCorrectly(Severity severity, int expected)
    {
        Assert.Equal(expected, MisunderstandingInstance.SeverityToMod(severity));
    }

    [Fact]
    public void Enums_HaveExpectedValues()
    {
        Assert.Equal(3, Enum.GetValues<SourceType>().Length);
        Assert.Equal(3, Enum.GetValues<Severity>().Length);
        Assert.Equal(4, Enum.GetValues<Transparency>().Length);
        Assert.Equal(6, Enum.GetValues<MisunderstandingState>().Length);
    }
}
