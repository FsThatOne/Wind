using System.Security.Cryptography;
using System.Text;

namespace FengZhi.Foundation.SaveSystem;

/// <summary>
/// 存档 JSON payload 的 AES-256-CBC 加密、HMAC-SHA256 签名和文件头校验服务。
/// </summary>
public sealed partial class SaveCryptoService
{
    private const int AesKeyByteLength = 32;
    private readonly byte[] _encryptionKey;
    private readonly byte[] _hmacKey;

    /// <summary>创建存档加密服务。两个 key 必须各为 32 字节。</summary>
    public SaveCryptoService(byte[] encryptionKey, byte[] hmacKey)
    {
        if (encryptionKey.Length != AesKeyByteLength)
            throw new ArgumentException("AES-256 encryption key 必须为 32 字节。", nameof(encryptionKey));
        if (hmacKey.Length != AesKeyByteLength)
            throw new ArgumentException("HMAC key 必须为 32 字节。", nameof(hmacKey));

        _encryptionKey = encryptionKey.ToArray();
        _hmacKey = hmacKey.ToArray();
    }

    /// <summary>
    /// 将 JSON UTF-8 文本加密为完整存档字节：Header + IV + Ciphertext + HMAC。
    /// </summary>
    public byte[] EncryptJson(string json, int schemaVersion = SaveFileHeader.InitialSchemaVersion)
    {
        var header = new SaveFileHeader
        {
            SchemaVersion = schemaVersion,
            Flags = SaveFileFlags.Encrypted | SaveFileFlags.HasHmac
        };
        var headerBytes = header.ToBytes();
        var iv = RandomNumberGenerator.GetBytes(SaveFileHeader.IvByteLength);
        var plaintext = Encoding.UTF8.GetBytes(json);
        var ciphertext = EncryptPayload(plaintext, iv);

        var signedLength = headerBytes.Length + iv.Length + ciphertext.Length;
        var output = new byte[signedLength + SaveFileHeader.HmacByteLength];
        headerBytes.CopyTo(output, 0);
        iv.CopyTo(output, headerBytes.Length);
        ciphertext.CopyTo(output, headerBytes.Length + iv.Length);

        var hmac = ComputeHmac(output.AsSpan(0, signedLength));
        hmac.CopyTo(output, signedLength);
        return output;
    }

    /// <summary>
    /// 校验并解密完整存档字节，返回 JSON UTF-8 文本与文件头。
    /// </summary>
    public SaveDecryptResult DecryptJson(byte[] fileBytes)
    {
        var minimumLength = SaveFileHeader.HeaderByteLength
            + SaveFileHeader.IvByteLength
            + SaveFileHeader.HmacByteLength;
        if (fileBytes.Length < minimumLength)
            return SaveDecryptResult.Fail("存档文件长度不足。");

        SaveFileHeader header;
        try
        {
            header = SaveFileHeader.FromBytes(fileBytes.AsSpan(0, SaveFileHeader.HeaderByteLength));
        }
        catch (InvalidDataException ex)
        {
            return SaveDecryptResult.Fail(ex.Message);
        }

        if (!header.Flags.HasFlag(SaveFileFlags.Encrypted) || !header.Flags.HasFlag(SaveFileFlags.HasHmac))
            return SaveDecryptResult.Fail("存档 flags 缺少加密或 HMAC 标记。");

        var signedLength = fileBytes.Length - SaveFileHeader.HmacByteLength;
        var expectedHmac = ComputeHmac(fileBytes.AsSpan(0, signedLength));
        var actualHmac = fileBytes.AsSpan(signedLength, SaveFileHeader.HmacByteLength);
        if (!CryptographicOperations.FixedTimeEquals(expectedHmac, actualHmac))
            return SaveDecryptResult.Fail("存档 HMAC 校验失败。");

        try
        {
            var iv = fileBytes.AsSpan(SaveFileHeader.HeaderByteLength, SaveFileHeader.IvByteLength).ToArray();
            var ciphertextOffset = SaveFileHeader.HeaderByteLength + SaveFileHeader.IvByteLength;
            var ciphertextLength = signedLength - ciphertextOffset;
            var ciphertext = fileBytes.AsSpan(ciphertextOffset, ciphertextLength).ToArray();
            var plaintext = DecryptPayload(ciphertext, iv);
            return SaveDecryptResult.Ok(Encoding.UTF8.GetString(plaintext), header);
        }
        catch (CryptographicException ex)
        {
            return SaveDecryptResult.Fail($"存档解密失败：{ex.Message}");
        }
    }

    private byte[] EncryptPayload(byte[] plaintext, byte[] iv)
    {
        using var aes = CreateAes(iv);
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    private byte[] DecryptPayload(byte[] ciphertext, byte[] iv)
    {
        using var aes = CreateAes(iv);
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private Aes CreateAes(byte[] iv)
    {
        var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = _encryptionKey;
        aes.IV = iv;
        return aes;
    }

    private byte[] ComputeHmac(ReadOnlySpan<byte> data)
    {
        using var hmac = new HMACSHA256(_hmacKey);
        return hmac.ComputeHash(data.ToArray());
    }
}

/// <summary>
/// 存档解密结果。
/// </summary>
public sealed partial record SaveDecryptResult
{
    /// <summary>是否成功。</summary>
    public bool Success { get; init; }

    /// <summary>失败原因。</summary>
    public string? Error { get; init; }

    /// <summary>解密出的 JSON 文本。</summary>
    public string? Json { get; init; }

    /// <summary>解析出的文件头。</summary>
    public SaveFileHeader? Header { get; init; }

    /// <summary>成功结果。</summary>
    public static SaveDecryptResult Ok(string json, SaveFileHeader header)
    {
        return new SaveDecryptResult { Success = true, Json = json, Header = header };
    }

    /// <summary>失败结果。</summary>
    public static SaveDecryptResult Fail(string error)
    {
        return new SaveDecryptResult { Success = false, Error = error };
    }
}
