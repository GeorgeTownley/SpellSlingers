using Godot;

public partial class AimIndicator : Node2D
{
    [Export]
    public int TrajectoryPoints = 50;
    [Export]
    public float TimeStep = 0.05f;  // Smaller steps for more accuracy
    [Export]
    public float LineWidth = 1.0f;
    [Export]
    public Color TrajectoryColor = Colors.Cyan;
    [Export]
    public float FadeSpeed = 5.0f;
    [Export]
    public float MaxTrajectoryDistance = 400.0f; // How far to show trajectory before fully faded
    [Export]
    public float FadeStartDistance = 200.0f;     // Distance where fade begins
    
    private bool isVisible = false;
    private float currentAlpha = 0.0f;
    private Vector2 aimDirection = Vector2.Right;
    private ProjectileStats projectileStats;
    private float gravity;
    private Vector2[][] trajectoryPoints; // Array of trajectory arrays for multiple projectiles
    private int projectileCount = 1; // How many projectiles this spell fires
    
    public override void _Ready()
    {
        ZIndex = 100;
        Modulate = new Color(1, 1, 1, 0);
        
        gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        
        // Initialize for single projectile (will be resized when needed)
        InitializeTrajectoryArrays(1);
        
        // Default stats (will be overridden when spell is prepared)
        projectileStats = ProjectileStats.GetProjectileStats("ice_shard");
    }
    
    private void InitializeTrajectoryArrays(int count)
    {
        projectileCount = count;
        trajectoryPoints = new Vector2[count][];
        for (int i = 0; i < count; i++)
        {
            trajectoryPoints[i] = new Vector2[TrajectoryPoints];
        }
    }
    
    public override void _Process(double delta)
    {
        float targetAlpha = isVisible ? 1.0f : 0.0f;
        currentAlpha = Mathf.MoveToward(currentAlpha, targetAlpha, FadeSpeed * (float)delta);
        
        var color = Modulate;
        color.A = currentAlpha;
        Modulate = color;
        
        if (currentAlpha > 0.01f)
        {
            CalculateTrajectory();
            QueueRedraw();
        }
    }
    
    private void CalculateTrajectory()
    {
        // Calculate trajectory for each projectile
        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            // MATCH the spawn offset from SpellSystem.CreateProjectile()
            Vector2 spawnOffset = aimDirection * 30; // 30 pixels in front (same as SpellSystem)
            Vector2 startPos = spawnOffset;
            
            // Calculate spread for multiple projectiles (SAME AS SpellSystem.CreateProjectile())
            Vector2 projectileDirection = aimDirection;
            if (projectileCount > 1)
            {
                float spreadAngle = (projectileIndex - (projectileCount - 1) / 2.0f) * 0.3f;
                projectileDirection = aimDirection.Rotated(spreadAngle);
            }
            
            Vector2 initialDirection = projectileDirection.Normalized();
            
            // EXACTLY match IceLance.Setup()
            Vector2 velocity = initialDirection * (projectileStats.Speed * projectileStats.InitialSpeedMultiplier);
            Vector2 position = startPos;
            float timeAlive = 0.0f;
            
            // Match IceLance gravity setup
            float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle() * projectileStats.GravityMultiplier;
            
            for (int i = 0; i < TrajectoryPoints; i++)
            {
                trajectoryPoints[projectileIndex][i] = position;
                
                // Stop if projectile would be "dead"
                if (timeAlive >= projectileStats.LifeTime)
                {
                    for (int j = i; j < TrajectoryPoints; j++)
                    {
                        trajectoryPoints[projectileIndex][j] = position;
                    }
                    break;
                }
                
                // EXACTLY match IceLance._PhysicsProcess() logic:
                
                // 1. Apply speed decay over time - exponential decay from 150% to 100% of target speed
                float decayFactor = Mathf.Exp(-projectileStats.SpeedDecayRate * timeAlive);
                float speedMultiplier = Mathf.Lerp(1.0f, projectileStats.InitialSpeedMultiplier, decayFactor);
                float currentHorizontalSpeed = projectileStats.Speed * speedMultiplier;
                
                // 2. Calculate horizontal velocity (maintaining original direction)
                Vector2 horizontalVelocity = initialDirection * currentHorizontalSpeed;
                
                // 3. Apply gravity to vertical component (SAME AS ICELANCE)
                if (projectileStats.AffectedByGravity)
                {
                    velocity.Y += gravity * TimeStep;
                }
                
                // 4. Combine horizontal movement with gravity-affected vertical movement
                velocity.X = horizontalVelocity.X;
                // Don't override Y - it's already affected by gravity above
                
                // 5. Move the projectile (SAME AS ICELANCE)
                position += velocity * TimeStep;
                
                // 6. Update time
                timeAlive += TimeStep;
            }
        }
    }
    
    public override void _Draw()
    {
        if (currentAlpha <= 0.01f || trajectoryPoints == null) return;
        
        // Use color from projectile stats
        Color projectileColor = projectileStats.ProjectileColor;
        projectileColor.A = currentAlpha;
        
        // Draw trajectory line for each projectile (all same color now)
        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            if (trajectoryPoints[projectileIndex].Length >= 2)
            {
                // Draw trajectory with distance-based fading
                DrawFadedTrajectory(trajectoryPoints[projectileIndex], projectileColor);
            }
        }
        
        // Draw starting position indicator
        DrawCircle(Vector2.Zero, 3.0f, projectileColor);
    }
    
    private void DrawFadedTrajectory(Vector2[] points, Color baseColor)
    {
        if (points.Length < 2) return;
        
        // Calculate distances and create faded segments
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 currentPoint = points[i];
            Vector2 nextPoint = points[i + 1];
            
            // Calculate distance from start for fade effect
            float distanceFromStart = currentPoint.Length();
            
            // Calculate fade based on distance
            float fadeAlpha = 1.0f;
            if (distanceFromStart > FadeStartDistance)
            {
                float fadeProgress = (distanceFromStart - FadeStartDistance) / (MaxTrajectoryDistance - FadeStartDistance);
                fadeAlpha = Mathf.Clamp(1.0f - fadeProgress, 0.0f, 1.0f);
            }
            
            // Stop drawing if completely faded or too far
            if (fadeAlpha <= 0.01f || distanceFromStart > MaxTrajectoryDistance)
                break;
            
            // Apply fade to color
            Color segmentColor = baseColor;
            segmentColor.A *= fadeAlpha;
            
            // Draw the line segment
            DrawLine(currentPoint, nextPoint, segmentColor, LineWidth);
        }
    }
    
    // New method that accepts projectile stats and count
    public void ShowAiming(Vector2 direction, ProjectileStats stats, int count = 1)
    {
        aimDirection = direction.Normalized();
        projectileStats = stats;
        
        // Resize trajectory arrays if needed
        if (count != projectileCount)
        {
            InitializeTrajectoryArrays(count);
        }
        
        isVisible = true;
    }
    
    // Backward compatibility methods
    public void ShowAiming(Vector2 direction)
    {
        ShowAiming(direction, ProjectileStats.GetProjectileStats("ice_shard"), 1);
    }
    
    public void ShowAiming(Vector2 direction, float speed)
    {
        ShowAiming(direction, new ProjectileStats(speed), 1);
    }
    
    public void ShowAiming(Vector2 direction, ProjectileStats stats)
    {
        ShowAiming(direction, stats, 1);
    }
    
    public void HideAiming()
    {
        isVisible = false;
    }
    
    public void UpdateDirection(Vector2 direction, ProjectileStats stats, int count = 1)
    {
        aimDirection = direction.Normalized();
        projectileStats = stats;
        
        // Resize trajectory arrays if needed
        if (count != projectileCount)
        {
            InitializeTrajectoryArrays(count);
        }
    }
    
    public void UpdateDirection(Vector2 direction)
    {
        aimDirection = direction.Normalized();
    }
    
    public void UpdateDirection(Vector2 direction, float speed)
    {
        UpdateDirection(direction, new ProjectileStats(speed), 1);
    }
    
    public void UpdateDirection(Vector2 direction, ProjectileStats stats)
    {
        UpdateDirection(direction, stats, 1);
    }
}