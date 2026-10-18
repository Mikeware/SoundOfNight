using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using LD60.Helpers;
using LD60.scenes.secondary;

namespace LD60.scenes;

public partial class Tutorial : Control
{
    [Export]
    public Label? LetterLabel { get; set; }

    [Export]
    public Label? MorseLabel { get; set; }

    [Export]
    public Label? TypeLabel { get; set; }

    private AudioManager? _audioManager;
    private MorseManager? _morseManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _audioManager = GetNode<AudioManager>("/root/AudioManager");
        _morseManager = new(_audioManager);
        _morseManager.QueueCompleted += MorseManagerOnQueueCompleted;

        // Kick us off
        MorseManagerOnQueueCompleted(this, EventArgs.Empty);
        StrongReferenceMessenger.Default.Send<DisplayDialogueMessage>(new([
            "Hi, to play this game you need to learn Morse Code.",
            "We can teach you. If you know already, press Skip."
        ]));
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
}
