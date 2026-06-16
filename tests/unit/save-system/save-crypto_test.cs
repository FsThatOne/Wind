using System.Text;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.Unit.SaveSystem;

public sealed class SaveCryptoTests
{
    private static readonly byte[] EncryptionKey = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] HmacKey = Enumerable.Range(101, 32).Select(i => (byte)i).ToArray();

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"metadata":{"chapter":"序章"},"systems":{"dialogue":{"node":"start"}}}""")]
    public void EncryptJson_ThenDecryptJson_RestoresOriginalPayload(string json)
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);

        var bytes = crypto.EncryptJson(json);
        var result = crypto.DecryptJson(bytes);

        Assert.True(result.Success, result.Error);
        Assert.Equal(json, result.Json);
        Assert.NotNull(result.Header);
        Assert.Equal("FZHI", result.Header.Magic);
        Assert.Equal(1, result.Header.SchemaVersion);
        Assert.True(result.Header.Flags.HasFlag(SaveFileFlags.Encrypted));
        Assert.True(result.Header.Flags.HasFlag(SaveFileFlags.HasHmac));
    }

    [Fact]
    public void EncryptJson_LargePayload_RoundTrips()
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);
        var json = "{\"blob\":\"" + new string('风', 4096) + "\"}";

        var bytes = crypto.EncryptJson(json);
        var result = crypto.DecryptJson(bytes);

        Assert.True(result.Success, result.Error);
        Assert.Equal(json, result.Json);
    }

    [Fact]
    public void EncryptJson_CalledTwice_GeneratesDifferentIvAndCiphertext()
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);

        var first = crypto.EncryptJson("""{"value":1}""");
        var second = crypto.EncryptJson("""{"value":1}""");

        var firstIv = first.Skip(SaveFileHeader.HeaderByteLength).Take(SaveFileHeader.IvByteLength).ToArray();
        var secondIv = second.Skip(SaveFileHeader.HeaderByteLength).Take(SaveFileHeader.IvByteLength).ToArray();
        Assert.NotEqual(firstIv, secondIv);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void DecryptJson_InvalidMagic_RejectsBeforeDecrypting()
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);
        var bytes = crypto.EncryptJson("""{"value":1}""");
        bytes[0] = Encoding.ASCII.GetBytes("X")[0];

        var result = crypto.DecryptJson(bytes);

        Assert.False(result.Success);
        Assert.Contains("magic", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(SaveFileHeader.HeaderByteLength + 2)]
    [InlineData(SaveFileHeader.HeaderByteLength + SaveFileHeader.IvByteLength + 1)]
    public void DecryptJson_TamperHeaderIvOrPayloadByte_FailsHmacValidation(int byteIndex)
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);
        var bytes = crypto.EncryptJson("""{"value":1}""");
        bytes[byteIndex] ^= 0x01;

        var result = crypto.DecryptJson(bytes);

        Assert.False(result.Success);
        Assert.Contains("HMAC", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecryptJson_TamperHmacByte_FailsHmacValidation()
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);
        var bytes = crypto.EncryptJson("""{"value":1}""");
        bytes[^1] ^= 0x01;

        var result = crypto.DecryptJson(bytes);

        Assert.False(result.Success);
        Assert.Contains("HMAC", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecryptJson_FileTooShort_ReturnsFailure()
    {
        var crypto = new SaveCryptoService(EncryptionKey, HmacKey);

        var result = crypto.DecryptJson(new byte[8]);

        Assert.False(result.Success);
        Assert.Contains("长度不足", result.Error);
    }

    [Fact]
    public void SaveFileHeader_ToBytesAndFromBytes_PreservesVersionAndFlags()
    {
        var header = new SaveFileHeader
        {
            SchemaVersion = 7,
            Flags = SaveFileFlags.Encrypted | SaveFileFlags.HasHmac,
            Reserved = Enumerable.Range(0, 20).Select(i => (byte)i).ToArray()
        };

        var parsed = SaveFileHeader.FromBytes(header.ToBytes());

        Assert.Equal(7, parsed.SchemaVersion);
        Assert.True(parsed.Flags.HasFlag(SaveFileFlags.Encrypted));
        Assert.True(parsed.Flags.HasFlag(SaveFileFlags.HasHmac));
        Assert.Equal(header.Reserved, parsed.Reserved);
    }
}
