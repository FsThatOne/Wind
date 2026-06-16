// VERTICAL SLICE - NOT FOR PRODUCTION
// Validation Question: Can a player experience Burst+Read combat + mindset choice + blurred UI in 3-5 min?
// Date: 2026-06-10
using Godot;
using System;
using System.Collections.Generic;

namespace FengzhiSlice;

/// <summary>
/// Minimal dialogue manager for vertical slice.
/// Handles pre-combat narrative and post-combat mindset choice.
/// </summary>
public partial class DialogueManager : Node
{
    [Signal] public delegate void DialogueLineShownEventHandler(string speaker, string text);
    [Signal] public delegate void ChoicesPresentedEventHandler(string[] choices);
    [Signal] public delegate void DialogueCompleteEventHandler();
    
    private Queue<DialogueLine> _currentLines = new();
    private string _currentDialogueId = "";
    private bool _waitingForChoice;
    
    public bool IsActive => _currentLines.Count > 0 || _waitingForChoice;
    
    public override void _Ready()
    {
        EventBus.DialogueStarted += OnDialogueStarted;
    }
    
    public override void _ExitTree()
    {
        EventBus.DialogueStarted -= OnDialogueStarted;
    }
    
    private void OnDialogueStarted(string dialogueId)
    {
        _currentDialogueId = dialogueId;
        _currentLines.Clear();
        _waitingForChoice = false;
        
        switch (dialogueId)
        {
            case "pre_combat":
                LoadPreCombatDialogue();
                break;
            case "post_combat_choice":
                LoadPostCombatChoice();
                break;
        }
        
        AdvanceDialogue();
    }
    
    private void LoadPreCombatDialogue()
    {
        _currentLines.Enqueue(new DialogueLine("旁白", "落尘谷，薄雾中。你循着线索来到此处。"));
        _currentLines.Enqueue(new DialogueLine("旁白", "一个持刀的身影挡住了去路。"));
        _currentLines.Enqueue(new DialogueLine("山贼头目", "风止山庄？哈，那已经是一片死地了。你从坟里爬出来的？"));
        _currentLines.Enqueue(new DialogueLine("无名", "……让开。"));
        _currentLines.Enqueue(new DialogueLine("山贼头目", "好大的口气。那晚灭门的事，你想知道？先过了我这一关再说！"));
    }
    
    private void LoadPostCombatChoice()
    {
        _currentLines.Enqueue(new DialogueLine("旁白", "山贼头目跪倒在地，刀已脱手。"));
        _currentLines.Enqueue(new DialogueLine("山贼头目", "饶……饶命！灭门那晚，我只是看门的……我知道些事情……"));
        _currentLines.Enqueue(new DialogueLine("旁白", "你握紧了手中的尺。"));
        // After this line, present choice
    }
    
    public void AdvanceDialogue()
    {
        if (_waitingForChoice) return;
        
        if (_currentLines.Count > 0)
        {
            var line = _currentLines.Dequeue();
            GD.Print($"[Dialogue] {line.Speaker}: {line.Text}");
            EmitSignal(SignalName.DialogueLineShown, line.Speaker, line.Text);
            
            // If no more lines and this is post-combat, present choice
            if (_currentLines.Count == 0 && _currentDialogueId == "post_combat_choice")
            {
                PresentMindsetChoice();
            }
        }
        else
        {
            CompleteDialogue();
        }
    }
    
    private void PresentMindsetChoice()
    {
        _waitingForChoice = true;
        string[] choices = new[]
        {
            "杀了他。仇人的帮凶，不该活。",    // 执念 +0.2
            "留他一命。也许他的线索更有用。"     // 释怀 +0.2, 入世 +0.1
        };
        GD.Print("[Dialogue] Choice presented: Kill or Spare");
        EmitSignal(SignalName.ChoicesPresented, choices);
    }
    
    public void SelectChoice(int index)
    {
        if (!_waitingForChoice) return;
        _waitingForChoice = false;
        
        MindsetChoice choice;
        if (index == 0)
        {
            // Kill — obsession increases
            choice = new MindsetChoice("kill_bandit", 0.2f, 0f);
            GD.Print("[Dialogue] Player chose: KILL (执念 +0.2)");
        }
        else
        {
            // Spare — release + engagement
            choice = new MindsetChoice("spare_bandit", -0.2f, 0.1f);
            GD.Print("[Dialogue] Player chose: SPARE (释怀 +0.2, 入世 +0.1)");
        }
        
        EventBus.PublishMindsetChoice(choice);
        CompleteDialogue();
    }
    
    private void CompleteDialogue()
    {
        GD.Print($"[Dialogue] Complete: {_currentDialogueId}");
        EmitSignal(SignalName.DialogueComplete);
        EventBus.PublishDialogueEnded();
    }
}

public record DialogueLine(string Speaker, string Text);
