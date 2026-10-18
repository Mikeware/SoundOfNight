using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using LD60.Helpers;
using LD60.scenes.secondary;

namespace LD60.scenes;

public partial class Tutorial : Control,
    IRecipient<DialogueFinishedMessage>
{
    public const int NumberOfKeySounds = 3;

    [Export]
    public Label? LetterLabel { get; set; }

    [Export]
    public Label? MorseLabel { get; set; }

    [Export]
    public Label? TypeLabel { get; set; }

    private AudioManager? _audioManager;
    private MorseManager? _morseManager;

    private double _messageDelay;
    private int _tutorialStage = -1;
    private string? _currentPhrase;
    private bool _sentCurrentInstruction;
    private TutorialInstruction? _currentInstruction;

    private readonly TutorialInstruction[] _instructions =
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
        new(0.5f, [
            "Dots (or dits) are [color=#FFB800]one unit[/color] long",
            "Dashes (or dahs) are [color=#FFB800]three[/color] units long."
        ], "ET", false), // single . and dash -
        new(0.5f, [
            "A single '[color=#FFB800].[/color]' is the letter '[color=#FFB800]E[/color]'.",
            "And a single '[color=#FFB800]-[/color]' is the letter '[color=#FFB800]T[/color]'."
        ], "ET", true),
        new(0.5f, [
            "Type the letters '[color=#FFB800]E[/color]' and '[color=#FFB800]T[/color]' on your keyboard.",
            "Then press [color=#FFB800]Enter[/color] to continue. Use [color=#FFB800]Backspace[/color] to correct."
        ], "ET", true, true),
        new(0.5f, [
            "[color=#FFB800]Great job![/color]",
            "..."
        ]),
    ];

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _audioManager = GetNode<AudioManager>("/root/AudioManager");
        _morseManager = new(_audioManager);
        _morseManager.QueueCompleted += MorseManagerOnQueueCompleted;

        // Trigger initial dialogue to keep it all in one place
        Receive(new DialogueFinishedMessage());
    }

    public override void _EnterTree()
    {
        StrongReferenceMessenger.Default.Register<DialogueFinishedMessage>(this);
    }

    public override void _ExitTree()
    {
        StrongReferenceMessenger.Default.Unregister<DialogueFinishedMessage>(this);
    }

    private void MorseManagerOnQueueCompleted(object? sender, EventArgs e)
    {
        if (_currentPhrase != null)
        {
            _morseManager?.Queue(_currentPhrase);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        _morseManager?.Process(delta);

        _messageDelay -= delta;
        if (_messageDelay <= 0 &&
            _currentInstruction != null &&
            !_sentCurrentInstruction)
        {
            _sentCurrentInstruction = true;

            // TODO: Do we want a flag for not playing the dialogue sounds?
            StrongReferenceMessenger.Default.Send<DisplayDialogueMessage>(new(_currentInstruction.Message, !_currentInstruction.NeedInput));

            // TODO: Maybe want bool for if happens immediately or after dialogue?
            if (_currentInstruction.MorsePhrase != null)
            {
                MorseLabel?.Text = _currentInstruction.MorsePhrase;
                if (_currentInstruction.DisplayPhrase)
                {
                    LetterLabel?.Text = _currentInstruction.MorsePhrase;
                }

                _currentPhrase = _currentInstruction.MorsePhrase + "  ";
                _morseManager?.Queue(_currentPhrase);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_currentInstruction?.NeedInput == true &&
            @event is InputEventKey eventKey &&
            @event.IsReleased() &&
            !eventKey.Echo)
        {
            bool pressedKey = false;
            string rep = OS.GetKeycodeString(DisplayServer.KeyboardGetLabelFromPhysical(eventKey.PhysicalKeycode));
            if (eventKey.Keycode == Key.Backspace &&
                TypeLabel?.Text.Length > 0)
            {
                pressedKey = true;
                TypeLabel?.Text = TypeLabel.Text.Substring(0, TypeLabel.Text.Length - 1);
                TypeLabel?.Modulate = Colors.White;
            }
            else if (eventKey.Keycode == Key.Space)
            {
                pressedKey = true;
                TypeLabel?.Text += " ";
            }
            else if (rep.Length == 1 && char.IsAsciiLetterOrDigit(rep[0]))
            {
                pressedKey = true;
                TypeLabel?.Text += OS.GetKeycodeString(eventKey.Keycode).ToUpper();
            }
            else if (eventKey.Keycode == Key.Enter)
            {
                // Submit
                pressedKey = true;
                if (TypeLabel?.Text == _currentInstruction.MorsePhrase)
                {
                    // Success
                    _audioManager?.Play($"res://sounds/correct.wav", true,
                        0.9f + Random.Shared.NextSingle() / 20f, 0.9f + Random.Shared.NextSingle() / 10f);

                    Receive(new DialogueFinishedMessage());
                }
                else
                {
                    // Failure
                    TypeLabel?.Modulate = Colors.Red;
                    _audioManager?.Play($"res://sounds/incorrect.wav", true,
                        0.9f + Random.Shared.NextSingle() / 20f, 0.9f + Random.Shared.NextSingle() / 10f);
                }
            }

            // Key Sound
            if (pressedKey)
            {
                _audioManager?.Play($"res://sounds/key{Random.Shared.Next(NumberOfKeySounds)}.wav", true,
                    0.9f + Random.Shared.NextSingle() / 20f, 0.9f + Random.Shared.NextSingle() / 10f);
            }
        }
    }

    public void Receive(DialogueFinishedMessage message)
    {
        _tutorialStage++;
        if (_tutorialStage < _instructions.Length)
        {
            _currentInstruction = _instructions[_tutorialStage];
            _sentCurrentInstruction = false;
            _messageDelay = _currentInstruction.Delay;
        }
    }
}

public record TutorialInstruction(double Delay, string[] Message, string? MorsePhrase = null, bool DisplayPhrase = false, bool NeedInput = false);
