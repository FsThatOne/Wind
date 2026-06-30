namespace FengZhi.Foundation.Misunderstanding;

/// <summary>误会触发来源类型。</summary>
public enum SourceType
{
    JianghuEvent,
    DialogueChoice,
    Absence,
}

/// <summary>误会严重度等级。</summary>
public enum Severity
{
    Minor,
    Moderate,
    Severe,
}

/// <summary>误会透明度阶段。</summary>
public enum Transparency
{
    Hidden,
    Hinted,
    Perceived,
    Urgent,
}

/// <summary>误会状态机状态。</summary>
public enum MisunderstandingState
{
    Dormant,
    Active,
    Escalated,
    Resolved,
    Permanent,
    Broken,
}
