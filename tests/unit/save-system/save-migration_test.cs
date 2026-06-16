using System.Text.Json;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Unit.SaveSystem;

public sealed class SaveMigrationTests
{
    [Fact]
    public async Task ApplyAsync_LowerVersionPayload_RunsEveryMigrationInOrder()
    {
        var order = new List<int>();
        var chain = new MigrationChain();
        Assert.True(chain.Register(new AddFieldMigration(1, "party", "[]", order)).Success);
        Assert.True(chain.Register(new AddFieldMigration(2, "chapter", "prologue", order)).Success);

        var result = await chain.ApplyAsync(CreatePayload(1), 3);

        Assert.True(result.Success, result.Error);
        Assert.Equal(new[] { 1, 2 }, order);
        Assert.Equal(new[] { 1, 2 }, result.AppliedFromVersions);
        Assert.NotNull(result.Payload);
        Assert.Equal(3, result.Payload.SchemaVersion);
        Assert.Equal("[]", result.Payload.Systems["system"].Values["party"].GetString());
        Assert.Equal("prologue", result.Payload.Systems["system"].Values["chapter"].GetString());
    }

    [Fact]
    public async Task ApplyAsync_MissingIntermediateMigration_FailsWithoutSkippingVersion()
    {
        var chain = new MigrationChain();
        Assert.True(chain.Register(new AddFieldMigration(1, "party", "[]")).Success);

        var result = await chain.ApplyAsync(CreatePayload(1), 3);

        Assert.False(result.Success);
        Assert.Contains("缺少迁移", result.Error);
        Assert.Contains("v2 -> v3", result.Error);
    }

    [Fact]
    public void Register_NonSequentialMigration_ReturnsFailure()
    {
        var chain = new MigrationChain();

        var result = chain.Register(new JumpMigration());

        Assert.False(result.Success);
        Assert.Contains("单步递增", result.Error);
    }

    [Fact]
    public async Task CreateMigrationBackupAsync_ExistingSaveFile_CreatesBakBeforeMigration()
    {
        var service = new SaveDebugFileService();
        var directory = CreateTempDirectory();
        var savePath = Path.Combine(directory, "slot01.sav");
        await File.WriteAllTextAsync(savePath, "old-save");

        var result = await service.CreateMigrationBackupAsync(savePath);

        Assert.True(result.Success, result.Error);
        Assert.Equal("old-save", await File.ReadAllTextAsync(SaveDebugFileService.GetBackupPath(savePath)));
    }

    [Fact]
    public async Task ApplyAsync_MigrationExceedsTimeout_FailsConservatively()
    {
        var chain = new MigrationChain(TimeSpan.FromMilliseconds(20));
        Assert.True(chain.Register(new SlowMigration()).Success);

        var result = await chain.ApplyAsync(CreatePayload(1), 2);

        Assert.False(result.Success);
        Assert.True(result.TimedOut);
        Assert.Contains("超时", result.Error);
    }

#if DEBUG
    [Fact]
    public async Task WriteDevJsonAsync_DebugBuild_WritesDevJsonDump()
    {
        var service = new SaveDebugFileService();
        var directory = CreateTempDirectory();
        var savePath = Path.Combine(directory, "slot01.sav");

        var result = await service.WriteDevJsonAsync(savePath, """{"schema_version":1}""");

        Assert.True(SaveDebugFileService.IsDevJsonDumpCompiledIn);
        Assert.True(result.Success, result.Error);
        Assert.Equal("""{"schema_version":1}""", await File.ReadAllTextAsync(SaveDebugFileService.GetDevJsonPath(savePath)));
    }
#else
    [Fact]
    public async Task WriteDevJsonAsync_ReleaseBuild_DoesNotWriteDevJsonDump()
    {
        var service = new SaveDebugFileService();
        var directory = CreateTempDirectory();
        var savePath = Path.Combine(directory, "slot01.sav");

        var result = await service.WriteDevJsonAsync(savePath, """{"schema_version":1}""");

        Assert.False(SaveDebugFileService.IsDevJsonDumpCompiledIn);
        Assert.True(result.Success, result.Error);
        Assert.False(File.Exists(SaveDebugFileService.GetDevJsonPath(savePath)));
    }
#endif

    private static SavePayload CreatePayload(int schemaVersion)
    {
        var payload = SavePayload.Create(SlotMetadata.Empty(SaveSlotId.Manual(1))) with
        {
            SchemaVersion = schemaVersion
        };
        payload.Systems["system"] = SaveSnapshot.Empty;
        return payload;
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "fengzhi-save-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class AddFieldMigration : IMigration
    {
        private readonly string _field;
        private readonly string _value;
        private readonly List<int>? _order;

        public AddFieldMigration(int fromVersion, string field, string value, List<int>? order = null)
        {
            FromVersion = fromVersion;
            ToVersion = fromVersion + 1;
            _field = field;
            _value = value;
            _order = order;
        }

        public int FromVersion { get; }

        public int ToVersion { get; }

        public SavePayload Migrate(SavePayload payload)
        {
            _order?.Add(FromVersion);
            payload.Systems["system"].Values[_field] = JsonSerializer.SerializeToElement(_value);
            return payload;
        }
    }

    private sealed class JumpMigration : IMigration
    {
        public int FromVersion => 1;

        public int ToVersion => 3;

        public SavePayload Migrate(SavePayload payload) => payload;
    }

    private sealed class SlowMigration : IMigration
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public SavePayload Migrate(SavePayload payload)
        {
            Thread.Sleep(100);
            return payload;
        }
    }
}
