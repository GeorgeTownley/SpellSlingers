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
            window.Position = new Vector2I(100, 100);
        }
        
        // Spawn player
        var playerScene = GD.Load<PackedScene>("res://scenes/characters/Player.tscn");
        var player = playerScene.Instantiate();
        AddChild(player);
        
        // Optional: set player position
        if (player is Node2D playerNode2D)
        {
            playerNode2D.Position = new Vector2(400, 300); // or wherever you want
        }
    }
    
    public override void _Process(double delta)
    {
    }
}