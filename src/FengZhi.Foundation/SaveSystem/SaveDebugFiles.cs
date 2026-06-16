namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 存档迁移与调试用文件辅助。只封装平台文件操作，不持有游戏逻辑。
/// </summary>
public sealed partial class SaveDebugFileService
{
    /// <summary>当前编译产物是否包含 DEBUG 明文 dump 路径。</summary>
    public static bool IsDevJsonDumpCompiledIn
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>在迁移前创建原文件备份，备份路径固定为 savePath + ".bak"。</summary>
    public async Task<SaveResult> CreateMigrationBackupAsync(string savePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            return SaveResult.Fail("存档路径不能为空。");

        if (!File.Exists(savePath))
            return SaveResult.Fail($"存档文件不存在：{savePath}");

        try
        {
            var backupPath = GetBackupPath(savePath);
            await using var source = File.Open(savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var target = File.Open(backupPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            return SaveResult.Ok();
        }
        catch (Exception ex)
        {
            return SaveResult.Fail($"创建迁移备份失败：{ex.Message}");
        }
    }

    /// <summary>DEBUG 构建写出 savePath + ".dev.json"；非 DEBUG 构建编译为空操作。</summary>
    public async Task<SaveResult> WriteDevJsonAsync(
        string savePath,
        string json,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            return SaveResult.Fail("存档路径不能为空。");

#if DEBUG
        try
        {
            await File.WriteAllTextAsync(GetDevJsonPath(savePath), json, cancellationToken).ConfigureAwait(false);
            return SaveResult.Ok();
        }
        catch (Exception ex)
        {
            return SaveResult.Fail($"写出 DEBUG 明文存档失败：{ex.Message}");
        }
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return SaveResult.Ok();
#endif
    }

    /// <summary>获取迁移备份路径。</summary>
    public static string GetBackupPath(string savePath) => savePath + ".bak";

    /// <summary>获取 DEBUG 明文 dump 路径。</summary>
    public static string GetDevJsonPath(string savePath) => savePath + ".dev.json";
}
