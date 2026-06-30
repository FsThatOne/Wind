namespace FengZhi.Foundation.Epiphany;

public sealed class EpiphanyRegistry
{
    private readonly List<EpiphanyConfig> _configs = new();
    private readonly Dictionary<string, EpiphanyEventInstance> _instances = new();
    private readonly Dictionary<int, int> _chapterCaps = new()
    {
        [0] = 1, [1] = 3, [2] = 4, [3] = 5, [4] = 5, [5] = 5, [6] = 5
    };
    private readonly Dictionary<int, int> _chapterUsed = new();
    private int _meditationEpiphanyCount;

    public const int MeditationEpiphanyCap = 4;

    public void RegisterConfig(EpiphanyConfig config)
    {
        _configs.Add(config);
        _instances[config.Id] = new EpiphanyEventInstance
        {
            ConfigId = config.Id,
            State = EpiphanyEventState.Locked
        };
    }

    public void SetChapterCap(int chapter, int cap)
    {
        _chapterCaps[chapter] = cap;
    }

    public IReadOnlyList<EpiphanyConfig> AllConfigs => _configs;
    public IReadOnlyDictionary<string, EpiphanyEventInstance> AllInstances => _instances;

    public EpiphanyEventInstance? GetInstance(string id)
    {
        return _instances.TryGetValue(id, out var inst) ? inst : null;
    }

    public EpiphanyConfig? GetConfig(string id)
    {
        return _configs.Find(c => c.Id == id);
    }

    public void UpdateAvailability(IEpiphanyFlagProvider flags)
    {
        int chapter = flags.GetCurrentChapter();

        foreach (var config in _configs)
        {
            var instance = _instances[config.Id];
            if (instance.IsDone) continue;
            if (instance.State != EpiphanyEventState.Locked &&
                instance.State != EpiphanyEventState.Available) continue;

            bool preconditionsMet = chapter >= config.ChapterMin &&
                                    chapter <= config.ChapterMax &&
                                    CheckPreconditions(config, flags);

            if (preconditionsMet && instance.State == EpiphanyEventState.Locked)
                instance.State = EpiphanyEventState.Available;
            else if (!preconditionsMet && instance.State == EpiphanyEventState.Available)
                instance.State = EpiphanyEventState.Locked;
        }
    }

    public bool IsChapterCapReached(int chapter)
    {
        int cap = _chapterCaps.TryGetValue(chapter, out int c) ? c : 5;
        int used = _chapterUsed.TryGetValue(chapter, out int u) ? u : 0;
        return used >= cap;
    }

    public bool IsMeditationCapReached()
    {
        return _meditationEpiphanyCount >= MeditationEpiphanyCap;
    }

    public void ConsumeChapterQuota(int chapter, int cost)
    {
        if (!_chapterUsed.ContainsKey(chapter))
            _chapterUsed[chapter] = 0;
        _chapterUsed[chapter] += cost;
    }

    public void IncrementMeditationCount()
    {
        _meditationEpiphanyCount++;
    }

    public EpiphanyConfig? FindBestAvailableCombatEpiphany(int chapter)
    {
        EpiphanyConfig? best = null;
        foreach (var config in _configs)
        {
            if (config.Type != EpiphanyType.Combat) continue;
            var inst = _instances[config.Id];
            if (inst.State != EpiphanyEventState.Available) continue;
            if (best == null || config.Priority > best.Priority)
                best = config;
        }
        return best;
    }

    public EpiphanyConfig? FindNarrativeEpiphany(string nodeId)
    {
        foreach (var config in _configs)
        {
            if (config.Type != EpiphanyType.Narrative) continue;
            if (config.NarrativeNodeId != nodeId) continue;
            var inst = _instances[config.Id];
            if (inst.State == EpiphanyEventState.Available)
                return config;
        }
        return null;
    }

    public EpiphanyConfig? FindMeditationEpiphany(IEpiphanyFlagProvider flags)
    {
        if (IsMeditationCapReached()) return null;

        int meditationDays = flags.GetMeditationDays();
        foreach (var config in _configs)
        {
            if (config.Type != EpiphanyType.Meditation) continue;
            var inst = _instances[config.Id];
            if (inst.State != EpiphanyEventState.Available) continue;
            if (config.MeditationCondition == null) continue;

            if (meditationDays >= config.MeditationCondition.MinMeditationDays &&
                AllFlagsPresent(config.MeditationCondition.RequiredFlags, flags))
                return config;
        }
        return null;
    }

    public int GetChapterUsed(int chapter)
    {
        return _chapterUsed.TryGetValue(chapter, out int u) ? u : 0;
    }

    public int MeditationEpiphanyCount => _meditationEpiphanyCount;

    public void LoadState(EpiphanySaveData data)
    {
        foreach (var entry in data.EventStates)
        {
            if (_instances.TryGetValue(entry.Key, out var inst))
            {
                inst.State = entry.Value.State;
                inst.SkipCount = entry.Value.SkipCount;
            }
        }
        foreach (var entry in data.ChapterUsed)
            _chapterUsed[entry.Key] = entry.Value;
        _meditationEpiphanyCount = data.MeditationCount;
    }

    public EpiphanySaveData ExportState()
    {
        var data = new EpiphanySaveData
        {
            MeditationCount = _meditationEpiphanyCount
        };
        foreach (var (id, inst) in _instances)
            data.EventStates[id] = new EpiphanyEventSaveEntry
            {
                State = inst.State,
                SkipCount = inst.SkipCount
            };
        foreach (var (ch, used) in _chapterUsed)
            data.ChapterUsed[ch] = used;
        return data;
    }

    private bool CheckPreconditions(EpiphanyConfig config, IEpiphanyFlagProvider flags)
    {
        foreach (var pre in config.Preconditions)
        {
            if (pre.StartsWith("not_flag:"))
            {
                string flag = pre["not_flag:".Length..].Trim();
                if (flags.HasFlag(flag)) return false;
            }
            else if (pre.StartsWith("flag:"))
            {
                string flag = pre["flag:".Length..].Trim();
                if (!flags.HasFlag(flag)) return false;
            }
        }
        return true;
    }

    private bool AllFlagsPresent(List<string> requiredFlags, IEpiphanyFlagProvider flags)
    {
        foreach (var f in requiredFlags)
        {
            if (!flags.HasFlag(f)) return false;
        }
        return true;
    }
}
