using Godot;

public partial class AimIndicator : Node2D
{
    [Export]
    public int TrajectoryPoints = 50;
    [Export]
    public float TimeStep = 0.05f;
    [Export]
    public float LineWidth = 1.0f;
    [Export]
    public Color TrajectoryColor = Colors.Cyan; // Fallback color
    [Export]
    public float FadeSpeed = 5.0f;
    [Export]
    public float MaxTrajectoryDistance = 400.0f;
    [Export]
    public float FadeStartDistance = 200.0f;
    
    private bool isVisible = false;
    private float currentAlpha = 0.0f;
    private Vector2 aimDirection = Vector2.Right;
    private ProjectileStats projectileStats;
    private int projectileCount = 1;
    private float speedMultiplier = 1.0f;
    private float spreadMultiplier = 1.0f;
    private float gravity;
    private Vector2[][] trajectoryPoints;
    
    public override void _Ready()
    {
        ZIndex = 100;
        Modulate = new Color(1, 1, 1, 0);
        
        gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        
        // Initialize for single projectile
        InitializeTrajectoryArrays(1);
        
        // Default stats
        projectileStats = ProjectileStats.GetProjectileStats("ice_shard");
        
        GD.Print("AimIndicator ready!");
    }
    
    private void InitializeTrajectoryArrays(int count)
    {
        projectileCount = count;
        trajectoryPoints = new Vector2[count][];
        for (int i = 0; i < count; i++)
        {
            trajectoryPoints[i] = new Vector2[TrajectoryPoints];
        }
        GD.Print($"AimIndicator: Initialized for {count} projectiles");
    }
    
    public override void _Process(double delta)
    {
        // Smooth fade in/out
        float targetAlpha = isVisible ? 1.0f : 0.0f;
        currentAlpha = Mathf.MoveToward(currentAlpha, targetAlpha, FadeSpeed * (float)delta);
        
        var color = Modulate;
        color.A = currentAlpha;
        Modulate = color;
        
        // Recalculate trajectory every frame when visible (for real-time updates)
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
            // Spawn position (30px in front of player, same as SpellSystem)
            Vector2 spawnOffset = aimDirection * 30;
            Vector2 startPos = spawnOffset;
            
            // Calculate spread direction for this projectile
            Vector2 projectileDirection = aimDirection;
            if (projectileCount > 1)
            {
                float baseSpreadAngle = (projectileIndex - (projectileCount - 1) / 2.0f) * 0.3f;
                float actualSpreadAngle = baseSpreadAngle * spreadMultiplier; // Apply spread reduction
                projectileDirection = aimDirection.Rotated(actualSpreadAngle);
            }
            
            Vector2 initialDirection = projectileDirection.Normalized();
            
            // Apply speed multiplier to base speed
            float enhancedSpeed = projectileStats.Speed * speedMultiplier;
            
            // Initial velocity (same as IceLance.Setup)
            Vector2 velocity = initialDirection * (enhancedSpeed * projectileStats.InitialSpeedMultiplier);
            Vector2 position = startPos;
            float timeAlive = 0.0f;
            
            // Gravity setup
            float effectiveGravity = projectileStats.AffectedByGravity ? 
                gravity * projectileStats.GravityMultiplier : 0.0f;
            
            // Simulate trajectory point by point
            for (int i = 0; i < TrajectoryPoints; i++)
            {
                trajectoryPoints[projectileIndex][i] = position;
                
                // Stop if projectile would be destroyed
                if (timeAlive >= projectileStats.LifeTime)
                {
                    // Fill remaining points with last position
                    for (int j = i; j < TrajectoryPoints; j++)
                    {
                        trajectoryPoints[projectileIndex][j] = position;
                    }
                    break;
                }
                
                // Apply speed decay (same as IceLance)
                float decayFactor = Mathf.Exp(-projectileStats.SpeedDecayRate * timeAlive);
                float speedDecayMultiplier = Mathf.Lerp(1.0f, projectileStats.InitialSpeedMultiplier, decayFactor);
                float currentHorizontalSpeed = enhancedSpeed * speedDecayMultiplier;
                
                // Calculate horizontal velocity
                Vector2 horizontalVelocity = initialDirection * currentHorizontalSpeed;
                
                // Apply gravity to Y component (accumulates over time, same as IceLance)
                velocity.Y += effectiveGravity * TimeStep;
                
                // Update velocity (X gets overwritten, Y accumulates - same as IceLance)
                velocity.X = horizontalVelocity.X;
                
                // Move projectile
                position += velocity * TimeStep;
                
                // Update time
                timeAlive += TimeStep;
            }
        }
    }
    
    public override void _Draw()
    {
        if (currentAlpha <= 0.01f || trajectoryPoints == null) return;
        
        // Use projectile's signature color
        Color projectileColor = projectileStats.ProjectileColor;
        projectileColor.A = currentAlpha;
        
        // Draw trajectory for each projectile
        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            if (trajectoryPoints[projectileIndex].Length >= 2)
            {
                DrawFadedTrajectory(trajectoryPoints[projectileIndex], projectileColor);
            }
        }
        
        // Draw starting position indicator
        DrawCircle(Vector2.Zero, 3.0f, projectileColor);
    }
    
    private void DrawFadedTrajectory(Vector2[] points, Color baseColor)
    {
        if (points.Length < 2) return;
        
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
    
    // Main method - accepts all parameters
    public void ShowAiming(Vector2 direction, ProjectileStats stats, int count = 1, float speedMult = 1.0f, float spreadMult = 1.0f)
    {
        aimDirection = direction.Normalized();
        projectileStats = stats;
        speedMultiplier = speedMult;
        spreadMultiplier = spreadMult;
        
        if (count != projectileCount)
        {
            InitializeTrajectoryArrays(count);
        }
        
        isVisible = true;
        
        // Debug log
        GD.Print($"AimIndicator: ShowAiming - Count: {count}, Speed: {speedMult:F2}x, Spread: {spreadMult:F2}x");
    }
    
    public void UpdateDirection(Vector2 direction, ProjectileStats stats, int count = 1, float speedMult = 1.0f, float spreadMult = 1.0f)
    {
        // Just update parameters - trajectory will recalculate automatically in _Process
        aimDirection = direction.Normalized();
        projectileStats = stats;
        speedMultiplier = speedMult;
        spreadMultiplier = spreadMult;
        
        if (count != projectileCount)
        {
            InitializeTrajectoryArrays(count);
        }
        
        // Debug log (less frequent)
        if (speedMult > 1.01f || spreadMult < 0.99f)
        {
            GD.Print($"AimIndicator: UpdateDirection - Speed: {speedMult:F2}x, Spread: {spreadMult:F2}x");
        }
    }
    
    public void HideAiming()
    {
        isVisible = false;
    }
    
    // Backward compatibility methods
    public void ShowAiming(Vector2 direction)
    {
        ShowAiming(direction, ProjectileStats.GetProjectileStats("ice_shard"), 1, 1.0f, 1.0f);
    }
    
    public void ShowAiming(Vector2 direction, float speed)
    {
        ShowAiming(direction, new ProjectileStats(speed), 1, 1.0f, 1.0f);
    }
    
    public void ShowAiming(Vector2 direction, ProjectileStats stats)
    {
        ShowAiming(direction, stats, 1, 1.0f, 1.0f);
    }
    
    public void ShowAiming(Vector2 direction, ProjectileStats stats, int count)
    {
        ShowAiming(direction, stats, count, 1.0f, 1.0f);
    }
    
    public void UpdateDirection(Vector2 direction)
    {
        aimDirection = direction.Normalized();
    }
    
    public void UpdateDirection(Vector2 direction, float speed)
    {
        UpdateDirection(direction, new ProjectileStats(speed), 1, 1.0f, 1.0f);
    }
    
    public void UpdateDirection(Vector2 direction, ProjectileStats stats)
    {
        UpdateDirection(direction, stats, 1, 1.0f, 1.0f);
    }
    
    public void UpdateDirection(Vector2 direction, ProjectileStats stats, int count)
    {
        UpdateDirection(direction, stats, count, 1.0f, 1.0f);
    }
}