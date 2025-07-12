using Godot;
using System.Collections.Generic;

// Data class for projectile physics properties
public class ProjectileStats
{
    public float Speed { get; set; } = 400.0f;
    public float InitialSpeedMultiplier { get; set; } = 1.5f;
    public float SpeedDecayRate { get; set; } = 0.8f;
    public float GravityMultiplier { get; set; } = 1.0f;
    public float AirResistance { get; set; } = 0.0f;
    public float LifeTime { get; set; } = 3.0f;
    public bool AffectedByGravity { get; set; } = true;
    public Color ProjectileColor { get; set; } = Colors.Cyan;  // Trajectory color for this projectile type
    
    public ProjectileStats(float speed = 400.0f, float initialSpeedMult = 1.5f, float decayRate = 0.8f, 
                          float gravityMult = 1.0f, bool gravity = true, Color? color = null)
    {
        Speed = speed;
        InitialSpeedMultiplier = initialSpeedMult;
        SpeedDecayRate = decayRate;
        GravityMultiplier = gravityMult;
        AffectedByGravity = gravity;
        ProjectileColor = color ?? Colors.Cyan;  // Default to cyan if no color specified
    }
    
    // Static registry - all projectile types in one place
    public static Dictionary<string, ProjectileStats> ProjectileTypes = new Dictionary<string, ProjectileStats>
    {
        {"ice_shard", new ProjectileStats(
            speed: 400.0f,
            initialSpeedMult: 1.5f,
            decayRate: 0.8f,
            gravityMult: 1.0f,
            gravity: true,
            color: Colors.Cyan
        )},
        
        // Future projectile types:
        // {"fireball", new ProjectileStats(300f, 1.2f, 0.5f, 0.8f, true)},
        // {"lightning", new ProjectileStats(800f, 1.0f, 0.0f, 0.0f, false)},
    };
    
    // Helper method to get projectile stats safely
    public static ProjectileStats GetProjectileStats(string projectileType)
    {
        if (ProjectileTypes.ContainsKey(projectileType))
        {
            return ProjectileTypes[projectileType];
        }
        else
        {
            GD.PrintErr($"Unknown projectile type: {projectileType}. Using default ice_shard stats.");
            return ProjectileTypes["ice_shard"];
        }
    }
}