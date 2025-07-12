using Godot;

public partial class IceLance : Area2D
{
    [Export]
    public float Speed = 400.0f;  // This becomes the "final" speed after decay
    [Export]
    public float LifeTime = 3.0f;
    [Export]
    public float SpeedDecayRate = 0.8f;  // How fast speed decays (higher = faster decay)
    [Export]
    public float GravityMultiplier = 1.0f;  // Multiplier for gravity effect
   
    private Vector2 velocity;
    private float timeAlive = 0.0f;
    private float initialSpeed;
    private float currentHorizontalSpeed;
    private Vector2 initialDirection;
    private float gravity;
   
    public override void _Ready()
    {
        GD.Print("IceLance ready!");
        
        // Get gravity from project settings
        gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() * GravityMultiplier;
    }
   
    public override void _PhysicsProcess(double delta)
    {
        // Update time alive
        timeAlive += (float)delta;
        
        // Apply speed decay over time - exponential decay from 150% to 100% of target speed
        float decayFactor = Mathf.Exp(-SpeedDecayRate * timeAlive);
        float speedMultiplier = Mathf.Lerp(1.0f, 1.5f, decayFactor);  // Goes from 1.5x to 1.0x
        currentHorizontalSpeed = Speed * speedMultiplier;
        
        // Calculate horizontal velocity (maintaining original direction)
        Vector2 horizontalVelocity = initialDirection * currentHorizontalSpeed;
        
        // Apply gravity to vertical component
        velocity.Y += gravity * (float)delta;
        
        // Combine horizontal movement with gravity-affected vertical movement
        velocity.X = horizontalVelocity.X;
        // Don't override Y if it's already affected by gravity
        
        // Move the projectile
        GlobalPosition += velocity * (float)delta;
        
        // Rotate to match velocity direction (so it points where it's actually going)
        if (velocity.Length() > 0)
        {
            Rotation = velocity.Angle();
        }
       
        // Destroy after time limit
        if (timeAlive >= LifeTime)
        {
            QueueFree();
        }
        
        // Optional: Destroy if it goes too far off screen
        var viewport = GetViewport();
        if (viewport != null)
        {
            var viewportRect = viewport.GetVisibleRect();
            var screenPos = GlobalPosition;
            
            // Add some buffer around screen edges
            float buffer = 200.0f;
            if (screenPos.X < -buffer || screenPos.X > viewportRect.Size.X + buffer ||
                screenPos.Y < -buffer || screenPos.Y > viewportRect.Size.Y + buffer)
            {
                QueueFree();
            }
        }
    }
   
    public void Setup(Vector2 startPosition, Vector2 direction, ProjectileStats stats)
    {
        GlobalPosition = startPosition;
        
        // Store initial values from stats
        initialDirection = direction.Normalized();
        Speed = stats.Speed;  // This is our "target" speed
        initialSpeed = stats.Speed * stats.InitialSpeedMultiplier;
        currentHorizontalSpeed = initialSpeed;
        SpeedDecayRate = stats.SpeedDecayRate;
        GravityMultiplier = stats.GravityMultiplier;
        LifeTime = stats.LifeTime;
        
        // Update gravity with new multiplier
        gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() * GravityMultiplier;
        
        // Set initial velocity (horizontal only, gravity will handle vertical)
        velocity = initialDirection * initialSpeed;
        
        // Start rotation pointing in initial direction
        Rotation = direction.Angle();
       
        GD.Print($"IceLance setup: pos={startPosition}, dir={direction}, initial_speed={initialSpeed}, target_speed={Speed}");
    }
    
    // Backward compatibility method
    public void Setup(Vector2 startPosition, Vector2 direction, float projectileSpeed)
    {
        Setup(startPosition, direction, new ProjectileStats(projectileSpeed));
    }
}