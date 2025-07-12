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
    private Node caster; // Reference to whoever cast this projectile
    private float graceTime = 0.0f;
    private const float GracePeriod = 0.15f; // 150ms grace period
    private bool isTelegraph = false; // Is this a telegraph projectile (visual only)?
   
    public override void _Ready()
    {
        GD.Print("IceLance ready!");
        
        // Get gravity from project settings
        gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() * GravityMultiplier;
        
        // Connect collision signals
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
    }
   
    public override void _PhysicsProcess(double delta)
    {
        // Telegraph projectiles don't move or have physics
        if (isTelegraph)
        {
            // Just pulse the visual to show it's a telegraph
            float pulseAlpha = 0.3f + 0.3f * Mathf.Sin(timeAlive * 8.0f); // Pulse between 0.3 and 0.6 alpha
            Modulate = new Color(1, 1, 1, pulseAlpha);
            timeAlive += (float)delta;
            return;
        }
        
        // Normal projectile physics
        // Update time alive and grace period
        timeAlive += (float)delta;
        graceTime += (float)delta;
        
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
        
        // Store reference to caster for grace period
        caster = GetTree().GetFirstNodeInGroup("player"); // Assuming player is in "player" group
        graceTime = 0.0f; // Reset grace timer
        isTelegraph = false; // This is a real projectile
       
        GD.Print($"IceLance setup: pos={startPosition}, dir={direction}, initial_speed={initialSpeed}, target_speed={Speed}");
    }
    
    public void SetupTelegraph(Vector2 startPosition, Vector2 direction, ProjectileStats stats)
    {
        GlobalPosition = startPosition;
        
        // Store values but don't set up physics
        initialDirection = direction.Normalized();
        Speed = stats.Speed;
        
        // Set rotation to show direction
        Rotation = direction.Angle();
        
        // Mark as telegraph
        isTelegraph = true;
        timeAlive = 0.0f;
        
        // Disable collision for telegraph
        SetCollisionLayerValue(1, false);
        SetCollisionMaskValue(1, false);
        
        // Set initial visual state (semi-transparent)
        Modulate = new Color(1, 1, 1, 0.5f);
        
        GD.Print($"IceLance telegraph setup: pos={startPosition}, dir={direction}");
    }
    
    private void OnBodyEntered(Node body)
    {
        GD.Print($"IceLance hit body: {body.Name} (Type: {body.GetType().Name})");
        
        // Ignore caster during grace period
        if (graceTime < GracePeriod && body == caster)
        {
            GD.Print($"Ignoring caster {body.Name} during grace period ({graceTime:F2}s < {GracePeriod}s)");
            return;
        }
        
        // Check if it's a player or platform by type name or node name
        bool isPlayer = body.GetType().Name == "Player" || body.Name.ToString().Contains("Player");
        bool isPlatform = body is StaticBody2D || body.Name.ToString().Contains("Platform") || body.Name.ToString().Contains("Wall");
        bool isPhysicsBody = body is RigidBody2D;
        bool isCharacterBody = body is CharacterBody2D;
        
        if (isPlayer || isPlatform || isPhysicsBody || isCharacterBody)
        {
            // Deal damage if it's a player
            if ((isPlayer || isCharacterBody) && body.HasMethod("TakeDamage"))
            {
                // Calculate damage based on current speed (faster = more damage)
                float maxSpeed = Speed * 1.5f; // Use the initial speed multiplier (1.5f is hardcoded in original)
                float currentSpeedRatio = velocity.Length() / maxSpeed;
                float damage = 10.0f * currentSpeedRatio; // Base 10 damage, scaled by speed
                
                body.Call("TakeDamage", damage);
                GD.Print($"Dealt {damage:F1} damage to {body.Name}");
            }
            
            // Create impact effect and destroy projectile
            CreateImpactEffect();
            QueueFree();
        }
    }
    
    private void OnAreaEntered(Area2D area)
    {
        GD.Print($"IceLance hit area: {area.Name}");
        
        // Ignore other projectiles during grace period (prevents multi-projectile spells from hitting each other)
        if (graceTime < GracePeriod && area.GetType().Name == "IceLance")
        {
            GD.Print($"Ignoring sibling projectile {area.Name} during grace period ({graceTime:F2}s < {GracePeriod}s)");
            return;
        }
        
        // Hit another projectile or area - destroy both
        if (area != this) // Don't collide with self
        {
            CreateImpactEffect();
            QueueFree();
        }
    }
    
    private void CreateImpactEffect()
    {
        // TODO: Add particle effects, sound effects, etc.
        GD.Print($"💥 IceLance impact at {GlobalPosition}");
        
        // Future: Spawn ice shatter particles, play impact sound
    }
    
    // Backward compatibility method
    public void Setup(Vector2 startPosition, Vector2 direction, float projectileSpeed)
    {
        Setup(startPosition, direction, new ProjectileStats(projectileSpeed));
    }
}