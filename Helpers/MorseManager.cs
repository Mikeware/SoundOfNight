using System;
using System.Collections.Generic;

namespace LD60.Helpers;

public class MorseManager(AudioManager audioManager)
{
    private const int NumberGapSounds = 8;
    private const int NumberDotSounds = 3;
    private const int NumberDashSounds = 4;

    private const float SymbolGapTime = 0.15f;
    private const char Dot = '.';
    private const char Dash = '-';
    private const char SymbolGap = ' ';
    private const int LetterGaps = 3;
    private const int WordGaps = 7;

    private double _beat = 0;
    private int _delay = 0;

    // Represents stream of '.', '-', or ' ' (symbol gap) to transmit audio of morse code
    private readonly Queue<char> _encodedQueue = new(64);

    public event EventHandler? QueueCompleted;

    public void Queue(string phrase, bool prosign = false)
    {
        // If we're already processing anything, put a word gap next first
        if (_encodedQueue.Count > 0)
        {
            AddWordGap();
        }

        // Encode the sequence
        var uPhrase = phrase.ToUpper();
        for (var i = 0; i < uPhrase.Length; i++)
        {
            var @char = uPhrase[i];

            // Space in our phrase is a gap between words.
            if (@char == ' ')
            {
                AddWordGap();
                continue;
            }

            // Encode Letter
            var map = MorseMap[@char];
            for (var j = 0; j < map.Length; j++)
            {
                _encodedQueue.Enqueue(map[j]);

                // Only put symbol gaps between the inner symbols, as we account for gap for letter after
                if (j != map.Length - 1)
                {
                    _encodedQueue.Enqueue(SymbolGap);
                }
            }

            // Only put a letter gap if we have more stuff
            if (i != uPhrase.Length - 1)
            {
                if (prosign)
                {
                    // And it's not being encoded as a prosign: https://en.wikipedia.org/wiki/Prosigns_for_Morse_code
                    _encodedQueue.Enqueue(SymbolGap);
                }
                else
                {
                    AddLetterGap();
                }
            }
        }
    }

    public void Process(double delta)
    {
        _beat += delta;
        if (_beat > SymbolGapTime)
        {
            _beat -= SymbolGapTime;
            if (_delay > 0)
            {
                _delay--;
            }

            var playedSymbol = false;
            if (_delay == 0 &&
                _encodedQueue.TryDequeue(out char symbol))
            {
                if (symbol == Dot)
                {
                    playedSymbol = true;
                    audioManager.Play($"res://sounds/dot{Random.Shared.Next(NumberDotSounds)}.wav", true, 0.99f + Random.Shared.NextSingle() / 50f, 0.95f + Random.Shared.NextSingle() / 10f);
                }
                else if (symbol == Dash)
                {
                    playedSymbol = true;
                    audioManager.Play($"res://sounds/dash{Random.Shared.Next(NumberDashSounds)}.wav", true, 0.99f + Random.Shared.NextSingle() / 50f, 0.95f + Random.Shared.NextSingle() / 10f);
                    _delay = 3;
                }

                if (_encodedQueue.Count == 0)
                {
                    QueueCompleted?.Invoke(this, EventArgs.Empty);
                }
            }

            if (!playedSymbol)
            {
                // Play random background gap sounds otherwise
                audioManager.Play($"res://sounds/gap{Random.Shared.Next(NumberGapSounds)}.wav", true, 0.9f + Random.Shared.NextSingle() / 5f, 0.5f + Random.Shared.NextSingle() / 10f);
            }
        }
    }

    /// <summary>
    /// Adds <see cref="LetterGaps"/> SymbolGap signals to the queue to represent Letter spacing.
    /// </summary>
    private void AddLetterGap()
    {
        for (int i = 0; i < LetterGaps; i++)
        {
            _encodedQueue.Enqueue(SymbolGap);
        }
    }

    /// <summary>
    /// Adds <see cref="WordGaps"/> SymbolGap signals to the queue to represent Word spacing.
    /// </summary>
    private void AddWordGap()
    {
        for (int i = 0; i < WordGaps; i++)
        {
            _encodedQueue.Enqueue(SymbolGap);
        }
    }

    private readonly Dictionary<char, string> MorseMap = new()
    {
        ['A'] = ".-",
        ['B'] = "-...",
        ['C'] = "-.-.",
        ['D'] = "-..",
        ['E'] = ".",
        ['F'] = "..-.",
        ['G'] = "--.",
        ['H'] = "....",
        ['I'] = "..",
        ['J'] = ".---",
        ['K'] = "-.-",
        ['L'] = ".-..",
        ['M'] = "--",
        ['N'] = "-.",
        ['O'] = "---",
        ['P'] = ".--.",
        ['Q'] = "--.-",
        ['R'] = ".-.",
        ['S'] = "...",
        ['T'] = "-",
        ['U'] = "..-",
        ['V'] = "...-",
        ['W'] = ".--",
        ['X'] = "-..-",
        ['Y'] = "-.--",
        ['Z'] = "--..",
    };
}
