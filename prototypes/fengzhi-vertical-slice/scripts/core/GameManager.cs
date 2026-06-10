// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using Godot;

namespace FengzhiSlice;

/// <summary>
/// Game flow controller. Manages state transitions:
/// Dialogue (pre-combat) → Combat → Dialogue (post-combat choice) → Feedback
/// </summary>
public partial class GameManager : Node
{
    public enum GamePhase
    {
        PreCombatDialogue,
        Combat,
        PostCombatChoice,
        BlurredFeedback,
        Complete
    }
    
    public GamePhase CurrentPhase { get; private set; } = GamePhase.PreCombatDialogue;
    public CharacterData Player { get; private set; } = null!;
    public CharacterData Enemy { get; private set; } = null!;
    
    public override void _Ready()
    {
        Player = CharacterData.CreatePlayer();
        Enemy = CharacterData.CreateBanditEnemy();
        
        EventBus.CombatEnded += OnCombatEnded;
        EventBus.MindsetChoiceMade += OnMindsetChoice;
        EventBus.DialogueEnded += OnDialogueEnded;
        
        // Wire sub-systems after one frame so all nodes are ready
        Callable.From(WireSubSystems).CallDeferred();
    }
    
    private void WireSubSystems()
    {
        // Start the game loop
        TransitionTo(GamePhase.PreCombatDialogue);
    }
    
    public override void _ExitTree()
    {
        EventBus.CombatEnded -= OnCombatEnded;
        EventBus.MindsetChoiceMade -= OnMindsetChoice;
        EventBus.DialogueEnded -= OnDialogueEnded;
    }
    
    private void OnDialogueEnded()
    {
        if (CurrentPhase == GamePhase.PreCombatDialogue)
        {
            TransitionTo(GamePhase.Combat);
        }
    }
    
    public void TransitionTo(GamePhase phase)
    {
        CurrentPhase = phase;
        GD.Print($"[GameManager] Phase → {phase}");
        
        switch (phase)
        {
            case GamePhase.PreCombatDialogue:
                EventBus.PublishDialogueStarted("pre_combat");
                break;
            case GamePhase.Combat:
                EventBus.PublishCombatStarted();
                InitializeCombat();
                break;
            case GamePhase.PostCombatChoice:
                EventBus.PublishDialogueStarted("post_combat_choice");
                break;
            case GamePhase.BlurredFeedback:
                GenerateBlurredFeedback();
                break;
            case GamePhase.Complete:
                GD.Print("[GameManager] === VERTICAL SLICE COMPLETE ===");
                break;
        }
    }
    
    private void OnCombatEnded(CombatResult result)
    {
        if (result.PlayerWon)
        {
            TransitionTo(GamePhase.PostCombatChoice);
        }
        else
        {
            // For the slice, just restart
            Player = CharacterData.CreatePlayer();
            Enemy = CharacterData.CreateBanditEnemy();
            TransitionTo(GamePhase.PreCombatDialogue);
        }
    }
    
    private void OnMindsetChoice(MindsetChoice choice)
    {
        Player.ApplyMindsetShift(choice.ObsessionDelta, choice.EngagementDelta);
        TransitionTo(GamePhase.BlurredFeedback);
    }
    
    private void InitializeCombat()
    {
        var combatManager = GetTree().Root.GetNodeOrNull<CombatManager>("Main/CombatManager");
        var combatUI = GetTree().Root.GetNodeOrNull<CombatUI>("Main/UILayer/CombatUI");
        
        if (combatManager != null)
        {
            combatManager.Initialize(Player, Enemy);
            if (combatUI != null) combatUI.BindCombatManager(combatManager);
            combatManager.BeginCombat();
        }
    }
    
    private void GenerateBlurredFeedback()
    {
        // 朦胧化 feedback — literary, not numeric
        string gongliText = Player.GongliRank switch
        {
            1 => "初窥门径，内息尚浅",
            2 => "气沉丹田，已可与寻常武者一战",
            3 => "内力渐厚，招式之间已有连贯之意",
            _ => "功参造化"
        };
        
        string mindsetText;
        if (Player.Obsession > 0.3f)
            mindsetText = "心中执念渐深，仇恨如火——这条路的尽头，你看得见吗？";
        else if (Player.Obsession < -0.3f)
            mindsetText = "心境渐趋平和，风过无痕——放下，也许是另一种力量。";
        else
            mindsetText = "心如止水，尚未偏向——前路仍有抉择。";
        
        string feedback = $"【{gongliText}】\n\n{mindsetText}";
        EventBus.PublishBlurredFeedback(feedback);
    }
}
