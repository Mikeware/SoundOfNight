using System;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using LD60.Helpers;

namespace LD60.scenes.secondary;

public partial class DialoguePanel : PanelContainer,
    IRecipient<DisplayDialogueMessage>,
    IRecipient<DismissDialogueMessage>
{
    public const int NumberOfTypeSounds = 10;

    [Export]
    public Timer? NextLetterTimer { get; set; }

    [Export]
    public RichTextLabel? DialogueLabel { get; set; }

    private AudioManager? _audioManager;
    private string[]? _currentBlock;
    private int _typeOdd;
    private int _currentLine;
    private int _currentCharacter;

    private bool _isDismissable = true;

    public override void _EnterTree()
    {
        StrongReferenceMessenger.Default.Register<DisplayDialogueMessage>(this);
        StrongReferenceMessenger.Default.Register<DismissDialogueMessage>(this);
    }

    public override void _ExitTree()
    {
        StrongReferenceMessenger.Default.Unregister<DisplayDialogueMessage>(this);
        StrongReferenceMessenger.Default.Unregister<DismissDialogueMessage>(this);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _audioManager = GetNode<AudioManager>("/root/AudioManager");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.IsReleased())
        {
            NextDialogueLine();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Visible && Input.IsActionJustPressed("ui_accept"))
        {
            NextDialogueLine();
        }
    }

    private void NextDialogueLine()
    {
        // Have we stopped 'typing' out the message?
        if (NextLetterTimer?.IsStopped() == true)
        {
            FinishDialogue();
        }
        else
        {
            // User wants to skip ahead, cheat and just skip to the character before last so we don't reduplicate our next line logic...
            DialogueLabel?.Text += _currentBlock![_currentLine][_currentCharacter..^1];
            _currentCharacter = _currentBlock![_currentLine].Length - 1;
        }
    }

    public void NextLetterTimer_Timeout()
    {
        string line = _currentBlock![_currentLine];
        // copy over BBCode instruction
        if (line[_currentCharacter] == '[')
        {
            int len = line.Find("]", _currentCharacter) - _currentCharacter;
            DialogueLabel?.Text += line.Substring(_currentCharacter, len);
            _currentCharacter += len;
        }
        DialogueLabel?.Text += _currentBlock![_currentLine][_currentCharacter++];

        // Play Sound, Periodically // TODO: Make based on integer to vary with nextletter time too? maybe can make that a bit faster and make this every 3rd?
        _typeOdd++;
        if (_typeOdd >= 2)
        {
            _typeOdd = 0;
            _audioManager?.Play($"res://sounds/type{Random.Shared.Next(NumberOfTypeSounds)}.wav", true,
                0.95f + Random.Shared.NextSingle() / 10f, 0.1f + Random.Shared.NextSingle() / 10f);
        }

        // Have we reached the end of the line?
        if (_currentCharacter >= _currentBlock![_currentLine].Length)
        {
            DialogueLabel?.Text += '\n'; // Add Newline
            _currentCharacter = 0;
            _currentLine++;

            // Have we reached the end of this entire "line" of dialogue?
            if (_currentLine >= _currentBlock.Length)
            {
                _currentLine = 0;

                // Wait for input before proceeding to next "line" or closing dialogue
                NextLetterTimer?.Stop();
            }
        }
    }

    public void Receive(DisplayDialogueMessage message)
    {
        // Clear prior text
        DialogueLabel?.Text = string.Empty;

        //GetTree().Paused = true;
        Visible = true;

        _isDismissable = message.IsDismissable;
        _currentBlock = message.Lines;
        NextLetterTimer?.Start();
    }

    private void FinishDialogue()
    {
        if (_isDismissable)
        {
            NextLetterTimer?.Stop();

            Visible = false;
            //GetTree().Paused = false;

            StrongReferenceMessenger.Default.Send<DialogueFinishedMessage>(new());
        }
    }

    public void Receive(DismissDialogueMessage message)
    {
        _isDismissable = true;
        FinishDialogue();
    }
}

public record DisplayDialogueMessage(string[] Lines, bool IsDismissable = true);

public record DismissDialogueMessage();

public record DialogueFinishedMessage();
