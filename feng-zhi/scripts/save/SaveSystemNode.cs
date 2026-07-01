using System;
using Godot;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Save;

/// <summary>
/// 存档系统 Autoload 节点。初始化 Foundation SaveSystem 全部组件并桥接 Godot API。
/// </summary>
public partial class SaveSystemNode : Node
{
    private static readonly byte[] DevEncryptionKey = new byte[]
    {
        0x46, 0x5A, 0x48, 0x49, 0x2D, 0x44, 0x45, 0x56,
        0x2D, 0x4B, 0x45, 0x59, 0x2D, 0x32, 0x30, 0x32,
        0x36, 0x2D, 0x30, 0x37, 0x2D, 0x30, 0x31, 0x2D,
        0x53, 0x41, 0x56, 0x45, 0x53, 0x59, 0x53, 0x30
    };

    private static readonly byte[] DevHmacKey = new byte[]
    {
        0x46, 0x5A, 0x48, 0x49, 0x2D, 0x48, 0x4D, 0x41,
        0x43, 0x2D, 0x4B, 0x45, 0x59, 0x2D, 0x32, 0x30,
        0x32, 0x36, 0x2D, 0x30, 0x37, 0x2D, 0x30, 0x31,
        0x2D, 0x53, 0x41, 0x56, 0x45, 0x53, 0x59, 0x53
    };

    public SaveManager SaveManager { get; private set; } = null!;
    public AutosaveScheduler Scheduler { get; private set; } = null!;

    private GameFlow _gameFlow = null!;
    private FileSavePayloadStore _store = null!;
    private Action? _unsubscribeAutosave;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _gameFlow = GetNode<GameFlow>("/root/GameFlow");

        var savesPath = ProjectSettings.GlobalizePath("user://saves");
        using var dir = DirAccess.Open("user://");
        if (dir != null && !DirAccess.DirExistsAbsolute(savesPath))
            dir.MakeDirRecursive("saves");

        var crypto = new SaveCryptoService(DevEncryptionKey, DevHmacKey);
        var migrationChain = new MigrationChain();
        var debugFileService = new SaveDebugFileService();

        _store = new FileSavePayloadStore(
            savesPath, crypto, migrationChain,
            SaveFileHeader.InitialSchemaVersion, debugFileService);
        _store.Initialize();

        SaveManager = new SaveManager(_store, _gameFlow.EventBus, BuildMetadata);

        RegisterSaveables();

        var blocker = new GodotAutosaveBlocker(_gameFlow);
        Scheduler = new AutosaveScheduler(SaveManager, blocker);
        _unsubscribeAutosave = Scheduler.Subscribe(_gameFlow.EventBus);

        GD.Print("[SaveSystem] Autoload ready. Store initialized, adapters registered.");
    }

    public override void _ExitTree()
    {
        _unsubscribeAutosave?.Invoke();
    }

    public SaveLoadMenuPresenter CreatePresenter(SaveLoadMenuMode mode)
    {
        var blocker = new GodotSaveMenuBlocker(_gameFlow);
        return new SaveLoadMenuPresenter(SaveManager, mode, blocker);
    }

    private void RegisterSaveables()
    {
        // 注册现有存档系统中已有的 ISaveable 由各系统自行创建;
        // SaveSystemNode 只注册需要 Godot 层桥接的适配器。
        // 各系统的 ISaveable 实例应在系统初始化后通过 RegisterAdapter 注册。
    }

    /// <summary>供外部系统注册 ISaveable 适配器。</summary>
    public SaveResult RegisterAdapter(ISaveable adapter)
    {
        return SaveManager.RegisterSerializer(adapter);
    }

    private SlotMetadata BuildMetadata(SaveSlotId slotId)
    {
        return new SlotMetadata
        {
            SlotId = slotId,
            IsOccupied = true,
            Timestamp = System.DateTimeOffset.UtcNow,
            ChapterName = _gameFlow.CurrentChapterName,
            SceneName = _gameFlow.CurrentSceneName,
            GameDay = _gameFlow.CurrentGameDay,
            Playtime = _gameFlow.Playtime,
            ThumbnailPng = System.Array.Empty<byte>(),
            IsNewGamePlus = false,
        };
    }

    private sealed class GodotAutosaveBlocker : IAutosaveBlocker
    {
        private readonly GameFlow _gameFlow;
        public GodotAutosaveBlocker(GameFlow gameFlow) => _gameFlow = gameFlow;

        public bool IsAutosaveBlocked =>
            _gameFlow.IsInCombat || _gameFlow.IsInCinematicLock;

        public string BlockReason
        {
            get
            {
                if (_gameFlow.IsInCombat) return "战斗中";
                if (_gameFlow.IsInCinematicLock) return "演出中";
                return string.Empty;
            }
        }
    }

    private sealed class GodotSaveMenuBlocker : ISaveMenuBlocker
    {
        private readonly GameFlow _gameFlow;
        public GodotSaveMenuBlocker(GameFlow gameFlow) => _gameFlow = gameFlow;

        public bool IsSaveBlocked =>
            _gameFlow.IsInCombat || _gameFlow.IsInCinematicLock;

        public string BlockReason
        {
            get
            {
                if (_gameFlow.IsInCombat) return "战斗中不可存档";
                if (_gameFlow.IsInCinematicLock) return "演出中不可存档";
                return string.Empty;
            }
        }
    }
}
