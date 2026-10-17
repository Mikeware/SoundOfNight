using Godot;
using System;
using LD60.Helpers;

public partial class Setup : Control
{
    private MorseManager _morseManager;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        var audioManager = GetNode<AudioManager>("/root/AudioManager");
        _morseManager = new(audioManager);
        _morseManager.QueueCompleted += MorseManagerOnQueueCompleted;

        // Kick us off
        MorseManagerOnQueueCompleted(this, EventArgs.Empty);
    }

    private void MorseManagerOnQueueCompleted(object sender, EventArgs e)
    {
        // Put a small gap before we start again
        _morseManager.Queue(" SOS");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        _morseManager.Process(delta);
    }
}
