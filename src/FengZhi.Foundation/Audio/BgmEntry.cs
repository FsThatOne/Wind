namespace FengZhi.Foundation.Audio;

/// <summary>
/// BGM Override 栈条目。
/// </summary>
public readonly record struct BgmEntry(string TrackId, AudioState State);
