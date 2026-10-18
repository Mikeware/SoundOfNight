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
    private const int NumberOfKeySounds = 3;
    // How much to insert between repeats of the morse phrase
    private const string PhraseGap = "  ";

    [Export]
    public Label? LetterLabel { get; set; }

    [Export]
    public Label? MorseLabel { get; set; }

    [Export]
    public Label? TypeLabel { get; set; }

    [Export]
    public Label? CaretLabel { get; set; }

    [Export]
    public Button? SkipTutorialsButton { get; set; }

    [Export]
    public Button? RepeatPhraseButton { get; set; }

    private AudioManager? _audioManager;
    private MorseManager? _morseManager;

    private double _messageDelay;
    private int _tutorialStage = -1;
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
            "Dots (or dits) are [color=#FFB800]one unit[/color] long.",
            "Dashes (or dahs) are [color=#FFB800]three[/color] units long."
        ], "ET", false), // single . and dash -
        new(0.5f, [
            "A single '[color=#FFB800].[/color]' is the letter '[color=#FFB800]E[/color]'. One '[color=#FFB800]-[/color]' is a '[color=#FFB800]T[/color]'.",
            "Next, morse plays once. Use [color=#FFB800]Repeat Phrase[/color] to play again."
        ], "ET", true),
        new(0.5f, [
            "Type the letters '[color=#FFB800]E[/color]' and '[color=#FFB800]T[/color]' on your keyboard.",
            "Then press [color=#FFB800]Enter[/color] to continue. Use [color=#FFB800]Backspace[/color] to correct."
        ], "ET", true, true, AutoRepeat: false),
        new(1.5f, [
            "[color=#FFB800]Great job![/color] Have you been writing these down?",
            "It'll be a good thing to remember. What letter is this?"
        ], "T", false, true, AutoRepeat: false),
        new(2.0f, [
            "[color=#FFB800]Good![/color] The letter '[color=#FFB800]S[/color]' is 3 dots '[color=#FFB800]...[/color]'",
            "The letter '[color=#FFB800]O[/color]' is 3 dashes '[color=#FFB800]---[/color]', type it:"
        ], "SOS", true, true, AutoRepeat: false),
        new(0.5f, [
            "'[color=#FF0000]SOS[/color]' is used as the start of a distress signal.",
            "It gets condensed as a [color=#FFB800]prosign[/color], we'll cover that later.",
        ], "SOS", IsProsign: true),
        new(0.5f, [
            "Two last letters and some practice before your journey.",
            "2 dots '[color=#FFB800]..[/color]' is the letter '[color=#FFB800]I[/color]' and 2 dashes '[color=#FFB800]--[/color]' is '[color=#FFB800]M[/color]'."
        ], "IM", true, true),
        new(0.5f, [
            "Between '.'s & '-'s of a [color=#FFB800]single letter[/color] is a [color=#FFB800]1 unit[/color] gap.",
            "But [color=#FFB800]3 units[/color] of gap between letters themselves. Hear it?",
        ], "MOSS", true),
        new(0.5f, [
            "We've added a [color=#FFB800]Space[/color]! Words create a [color=#FFB800]7 unit[/color] gap!",
            "Gaps between are important; pay [color=#FFB800]close attention[/color] to them!"
        ], "IM ME", true, true, AutoRepeat: false),
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
        if (_currentInstruction != null &&
            _currentInstruction.MorsePhrase != null &&
            _currentInstruction.AutoRepeat)
        {
            _morseManager?.Queue(_currentInstruction.MorsePhrase + PhraseGap, _currentInstruction.IsProsign);
        }
        else
        {
            RepeatPhraseButton?.Disabled = false;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        _morseManager?.Process(delta);

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
            CaretLabel?.Modulate = Colors.White;
            TypeLabel?.Modulate = Colors.White;
            TypeLabel?.Text = string.Empty;
            CaretLabel?.Visible = _currentInstruction.NeedInput;
            RepeatPhraseButton?.Visible = !_currentInstruction.AutoRepeat;
            RepeatPhraseButton?.Disabled = true; // will be turned on after initial play of phrase

            // Display Next Message
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

                _morseManager?.Queue(_currentInstruction.MorsePhrase + PhraseGap, _currentInstruction.IsProsign);
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
                CaretLabel?.Modulate = Colors.White;
                TypeLabel?.Modulate = Colors.White;
            }
            // Only allow Space after other letters
            else if (eventKey.Keycode == Key.Space &&
                     TypeLabel?.Text.Length > 0)
            {
                // Prevent pressing the Repeat Button
                GetViewport().SetInputAsHandled();

                pressedKey = true;
                TypeLabel?.Text += " ";
            }
            else if (rep.Length == 1 && char.IsAsciiLetterOrDigit(rep[0]))
            {
                pressedKey = true;
                TypeLabel?.Text += OS.GetKeycodeString(eventKey.Keycode).ToUpper();
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

                    // Success
                    CaretLabel?.Modulate = Color.FromHtml("FFB800");
                    TypeLabel?.Modulate = Color.FromHtml("FFB800");
                    _audioManager?.Play($"res://sounds/correct.wav", true,
                        0.9f + Random.Shared.NextSingle() / 20f, 0.9f + Random.Shared.NextSingle() / 10f);

                    Receive(new DialogueFinishedMessage());
                }
                else
                {
                    // Failure
                    CaretLabel?.Modulate = Colors.Red;
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
        }
    }

    public void OnSkipTutorialsPressed()
    {
        // TODO: Save skip tutorials to config
        // TODO: Navigate to Game Scene...
        Debug.WriteLine("TODO: Skipping Tutorials");
    }
}

// Delay is the time before the message is displayed after the last one is dismissed.
// Message is the text to display (2 lines max)
public record TutorialInstruction(double Delay, string[] Message,
    // Morse Phrase is the character in Morse to display (if any)
    // Display Phrase shows the ASCII character equivelent and not just the dots and dashes
    string? MorsePhrase = null, bool DisplayPhrase = false,
    // Need Input means the player has to type the characters to proceed
    // IsQuiz is the final bit of the tutorial where MorsePhrase characters are picked at random and the player must type them (supersedes NeedInput)
    bool NeedInput = false, bool IsQuiz = false,
    // IsProsign is passed to MorseManager to reduce letter gap
    // AutoRepeat is on by default and continually repeats the phrase audio with the PhraseGap, if off, button appears to have player trigger audio again, it will play once by default
    bool IsProsign = false, bool AutoRepeat = true);
