using Xunit;

namespace FengZhi.Tests.Foundation.Narrative;

public sealed class PrologueSceneDialogueHooksTest
{
    private static readonly string[] RequiredDialoguePaths =
    {
        "sister_gather_herb_01.yaml",
        "prologue_opening_01.yaml",
        "sister_gather_ore_01.yaml",
        "animal_tracks_mount_foreshadow_01.yaml",
        "manor_errands_01.yaml",
        "master_study_01.yaml",
        "sister_wine_reminder_01.yaml",
        "wine_pickup_01.yaml",
        "storage_shelf_01.yaml",
        "memory_marker_01.yaml",
        "rest_spot_01.yaml",
        "massacre_return_01.yaml",
        "massacre_evidence_01.yaml",
        "senior_brother_return_01.yaml",
        "senior_brother_misunderstanding_01.yaml",
        "joint_burial_01.yaml",
        "farewell_inheritance_01.yaml",
        "letter_promise_01.yaml"
    };

    [Fact]
    public void PrologueDialogueFiles_AreHookedByReachableSceneScripts()
    {
        var sceneScriptText = ReadSceneScripts();

        foreach (var fileName in RequiredDialoguePaths)
            Assert.Contains($"res://assets/data/dialogues/chapter_00/{fileName}", sceneScriptText);
    }

    [Fact]
    public void MainMenuNewGame_StartsAtSectCompoundRatherThanOldCavePlaceholder()
    {
        var mainMenu = ReadFile("feng-zhi/scripts/ui/MainMenuGame.cs");

        Assert.Contains("res://scenes/sect_compound/SectCompound.tscn", mainMenu);
        Assert.DoesNotContain("PrologueScenePath = \"res://scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn\"", mainMenu);
    }

    [Fact]
    public void PauseMenu_IsMountedOnViewportCanvasLayerInsteadOfWorldCanvas()
    {
        var settingsManager = ReadFile("feng-zhi/scripts/settings/SettingsManager.cs");
        var pauseMenu = ReadFile("feng-zhi/scripts/ui/PauseMenuUi.cs");

        Assert.Contains("new CanvasLayer", settingsManager);
        Assert.Contains("Name = \"PauseMenuLayer\"", settingsManager);
        Assert.Contains("Layer = 30", settingsManager);
        Assert.Contains("_pauseMenuInstance.SetAnchorsPreset(Control.LayoutPreset.FullRect)", settingsManager);
        Assert.Contains("_pauseMenuLayer.AddChild(_pauseMenuInstance)", settingsManager);
        Assert.DoesNotContain("GetTree().Root.AddChild(_pauseMenuInstance)", settingsManager);

        Assert.Contains("GetParent().AddChild(instance)", pauseMenu);
        Assert.DoesNotContain("GetTree().Root.AddChild(instance)", pauseMenu);
    }

    [Fact]
    public void MassacreStudyHook_DoesNotLeakOldDirectCulpritPlaceholders()
    {
        var study = ReadFile("feng-zhi/scripts/StudyGame.cs");

        Assert.Contains("暗格里空空如也", study);
        Assert.Contains("这里曾经放着什么，你现在还不知道", study);
        Assert.DoesNotContain("令牌", study);
        Assert.DoesNotContain("旧信", study);
        Assert.DoesNotContain("山庄被盯上的原因", study);
    }

    [Fact]
    public void SeniorBrotherAndTutorialHooks_AreAfterNightVariantBranches()
    {
        var compound = ReadFile("feng-zhi/scripts/SectCompoundGame.cs");
        var mainHall = ReadFile("feng-zhi/scripts/MainHallGame.cs");

        Assert.Contains("Variant == \"night\"", compound);
        Assert.Contains("senior_brother_return_01.yaml", compound);
        Assert.Contains("senior_brother_misunderstanding_01.yaml", compound);
        Assert.Contains("joint_burial_01.yaml", compound);

        Assert.Contains("Variant == \"night\"", mainHall);
        Assert.Contains("farewell_inheritance_01.yaml", mainHall);
        Assert.Contains("letter_promise_01.yaml", mainHall);
    }

    [Fact]
    public void DialogueQuestFlags_ArePersistedAndImportedAcrossGodotScenes()
    {
        var gameFlow = ReadFile("feng-zhi/scripts/GameFlow.cs");
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");

        Assert.Contains("QuestFlags", gameFlow);
        Assert.Contains("RecordQuestFlag", gameFlow);
        Assert.Contains("HasQuestFlag", gameFlow);

        Assert.Contains("Subscribe<DialogueQuestFlagEvent>", sceneBase);
        Assert.Contains("ImportQuestFlagsFromGameFlow", sceneBase);
        Assert.Contains("SetQuestFlag(gameEvent.Key, gameEvent.Value)", sceneBase);
        Assert.Contains("ConditionProvider?.SetFlag(key, normalized)", sceneBase);
    }

    [Fact]
    public void MisunderstandingMods_AreImportedForDialogueConditions()
    {
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");
        var provider = ReadFile("feng-zhi/scripts/dialogue/SceneConditionValueProvider.cs");
        var dialogue = ReadFile("feng-zhi/assets/data/dialogues/chapter_00/senior_brother_misunderstanding_01.yaml");

        Assert.Contains("ImportMisunderstandingModsFromGameFlow", sceneBase);
        Assert.Contains("Subscribe<DialogueRegisterMisunderstandingEvent>", sceneBase);
        Assert.Contains("ConditionProvider.SetMisunderstandingMod(npcId, mod)", sceneBase);
        Assert.Contains("source.StartsWith(\"misunderstanding_mod.\"", provider);
        Assert.Contains("misunderstanding_mod.senior_brother", dialogue);
        Assert.Contains("称呼退回陌生处", dialogue);
    }

    [Fact]
    public void SeniorBrotherMisunderstandingEvent_IsConnectedToRuntimeState()
    {
        var gameFlow = ReadFile("feng-zhi/scripts/GameFlow.cs");

        Assert.Contains("DialogueRegisterMisunderstandingEvent", gameFlow);
        Assert.Contains("Subscribe<DialogueRegisterMisunderstandingEvent>", gameFlow);
        Assert.Contains("SeniorBrotherSurvivorMisunderstandingId", gameFlow);
        Assert.Contains("MisunderstandingInstance.Create", gameFlow);
        Assert.Contains("_misunderstandingStateMachine.Activate(instance)", gameFlow);
        Assert.Contains("_misunderstandingModCalculator.RecomputeAndWrite(SeniorBrotherNpcId)", gameFlow);
        Assert.Contains("senior_brother_mis_resolved", gameFlow);
        Assert.Contains("public IReadOnlyDictionary<string, int> MisunderstandingMods", gameFlow);
    }

    [Fact]
    public void LightweightQuestTracker_ShowsLatestThreeWithMainlineFirst()
    {
        var gameFlow = ReadFile("feng-zhi/scripts/GameFlow.cs");
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");
        var debtRegister = ReadFile("docs/tech-debt-register.md");

        Assert.Contains("public enum TrackedQuestKind", gameFlow);
        Assert.Contains("public sealed record TrackedQuestObjective", gameFlow);
        Assert.Contains("TrackObjective", gameFlow);
        Assert.Contains("GetTrackedObjectivesForHud", gameFlow);
        Assert.Contains("Where(objective => objective.Kind == TrackedQuestKind.Mainline)", gameFlow);
        Assert.Contains("Where(objective => objective.Kind != TrackedQuestKind.Mainline)", gameFlow);
        Assert.Contains("new List<TrackedQuestObjective>(capacity: 3)", gameFlow);

        Assert.Contains("Name = $\"ObjectiveItem{i + 1}\"", sceneBase);
        Assert.Contains("for (var i = 0; i < 3; i++)", sceneBase);
        Assert.Contains("GameFlow.TrackedQuestKind.Mainline => \"主线\"", sceneBase);
        Assert.Contains("GameFlow.TrackedQuestKind.Side => \"支线\"", sceneBase);
        Assert.Contains("GameFlow.TrackedQuestKind.Tutorial => \"教学\"", sceneBase);
        Assert.Contains("_objectivePanel.OffsetLeft = 20f", sceneBase);
        Assert.Contains("_objectivePanel.OffsetRight = 420f", sceneBase);
        Assert.DoesNotContain("ObjectiveLabel", sceneBase);
        Assert.DoesNotContain("ReasonLabel", sceneBase);

        Assert.Contains("正式任务系统", debtRegister);
        Assert.Contains("轻量任务追踪 HUD", debtRegister);
    }

    [Fact]
    public void SceneInteractions_UseIsoDiamondCollisionAndProximityHighlight()
    {
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");

        Assert.Contains("InteractionDiamondOffset = new(22f, 5f)", sceneBase);
        Assert.Contains("InteractionDefaultFill = new(0.08f, 0.72f, 0.24f, 0.28f)", sceneBase);
        Assert.Contains("InteractionHighlightFill = new(0.10f, 0.95f, 0.32f, 0.58f)", sceneBase);
        Assert.Contains("InteractionHighlightOutline = new(0.20f, 1.0f, 0.44f, 1.0f)", sceneBase);
        Assert.Contains("Position = position - InteractionDiamondOffset", sceneBase);
        Assert.Contains("new CollisionPolygon2D", sceneBase);
        Assert.Contains("Name = \"InteractionDiamondCollision\"", sceneBase);
        Assert.Contains("Color = InteractionDefaultFill", sceneBase);
        Assert.Contains("Visible = true", sceneBase);
        Assert.Contains("new(-TileWidth / 2f, 0f)", sceneBase);
        Assert.Contains("new(0f, -TileHeight / 2f)", sceneBase);
        Assert.Contains("Name = \"InteractionHighlight\"", sceneBase);
        Assert.Contains("Name = \"InteractionHighlightOutline\"", sceneBase);
        Assert.Contains("SetInteractionHighlight(area, true)", sceneBase);
        Assert.Contains("SetInteractionHighlight(_focusedArea, false)", sceneBase);
        Assert.Contains("ClearFocusedInteraction", sceneBase);
        Assert.Contains("fill.Visible = true", sceneBase);
        Assert.DoesNotContain("fill.Visible = visible", sceneBase);
        Assert.DoesNotContain("CircleShape2D { Radius = 30f }", sceneBase);
    }

    [Fact]
    public void SceneInteractions_BlockTheirOwnTileAndFocusFromAdjacentTiles()
    {
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");

        Assert.Contains("IsBlockingInteractionMarker(type)", sceneBase);
        Assert.Contains("_blockedTiles.Add(new Vector2I(marker.TileX, marker.TileY))", sceneBase);
        Assert.Contains("UpdateAdjacentInteractionFocus(tile)", sceneBase);
        Assert.Contains("IsAdjacent(playerTile, new Vector2I(marker.TileX, marker.TileY))", sceneBase);
        Assert.Contains("Math.Abs(delta.X) + Math.Abs(delta.Y) == 1", sceneBase);
        Assert.Contains("FocusInteractionArea(area)", sceneBase);
        Assert.Contains("type != \"exit\"", sceneBase);
    }

    [Fact]
    public void CharacterAndAnimalFootprints_AreSharedAcrossPlayerAndNpc()
    {
        var footprint = ReadFile("feng-zhi/scripts/CharacterFootprint.cs");
        var player = ReadFile("feng-zhi/scripts/PlayerCharacterController.cs");
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");

        Assert.Contains("public static class CharacterFootprint", footprint);
        Assert.Contains("CreateCollisionPolygon", footprint);
        Assert.Contains("CharacterFootprint.ApplyTo(_collisionPolygon)", player);
        Assert.Contains("CharacterFootprint.ApplyTo(collision)", sceneBase);
        Assert.Contains("CreateCharacterBlockingBody", sceneBase);
        Assert.Contains("Name = \"CharacterFootprintCollision\"", sceneBase);
        Assert.DoesNotContain("ShrinkCollisionPolygon", player);
    }

    [Fact]
    public void SectCompoundDayMap_SpawnsPrologueNpcMarkers()
    {
        var dayMap = ReadFile("feng-zhi/assets/maps/sect_compound/maps/sect_compound_day.tmx");
        var nightMap = ReadFile("feng-zhi/assets/maps/sect_compound/maps/sect_compound_night.tmx");

        Assert.Contains("name=\"sister_baitan_npc\" type=\"npc\"", dayMap);
        Assert.Contains("character_id\" value=\"sister_baitan\"", dayMap);
        Assert.Contains("sister_wine_reminder_01.yaml", dayMap);

        Assert.Contains("name=\"manor_master_npc\" type=\"npc\"", dayMap);
        Assert.Contains("character_id\" value=\"master\"", dayMap);
        Assert.Contains("master_study_01.yaml", dayMap);

        Assert.Contains("name=\"junior_brother_npc\" type=\"npc\"", dayMap);
        Assert.Contains("character_id\" value=\"junior_brother\"", dayMap);
        Assert.Contains("manor_errands_01.yaml", dayMap);

        Assert.DoesNotContain("type=\"npc\"", nightMap);
    }

    [Fact]
    public void SceneBlockedTiles_AreVisuallyMarkedOnTheIsoMap()
    {
        var sceneBase = ReadFile("feng-zhi/scripts/SceneGameBase.cs");

        Assert.Contains("Name = \"BlockedTileCollision\"", sceneBase);
        Assert.Contains("Name = \"BlockedTileOverlay\"", sceneBase);
        Assert.Contains("Name = \"BlockedTileOutline\"", sceneBase);
        Assert.Contains("BlockedTileOverlayFill", sceneBase);
        Assert.Contains("BlockedTileOverlayOutline", sceneBase);
        Assert.Contains("ZIndex = 20", sceneBase);
        Assert.Contains("ZIndex = 21", sceneBase);
        Assert.Contains("body.AddChild(CreateBlockedTileOverlay())", sceneBase);
        Assert.Contains("body.AddChild(CreateBlockedTileOutline())", sceneBase);
    }

    [Fact]
    public void PrologueLateBeats_AreGatedByEarlierQuestFlags()
    {
        var livingQuarter = ReadFile("feng-zhi/scripts/LivingQuarterGame.cs");
        var mountainPath = ReadFile("feng-zhi/scripts/BackMountainPathGame.cs");
        var cave = ReadFile("feng-zhi/scripts/BackMountainCliffCaveGame.cs");
        var compound = ReadFile("feng-zhi/scripts/SectCompoundGame.cs");
        var mainHall = ReadFile("feng-zhi/scripts/MainHallGame.cs");
        var study = ReadFile("feng-zhi/scripts/StudyGame.cs");

        Assert.Contains("HasQuestFlag(\"prologue_herb_tutorial_seen\")", livingQuarter);
        Assert.Contains("HasQuestFlag(\"prologue_ore_tutorial_seen\")", mountainPath);
        Assert.Contains("HasQuestFlag(\"prologue_wine_delayed\")", cave);
        Assert.Contains("HasQuestFlag(\"prologue_wine_obtained\")", cave);
        Assert.Contains("HasQuestFlag(\"prologue_cave_overnight\")", mountainPath);

        Assert.Contains("HasQuestFlag(\"prologue_massacre_discovered\")", compound);
        Assert.Contains("HasQuestFlag(\"prologue_senior_brother_returned\")", compound);
        Assert.Contains("HasQuestFlag(\"senior_brother_mis_resolved\")", compound);
        Assert.Contains("HasQuestFlag(\"prologue_joint_burial_completed\")", mainHall);

        Assert.Contains("SetQuestFlag(\"books_organized\")", study);
        Assert.Contains("SetQuestFlag(\"prologue_study_compartment_empty_seen\")", study);
    }

    private static string ReadSceneScripts()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            "feng-zhi/scripts/LivingQuarterGame.cs",
            "feng-zhi/scripts/BackMountainPathGame.cs",
            "feng-zhi/scripts/SectCompoundGame.cs",
            "feng-zhi/scripts/MainHallGame.cs",
            "feng-zhi/scripts/StudyGame.cs",
            "feng-zhi/scripts/BackMountainCliffCaveGame.cs"
        };

        return string.Join("\n", files.Select(file => File.ReadAllText(Path.Combine(root, file))));
    }

    private static string ReadFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FengZhi.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("无法定位 FengZhi.slnx 所在的仓库根目录。");
    }
}
