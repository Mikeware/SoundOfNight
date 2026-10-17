using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LD60.Helpers;

// Adapted from KenneyNL/Starter-Kit-Match-3/blob/main/scripts/audio.gd adapted from KidsCanCode

public partial class AudioManager : Node
{
    private const int MaxChannels = 4;
    private const string MainBus = "master";

    private readonly Stack<AudioStreamPlayer> _availableChannels = new(MaxChannels);
    private readonly Queue<QueuedAudioParameters> _queue = new(MaxChannels);
    private readonly Dictionary<NodePath, AudioStreamPlayer> _channelsInUse = new(MaxChannels);

    public void Play(NodePath soundPath, bool allowOverlap = false, float pitch = 1.0f, float volume = 1.0f)
    {
        // Always play sound or
        // check if not being played and not in queued
        // If it is we just won't queue it to be played
        if (allowOverlap ||
            (!_channelsInUse.ContainsKey(soundPath) &&
             !_queue.Select(p => p.Path).Contains(soundPath)))
        {
            _queue.Enqueue(new QueuedAudioParameters(soundPath, pitch, volume));
        }
    }

    public override void _Ready()
    {
        for (int i = 0; i <= MaxChannels; i++)
        {
            var player = new AudioStreamPlayer()
            {
                VolumeDb = -10,
                Bus = MainBus,
            };
            // Finished is an action, so capture the player here, no Bind C# support
            player.Finished += () => PlayerOnFinished(player);
            AddChild(player);
            _availableChannels.Push(player);
        }
    }

    private void PlayerOnFinished(AudioStreamPlayer player)
    {
        // Return used channel to available pool
        foreach (var channel in _channelsInUse)
        {
            if (channel.Value == player)
            {
                _channelsInUse.Remove(channel.Key);
                break;
            }
        }

        _availableChannels.Push(player);
    }

    public override void _Process(double delta)
    {
        // Check if we have stuff to play and a player for it!
        if (_queue.Count > 0 &&
            _availableChannels.TryPop(out var player))
        {
            var data = _queue.Dequeue();

            // Used to track sound overlap
            _channelsInUse[data.Path] = player;

            // Play sound
            player.Stream = GD.Load(data.Path) as AudioStream;
            player.PitchScale = data.Pitch;
            player.VolumeDb = data.Volume;
            player.Play();
        }
    }

    private record QueuedAudioParameters(NodePath Path, float Pitch, float Volume);
}
