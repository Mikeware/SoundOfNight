using Godot;
using System;
using LD60.Helpers;

public partial class Setup : Control
{
    private AudioManager _audioManager;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _audioManager = GetNode<AudioManager>("/root/AudioManager");
        _audioManager.Play("res://sounds/dash1.wav");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
