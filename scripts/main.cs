using Godot;
using System;

public partial class main : Node2D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (OS.IsDebugBuild())
        {
            var window = GetWindow();
            window.Mode = Window.ModeEnum.Windowed;
            window.Size = new Vector2I(1280, 720);
            window.Position = new Vector2I(100, 100); // Optional: set position
        }
    }
    
    public override void _Process(double delta)
    {
    }
}