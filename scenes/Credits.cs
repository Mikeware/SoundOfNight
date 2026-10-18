using System.Diagnostics;
using Godot;

namespace LD60.scenes;

public partial class Credits : Control
{
    [Export(PropertyHint.File, "*.tscn")]
    public string NextScenePath { get; set; } = "res://shell.tscn";


    [Export(PropertyHint.File, "*.txt")]
    public string CreditsPath { get; set; } = "res://credits.txt";

    [Export]
    public Label? CreditsLabel { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
    {
        // load credits
        CreditsLabel?.Position = CreditsLabel.Position with { Y = 360 };
        FileAccess file = FileAccess.Open(CreditsPath, FileAccess.ModeFlags.Read);
        CreditsLabel?.Text = file.GetAsText();
        file.Close();

        // Measure Credits Height
        await ToSignal(GetTree().CreateTimer(0.1), "timeout");

        // display credits
        Tween tween = CreateTween();
        tween.TweenProperty(CreditsLabel, "position:y", (double)-CreditsLabel!.Size.Y, 30.0);
        tween.Finished += () =>
        {
            Shell.Instance?.TransitionTo(NextScenePath);
        };

        Shell.Instance?.PreloadScene(NextScenePath);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
