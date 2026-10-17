using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

namespace LD60;

public partial class Shell : Control
{
    public static Shell? Instance { get; private set; }

    [Export]
    public PackedScene? InitialScene { get; set; }

    [Export]
    public ShaderMaterial? TransitionMaterial { get; set; }

    [Export]
    public double TransitionHalfDuration { get; set; } = 1.5;

    [Export]
    public Control? SceneHost { get; set; }

    private bool _triggeredTransition;
    private string? _preloadScene;

    public override void _Ready()
    {
        Instance = this;

        _ = WaitForLoadAsync();
    }

    private async Task WaitForLoadAsync()
    {
        // wait for a processed frame
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (InitialScene is null) throw new ArgumentNullException(nameof(InitialScene));
        PreloadScene(InitialScene.ResourcePath);
        _triggeredTransition = true;
    }

    public override void _Process(double delta)
    {
        if (_triggeredTransition &&
            _preloadScene != null &&
            ResourceLoader.LoadThreadedGetStatus(_preloadScene) == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            Node? scene = (ResourceLoader.LoadThreadedGet(_preloadScene) as PackedScene)?.Instantiate();
            if (scene is null) throw new InvalidOperationException($"Could not load scene {_preloadScene}");
            _preloadScene = null;
            _triggeredTransition = false;

            // TODO: Fire Loading event, if needed

            if (SceneHost?.GetChildCount() > 0)
            {
                Node? current = SceneHost?.GetChild(0);
                current?.QueueFree();
            }

            _ = LoadSceneInAsync(scene);
        }
    }

    public void PreloadScene(string nextScene)
    {
        _preloadScene = nextScene;
        ResourceLoader.LoadThreadedRequest(nextScene);
    }

    public void TransitionTo(string nextScene)
    {
        TransitionMaterial?.SetShaderParameter("inverted", false);
        var tween = CreateTween();
        tween.TweenMethod(Callable.From((float v) => TransitionMaterial?.SetShaderParameter("progress", v)), 0.0, 1.0, TransitionHalfDuration)
            .SetEase(Tween.EaseType.In);
        tween.Finished += () =>
        {
            if (nextScene == _preloadScene)
            {
                _triggeredTransition = true;
                return;
            }

            PackedScene scenePack = GD.Load<PackedScene>(nextScene);
            Node scene = scenePack.Instantiate();
            // TODO: Fire Loading event if need to know

            Node? current = SceneHost?.GetChild(0);
            current?.QueueFree();

            _ = LoadSceneInAsync(scene);
        };
    }

    private async Task LoadSceneInAsync(Node scene)
    {
        SceneHost?.AddChild(scene);

        TransitionMaterial?.SetShaderParameter("inverted", true);
        var tween = CreateTween();
        tween.TweenMethod(Callable.From((float v) => TransitionMaterial?.SetShaderParameter("progress", v)), 0.0, 1.0, TransitionHalfDuration)
            .SetEase(Tween.EaseType.In);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}
