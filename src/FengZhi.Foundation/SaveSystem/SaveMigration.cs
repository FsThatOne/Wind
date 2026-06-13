namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 存档 schema 迁移链。迁移必须按 vN -> vN+1 单步执行，不允许跳版本。
/// </summary>
public sealed partial class MigrationChain
{
    /// <summary>ADR-0004 要求的单次迁移超时。</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);

    private readonly SortedDictionary<int, IMigration> _migrations = new();
    private readonly TimeSpan _timeout;

    /// <summary>创建迁移链。</summary>
    public MigrationChain(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? DefaultTimeout;
    }

    /// <summary>注册一个单步迁移。</summary>
    public SaveResult Register(IMigration migration)
    {
        if (migration == null)
            throw new ArgumentNullException(nameof(migration));

        if (migration.ToVersion != migration.FromVersion + 1)
            return SaveResult.Fail($"迁移必须单步递增：v{migration.FromVersion} -> v{migration.ToVersion}");

        if (_migrations.ContainsKey(migration.FromVersion))
            return SaveResult.Fail($"重复的迁移起点：v{migration.FromVersion}");

        _migrations.Add(migration.FromVersion, migration);
        return SaveResult.Ok();
    }

    /// <summary>将 payload 从自身版本迁移到目标版本。</summary>
    public async Task<SaveMigrationResult> ApplyAsync(
        SavePayload payload,
        int targetVersion,
        CancellationToken cancellationToken = default)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        if (payload.SchemaVersion > targetVersion)
            return SaveMigrationResult.Fail($"存档版本 v{payload.SchemaVersion} 高于当前支持版本 v{targetVersion}。");

        var current = payload;
        var applied = new List<int>();

        while (current.SchemaVersion < targetVersion)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_migrations.TryGetValue(current.SchemaVersion, out var migration))
                return SaveMigrationResult.Fail($"缺少迁移：v{current.SchemaVersion} -> v{current.SchemaVersion + 1}");

            var stepResult = await RunStepWithTimeoutAsync(migration, current, cancellationToken).ConfigureAwait(false);
            if (!stepResult.Success || stepResult.Payload == null)
                return SaveMigrationResult.Fail(stepResult.Error ?? "迁移失败。", applied, stepResult.TimedOut);

            current = stepResult.Payload with { SchemaVersion = migration.ToVersion };
            applied.Add(migration.FromVersion);
        }

        return SaveMigrationResult.Ok(current, applied);
    }

    private async Task<SaveMigrationResult> RunStepWithTimeoutAsync(
        IMigration migration,
        SavePayload payload,
        CancellationToken cancellationToken)
    {
        var migrationTask = Task.Run(() => migration.Migrate(payload), cancellationToken);
        var timeoutTask = Task.Delay(_timeout, cancellationToken);
        var completed = await Task.WhenAny(migrationTask, timeoutTask).ConfigureAwait(false);

        if (completed == timeoutTask)
            return SaveMigrationResult.Fail($"迁移超时：v{migration.FromVersion} -> v{migration.ToVersion}", timedOut: true);

        try
        {
            return SaveMigrationResult.Ok(await migrationTask.ConfigureAwait(false), new[] { migration.FromVersion });
        }
        catch (Exception ex)
        {
            return SaveMigrationResult.Fail($"迁移失败：{ex.Message}");
        }
    }
}

/// <summary>
/// 迁移链执行结果。
/// </summary>
public sealed partial record SaveMigrationResult
{
    /// <summary>是否成功。</summary>
    public bool Success { get; init; }

    /// <summary>失败原因。</summary>
    public string? Error { get; init; }

    /// <summary>迁移后的 payload。</summary>
    public SavePayload? Payload { get; init; }

    /// <summary>已执行的迁移起点版本。</summary>
    public IReadOnlyList<int> AppliedFromVersions { get; init; } = Array.Empty<int>();

    /// <summary>是否因超时失败。</summary>
    public bool TimedOut { get; init; }

    /// <summary>成功结果。</summary>
    public static SaveMigrationResult Ok(SavePayload payload, IReadOnlyList<int> appliedFromVersions)
    {
        return new SaveMigrationResult
        {
            Success = true,
            Payload = payload,
            AppliedFromVersions = appliedFromVersions
        };
    }

    /// <summary>失败结果。</summary>
    public static SaveMigrationResult Fail(
        string error,
        IReadOnlyList<int>? appliedFromVersions = null,
        bool timedOut = false)
    {
        return new SaveMigrationResult
        {
            Success = false,
            Error = error,
            AppliedFromVersions = appliedFromVersions ?? Array.Empty<int>(),
            TimedOut = timedOut
        };
    }
}
