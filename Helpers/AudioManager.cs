using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LD60.Helpers;

// Adapted from KenneyNL/Starter-Kit-Match-3/blob/main/scripts/audio.gd adapted from KidsCanCode

public partial class AudioManager : Node
{
    private const int MaxChannels = 8;
    private const string MainBus = "master";

    private readonly Stack<AudioStreamPlayer> _availableChannels = new(MaxChannels);
    private readonly Queue<QueuedAudioParameters> _queue = new(MaxChannels);
    private readonly Dictionary<NodePath, HashSet<AudioStreamPlayer>> _channelsInUse = new(MaxChannels);

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
        for (int i = 0; i < MaxChannels; i++)
        {
            var channel = new AudioStreamPlayer()
            {
                VolumeDb = -10,
                Bus = MainBus,
            };
            // Finished is an action, so capture the player here, no Bind C# support
            channel.Finished += () => PlayerOnFinished(channel);
            AddChild(channel);
            _availableChannels.Push(channel);
        }
    }

    private void PlayerOnFinished(AudioStreamPlayer completedChannel)
    {
        // Return used channel to available pool
        foreach (var audioChannelSet in _channelsInUse)
        {
            if (audioChannelSet.Value.Contains(completedChannel))
            {
                audioChannelSet.Value.Remove(completedChannel);
                // TODO: This could be wasteful if the same audio is just going to be played over and over
                // Could use ObjectPool for the HashSet objects...
                if (audioChannelSet.Value.Count == 0)
                {
                    _channelsInUse.Remove(audioChannelSet.Key);
                }
                break;
            }
        }

        _availableChannels.Push(completedChannel);
    }

    public override void _Process(double delta)
    {
        // Check if we have stuff to play and a player for it!
        if (_queue.Count > 0 &&
            _availableChannels.TryPop(out var channel))
        {
            var data = _queue.Dequeue();

            // Used to track sound overlap
            if (!_channelsInUse.ContainsKey(data.Path))
            {
                // Use HashSet in case we allow overlap and start playing the same sound on another channel.
                _channelsInUse[data.Path] = new();
            }
            _channelsInUse[data.Path].Add(channel);

            // Play sound
            channel.Stream = GD.Load(data.Path) as AudioStream;
            channel.PitchScale = data.Pitch;
            channel.VolumeDb = data.Volume;
            channel.Play();
        }
    }

    private record QueuedAudioParameters(NodePath Path, float Pitch, float Volume);
}
