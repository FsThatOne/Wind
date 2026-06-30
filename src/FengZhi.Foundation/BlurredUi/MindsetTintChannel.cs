namespace FengZhi.Foundation.BlurredUi;

public sealed class MindsetTintChannel
{
    private static readonly Dictionary<string, TintColor> ZoneTintTable = new()
    {
        ["中庸"] = TintColor.Zero,
        ["孤剑入世"] = new TintColor(0.06f, -0.02f, -0.04f),
        ["执念未定"] = new TintColor(0.03f, -0.01f, -0.02f),
        ["风止尘湮"] = new TintColor(-0.02f, -0.01f, 0.04f),
        ["入世未定"] = new TintColor(0.02f, 0.01f, -0.01f),
        ["出世未定"] = new TintColor(-0.01f, 0.01f, 0.02f),
        ["白衣入世"] = new TintColor(0.04f, 0.03f, -0.01f),
        ["释怀未定"] = new TintColor(0.02f, 0.02f, 0f),
        ["大隐于市"] = new TintColor(-0.02f, 0.03f, 0.03f),
    };

    private readonly IMindsetDataProvider _provider;
    private readonly float _baseIntensity;
    private readonly float _extremityScale;

    private string _lastKnownZone = "中庸";

    public MindsetTintChannel(
        IMindsetDataProvider provider,
        float baseIntensity = 0.10f,
        float extremityScale = 0.3f)
    {
        _provider = provider;
        _baseIntensity = baseIntensity;
        _extremityScale = extremityScale;
    }

    public PendingReveal? CheckForZoneChange(long timestamp)
    {
        string currentZone = _provider.GetMindsetZone();
        if (currentZone == _lastKnownZone) return null;

        string previousZone = _lastKnownZone;
        _lastKnownZone = currentZone;

        return new PendingReveal
        {
            Channel = RevealChannel.Mindset,
            Message = $"心境转为「{currentZone}」",
            Payload = new MindsetRevealPayload
            {
                PreviousZone = previousZone,
                NewZone = currentZone,
                TargetTint = GetTintForZone(currentZone)
            },
            Timestamp = timestamp
        };
    }

    public TintColor GetCurrentTint()
    {
        return GetTintForZone(_lastKnownZone);
    }

    public float GetCurrentIntensity()
    {
        float extremity = _provider.GetZoneExtremity();
        return _baseIntensity * (1.0f + extremity * _extremityScale);
    }

    public IReadOnlyList<string> GetPendingEchoes()
    {
        return _provider.GetEchoQueue();
    }

    public void ConsumeEchoes(int count)
    {
        _provider.ConsumeEcho(count);
    }

    public static TintColor GetTintForZone(string zone)
    {
        return ZoneTintTable.TryGetValue(zone, out var tint) ? tint : TintColor.Zero;
    }

    public static PendingReveal CreateMergedReveal(PendingReveal latest)
    {
        return latest;
    }

    public string LastKnownZone => _lastKnownZone;

    public void SetLastKnownZone(string zone)
    {
        _lastKnownZone = zone;
    }
}
