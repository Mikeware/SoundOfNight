using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using LD60.Helpers;
using LD60.scenes.secondary;

namespace LD60.scenes;

public partial class Tutorial : Control,
    IRecipient<DialogueFinished>
{
    [Export]
    public Label? LetterLabel { get; set; }

    [Export]
    public Label? MorseLabel { get; set; }

    [Export]
    public Label? TypeLabel { get; set; }

    private AudioManager? _audioManager;
    private MorseManager? _morseManager;

    private double _messageDelay;
    private int _tutorialStage;
    private TutorialInstruction? _currentInstruction;

    private TutorialInstruction[] _instructions =
    [
        new(2.0f, [
            "Welcome! This box will have important instructions.",
            "[color=#FFB800]Click[/color] or press [color=#FFB800]Enter[/color] to dismiss it.",
        ]),
        new(0.5f, [
            "To play this game you need to learn [color=#FFB800]Morse Code[/color].",
            "We can teach you. If you know already, press [color=#FFB800]Skip[/color]."
        ]),
        new(0.5f, [
            "[color=#FFB800]Morse Code[/color] is a system of encoding messages using ",
            "[color=#FFB800]dots[/color] '[color=#FFB800].[/color]' and [color=#FFB800]dashes[/color] '[color=#FFB800]-[/color]', short and long audio tones."
        ]),
    ];

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _audioManager = GetNode<AudioManager>("/root/AudioManager");
        _morseManager = new(_audioManager);
        _morseManager.QueueCompleted += MorseManagerOnQueueCompleted;

        // Trigger initial dialogue to keep it all in one place
        Receive(new());
    }

    public override void _EnterTree()
    {
        StrongReferenceMessenger.Default.Register<DialogueFinished>(this);
    }

    public override void _ExitTree()
    {
        StrongReferenceMessenger.Default.Unregister<DialogueFinished>(this);
    }

    private void MorseManagerOnQueueCompleted(object? sender, EventArgs e)
    {
        // Put a small gap before we start again
        _morseManager?.Queue("S  ");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        _morseManager?.Process(delta);

        _messageDelay -= delta;
        if (_messageDelay <= 0 && _currentInstruction != null)
        {
            // TODO: Do we want a flag for not playing the dialogue sounds?
            StrongReferenceMessenger.Default.Send<DisplayDialogueMessage>(new(_currentInstruction.Message));

            // TODO: Setup display and audio ping for the current instruction as well...? (Maybe want bool for if happens immediately or after dialogue?)

            _tutorialStage++;
            _currentInstruction = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey &&
            @event.IsReleased() &&
            !eventKey.Echo)
        {
            string rep = OS.GetKeycodeString(DisplayServer.KeyboardGetLabelFromPhysical(eventKey.PhysicalKeycode));
            if (eventKey.Keycode == Key.Backspace &&
                TypeLabel?.Text.Length > 0)
            {
                TypeLabel?.Text = TypeLabel.Text.Substring(0, TypeLabel.Text.Length - 1);
            }
            else if (eventKey.Keycode == Key.Space)
            {
                TypeLabel?.Text += " ";
            }
            else if (rep.Length == 1 && char.IsAsciiLetterOrDigit(rep[0]))
            {
                TypeLabel?.Text += OS.GetKeycodeString(eventKey.Keycode).ToUpper();
            }
            else if (eventKey.Keycode == Key.Enter)
            {
                // TODO: Submit (if needed)
            }
        }
    }

    public void Receive(DialogueFinished message)
    {
        if (_tutorialStage < _instructions.Length)
        {
            _currentInstruction = _instructions[_tutorialStage];
            _messageDelay = _currentInstruction.Delay;
        }
    }
}

public record TutorialInstruction(double Delay, string[] Message, string? MorsePhrase = null, bool DisplayPhrase = false);
