using System;
using Godot;
using LD60.Helpers;

namespace LD60;

public partial class Setup : Control
{
    [Export]
    public Button? ProceedButton { get; set; }

    private MorseManager? _morseManager;

    private int _delay = 2;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var audioManager = GetNode<AudioManager>("/root/AudioManager");
        _morseManager = new(audioManager);
        _morseManager.QueueCompleted += MorseManagerOnQueueCompleted;

        // Kick us off
        MorseManagerOnQueueCompleted(this, EventArgs.Empty);
    }

    private void MorseManagerOnQueueCompleted(object? sender, EventArgs e)
    {
        if (_delay > 0)
        {
            _delay--;
            if (_delay == 0)
            {
                // Show proceed button after first SOS
                Tween tween = CreateTween();
                tween.TweenProperty(ProceedButton, "modulate:a", 1.0, 2.0);
                tween.Play();
            }
        }

        // Put a small gap before we start again
        _morseManager?.Queue(" SOS");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        _morseManager?.Process(delta);
    }
}
