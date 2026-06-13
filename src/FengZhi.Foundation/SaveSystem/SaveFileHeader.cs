namespace FengZhi.Foundation.SaveSystem;

using System.Buffers.Binary;
using System.Text;

/// <summary>
/// 存档文件明文头部，位于 IV、密文 payload 与 HMAC 之前。
/// </summary>
public sealed partial record SaveFileHeader
{
    private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes(ExpectedMagic);

    /// <summary>魔数 ASCII "FZHI"。</summary>
    public const string ExpectedMagic = "FZHI";

    /// <summary>明文头部固定字节数：magic(4) + schema_version(4) + flags(4) + reserved(20)。</summary>
    public const int HeaderByteLength = 32;

    /// <summary>AES-CBC IV 字节数。</summary>
    public const int IvByteLength = 16;

    /// <summary>HMAC-SHA256 字节数。</summary>
    public const int HmacByteLength = 32;

    /// <summary>首个正式存档结构版本。</summary>
    public const int InitialSchemaVersion = 1;

    /// <summary>存档魔数，必须为 "FZHI"。</summary>
    public string Magic { get; init; } = ExpectedMagic;

    /// <summary>payload schema 版本，从 1 起步。</summary>
    public int SchemaVersion { get; init; } = InitialSchemaVersion;

    /// <summary>文件标志位，供后续压缩、调试或平台差异扩展。</summary>
    public SaveFileFlags Flags { get; init; } = SaveFileFlags.None;

    /// <summary>预留 20 字节，保持头部向前兼容。</summary>
    public byte[] Reserved { get; init; } = new byte[20];

    /// <summary>创建 schema_version=1 的默认文件头。</summary>
    public static SaveFileHeader CreateDefault(SaveFileFlags flags = SaveFileFlags.None)
    {
        return new SaveFileHeader { Flags = flags };
    }

    /// <summary>写入 32 字节文件头。</summary>
    public byte[] ToBytes()
    {
        if (!string.Equals(Magic, ExpectedMagic, StringComparison.Ordinal))
            throw new InvalidOperationException($"存档 magic 必须为 {ExpectedMagic}。");

        if (SchemaVersion < InitialSchemaVersion)
            throw new InvalidOperationException("schema_version 必须大于等于 1。");

        if (Reserved.Length != 20)
            throw new InvalidOperationException("reserved 必须为 20 字节。");

        var bytes = new byte[HeaderByteLength];
        MagicBytes.CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), (uint)SchemaVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), (uint)Flags);
        Reserved.CopyTo(bytes, 12);
        return bytes;
    }

    /// <summary>从 32 字节文件头解析结构；magic 不正确会抛出异常。</summary>
    public static SaveFileHeader FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderByteLength)
            throw new InvalidDataException("存档文件头长度不足。");

        if (!bytes[..4].SequenceEqual(MagicBytes))
            throw new InvalidDataException("存档 magic 不正确。");

        var schemaVersion = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4));
        if (schemaVersion < InitialSchemaVersion)
            throw new InvalidDataException("schema_version 不合法。");

        var flags = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(8, 4));
        return new SaveFileHeader
        {
            SchemaVersion = checked((int)schemaVersion),
            Flags = (SaveFileFlags)flags,
            Reserved = bytes.Slice(12, 20).ToArray()
        };
    }
}

/// <summary>
/// 存档文件头标志位。
/// </summary>
[Flags]
public enum SaveFileFlags : uint
{
    /// <summary>无额外标志。</summary>
    None = 0,

    /// <summary>payload 已加密。ss-003 会实际设置与校验。</summary>
    Encrypted = 1 << 0,

    /// <summary>存在 HMAC 完整性校验。ss-003 会实际设置与校验。</summary>
    HasHmac = 1 << 1,

    /// <summary>开发构建可能存在旁路明文 dump。ss-004 使用。</summary>
    DebugDumpAvailable = 1 << 2
}
