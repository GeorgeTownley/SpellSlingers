using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed = 400.0f;
    [Export]
    public float JumpVelocity = -650.0f;
    
    // Get gravity from project settings
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    
    public override void _Ready()
    {
        GD.Print("Player ready!");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        
        // Apply gravity
        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;
        
        // Handle jumping
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;
        
        // Handle horizontal movement
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "", "");
        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        }
        
        Velocity = velocity;
        MoveAndSlide();
    }
}