using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using LD60.Helpers;
using LD60.scenes.secondary;

namespace LD60.scenes;

public partial class Game : Control,
    IRecipient<DialogueFinishedMessage>
{
    private const int NumberOfKeySounds = 3;
    // How much to insert between repeats of the morse phrase (used only when displaying a new letter)
    private const string PhraseGap = "  ";

    [Export(PropertyHint.File, "*.tscn")]
    public string NextScenePath { get; set; } = "res://scenes/credits.tscn";

    [Export]
    public Label? LetterLabel { get; set; }

    [Export]
    public Label? MorseLabel { get; set; }

    [Export]
    public Label? TypeLabel { get; set; }

    [Export]
    public Label? CaretLabel { get; set; }

    [Export]
    public Button? RepeatPhraseButton { get; set; }

    [Export]
    public Button? StopPlaybackButton { get; set; }

    [Export]
    public Button? PausePlaybackButton { get; set; }

    private AudioManager? _audioManager;
    private MorseManager? _morseManager;

    private double _messageDelay;
    private int _gameStage = -1;
    private bool _sentCurrentInstruction;
    private GameInstruction? _currentInstruction;

    private bool _isPlaybackPaused;

    private readonly GameInstruction[] _instructions =
    [
        new(2.0f, [
            "It was a [color=#000000]dark[/color] and [color=#005C7F]stormy night[/color].",
            "I was tasked yet again to monitor the comms...",
        ]),
        new(2.0f, [
            "That's when I heard it, a [color=#FF0000]distress call[/color].",
            "I could just make it out, even though it was distorted."
        ], "SOS", IsProsign: true),
        new(0.5f, [
            "There it is again... but there's [color=#FFB800]no ships[/color] around us?",
            "[i]Use your keyboard to decode the message, then press [color=#FFB800]Enter[/color][/i]."
        ], "SOS", IsProsign: true, NeedInput: true),
        new(1.5f, [
            "[i][color=#FFB800]Good job![/color] You played the tutorial, I hope, or know [color=#FFB800]Morse Code[/color][/i]?",
            "[color=#FFB800][b]Who[/b][/color] is sending this message? I thought to myself."
        ]),
        new(2.0f, [
            "Then a message came; I scrambled for my [b]memo pad[/b].",
            "[i]Use your [color=#FFB800]keyboard[/color] to decode the message, then press [color=#FFB800]Enter[/color][/i]."
        ], "MEMO TO ME", true),
        new(1.5f, [
            "That wasn't so hard. But there's something new here...",
            "[i]You know the drill now. But can you figure out this message[/i]?"
        ], "TIME IS LOST", true),
        new(2.0f, [
            "[i][color=#FFB800]Great job![/color] You've learned a new [color=#FFB800]letter[/color][/i]!",
            "[i]This sequence is the letter '[color=#FFB800]L[/color]'. You're [b]writing this down[/b], right?[/i]."
        ], "L", false, true),
        new(1.5f, [
            "Ok, that's a bit [color=#000000]odd[/color]. This is a [color=#FF0000]distress call[/color]?",
            "Am I really that tired already? My shift just began."
        ]),
        new(0.5f, [
            "Yup, 'MEMO TO ME: TIME IS LOST'. I'm losing my time...",
            "I was just about to go grab some [color=#000000]coffee[/color]..."
        ]),
        new(0.5f, [
            "Alright, another sequence. I [color=#FFB800]cracked my knuckles[/color]...",
            "This one seems harder. Just me! Everyone else is asleep.",
        ], "ILL SEE SOON", true),
        new(2.0f, [
            "[i][color=#FFB800]Good job![/color] You've learned a new [color=#FFB800]letter[/color][/i]!",
            "[i]This sequence is the letter '[color=#FFB800]N[/color]'. [b]Write it down[/b][/i]!"
        ], "N", false, true),
        new(0.5f, [
            "[color=#FFB800]What[/color] will they see soon? I asked.",
            "And [color=#FFB800]Who[/color] is sending this message in the [color=#000000]night[/color]?",
        ]),
        new(1.5f, [
            "[i]Thanks for playing my LD60 compo entry!",
            "[i]This is all the content of the game for now, [color=#FFB800]great work[/color] decoding[/i]!",
        ]),
        new(0.5f, [
            "[i]Made [color=#FFB800]by hand[/color] in less than 48-hours with [color=#FFB800]C#[/color] and [color=#FFB800]Godot[/color][/i].",
            "[i]Web export with C# made possible by [color=#FFB800]2dog[/color][/i]."
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

        Shell.Instance?.PreloadScene(NextScenePath);
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
        // If displaying a new letter, repeat the phrase automatically
        if (_currentInstruction is { DisplayPhrase: true })
        {
            _morseManager?.Queue(_currentInstruction.MorsePhrase + PhraseGap);
        }
        else
        {
            // Re-enable play again button after playing phrase
            RepeatPhraseButton?.Disabled = false;
            StopPlaybackButton?.Disabled = true;
            PausePlaybackButton?.Disabled = true;
            if (_isPlaybackPaused) OnPauseButtonPressed();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!_isPlaybackPaused)
        {
            _morseManager?.Process(delta);
        }

        // Use delay as it continually counts and can be our "time" factor, occasionally it'll get mucked with but fine...
        CaretLabel?.Modulate = CaretLabel.Modulate with { A = 0.5f * (1.0f + MathF.Sin((float)(2f * Math.PI * _messageDelay))) };

        _messageDelay -= delta;
        if (_messageDelay <= 0 &&
            _currentInstruction != null &&
            !_sentCurrentInstruction)
        {
            _sentCurrentInstruction = true;

            // Reset Text
            LetterLabel?.Text = string.Empty;
            MorseLabel?.Text = string.Empty;
            MorseLabel?.Modulate = Colors.White;
            CaretLabel?.Modulate = Colors.White;
            TypeLabel?.Modulate = Colors.White;
            TypeLabel?.Text = string.Empty;
            CaretLabel?.Visible = _currentInstruction.NeedInput;
            RepeatPhraseButton?.Visible = _currentInstruction.NeedInput;
            StopPlaybackButton?.Visible = _currentInstruction.NeedInput;
            PausePlaybackButton?.Visible = _currentInstruction.NeedInput;
            RepeatPhraseButton?.Disabled = true; // will be turned on after initial play of phrase
            StopPlaybackButton?.Disabled = false;
            PausePlaybackButton?.Disabled = false;

            // Unpause if paused
            if (_isPlaybackPaused) OnPauseButtonPressed();

            // Display Next Message
            // TODO: Do we want a flag for not playing the dialogue sounds?
            StrongReferenceMessenger.Default.Send<DisplayDialogueMessage>(new(_currentInstruction.Message, !_currentInstruction.NeedInput));

            // TODO: Maybe want bool for if happens immediately or after dialogue?
            if (_currentInstruction.MorsePhrase != null)
            {
                if (_currentInstruction.DisplayPhrase)
                {
                    MorseLabel?.Text = _currentInstruction.MorsePhrase;
                    LetterLabel?.Text = _currentInstruction.MorsePhrase;

                    // Ensure we stop any playing morse, but will re-trigger audio with gap for repeat
                    _morseManager?.Clear();
                }
                else
                {
                    _morseManager?.Queue(_currentInstruction.MorsePhrase, _currentInstruction.IsProsign);
                }
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
                MorseLabel?.Text = TypeLabel?.Text;
                CaretLabel?.Modulate = Colors.White;
                TypeLabel?.Modulate = Colors.White;
                MorseLabel?.Modulate = Colors.White;
            }
            // Only allow Space after other letters
            else if (eventKey.Keycode == Key.Space &&
                     TypeLabel?.Text.Length > 0)
            {
                // Prevent pressing the Repeat Button
                GetViewport().SetInputAsHandled();

                pressedKey = true;
                TypeLabel?.Text += " ";
                MorseLabel?.Text += " ";
            }
            else if (rep.Length == 1 && char.IsAsciiLetterOrDigit(rep[0]))
            {
                pressedKey = true;
                TypeLabel?.Text += OS.GetKeycodeString(eventKey.Keycode).ToUpper();
                MorseLabel?.Text += OS.GetKeycodeString(eventKey.Keycode).ToUpper();
                CaretLabel?.Modulate = Colors.White;
                TypeLabel?.Modulate = Colors.White;
                MorseLabel?.Modulate = Colors.White;
            }
            else if (eventKey.Keycode == Key.Enter ||
                     eventKey.Keycode == Key.KpEnter)
            {
                // Submit
                pressedKey = true;
                if (TypeLabel?.Text == _currentInstruction.MorsePhrase)
                {
                    // Prevent pressing the Repeat Button
                    GetViewport().SetInputAsHandled();

                    // Reset Pause and Morse buffer
                    _morseManager?.Clear();
                    if (_isPlaybackPaused) OnPauseButtonPressed();

                    // Success
                    CaretLabel?.Modulate = Color.FromHtml("FFB800");
                    TypeLabel?.Modulate = Color.FromHtml("FFB800");
                    MorseLabel?.Modulate = Color.FromHtml("FFB800");
                    _audioManager?.Play($"res://sounds/correct.wav", true,
                        0.9f + Random.Shared.NextSingle() / 20f, 0.9f + Random.Shared.NextSingle() / 10f);

                    Receive(new DialogueFinishedMessage());
                }
                else
                {
                    // Failure
                    CaretLabel?.Modulate = Colors.Red;
                    TypeLabel?.Modulate = Colors.Red;
                    MorseLabel?.Modulate = Colors.Red;
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
        _gameStage++;
        if (_gameStage < _instructions.Length)
        {
            _currentInstruction = _instructions[_gameStage];
            _sentCurrentInstruction = false;
            _messageDelay = _currentInstruction.Delay;
        }
        else
        {
            // Game completed: Navigate to Credits Scene...
            Shell.Instance?.TransitionTo(NextScenePath);
        }
    }

    public void OnRepeatPhraseButtonPressed()
    {
        // Allows player to replay phrase at their leisure when "AutoRepeat" is off in Tutorial Instruction
        if (_currentInstruction != null &&
            _currentInstruction.MorsePhrase != null)
        {
            // Note: We don't use the Phrase Gap here as it won't automatically repeat after!
            _morseManager?.Queue(_currentInstruction.MorsePhrase, _currentInstruction.IsProsign);

            // Disable the button while the phrase plays
            RepeatPhraseButton?.Disabled = true;
            StopPlaybackButton?.Disabled = false;
            PausePlaybackButton?.Disabled = false;
            if (_isPlaybackPaused) OnPauseButtonPressed();
        }
    }

    public void OnStopPlaybackButtonPressed()
    {
        _morseManager?.Clear();

        PausePlaybackButton?.Disabled = true;
        if (_isPlaybackPaused) OnPauseButtonPressed();
    }

    public void OnPauseButtonPressed()
    {
        _isPlaybackPaused = !_isPlaybackPaused;
        PausePlaybackButton?.Text = _isPlaybackPaused ? "UNPAUSE" : "PAUSE";
    }
}

// Delay is the time before the message is displayed after the last one is dismissed.
// Message is the text to display (2 lines max)
public record GameInstruction(double Delay, string[] Message,
    // Morse Phrase is the character in Morse to display (if any)
    string? MorsePhrase = null,
    // Need Input means the player has to type the characters to proceed (off when displaying new information)
    bool NeedInput = false,
    // Display Phrase shows the ASCII character equivalent and not just the dots and dashes (uncommon)
    bool DisplayPhrase = false,
    // IsProsign is passed to MorseManager to reduce letter gap TODO: Deal with longer passages later that have them...
    bool IsProsign = false);
