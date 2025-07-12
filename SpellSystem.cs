using Godot;
using System.Collections.Generic;

public partial class SpellSystem : Node
{
    // Dictionary to hold all projectile scenes
    private Dictionary<string, PackedScene> projectileScenes = new Dictionary<string, PackedScene>();
    
    // Spell dictionary - now just references projectile types
    private Dictionary<string, SpellData> spellbook = new Dictionary<string, SpellData>
    {
        {"XA", new SpellData("Ice Lance 1", 1, "ice_shard")},
        {"AX", new SpellData("Ice Lance 1", 1, "ice_shard")},
        {"XAXA", new SpellData("Ice Lance 2", 3, "ice_shard")},
        {"AXAX", new SpellData("Ice Lance 2", 3, "ice_shard")},
        
        // Future spells will just reference projectile types:
        // {"BB", new SpellData("Fireball", 1, "fireball")},
        // {"YY", new SpellData("Earth Boulder", 1, "earth_boulder")},
        // {"AA", new SpellData("Lightning Bolt", 1, "lightning")},
    };
    
    public override void _Ready()
    {
        GD.Print("SpellSystem ready!");
        LoadProjectileScenes();
    }
    
    private void LoadProjectileScenes()
    {
        // Load projectile scenes
        projectileScenes["ice_shard"] = GD.Load<PackedScene>("res://IceLance.tscn");
        // projectileScenes["fireball"] = GD.Load<PackedScene>("res://FireProjectile.tscn");
        // projectileScenes["lightning"] = GD.Load<PackedScene>("res://LightningProjectile.tscn");
        
        GD.Print($"Loaded {projectileScenes.Count} projectile types");
    }
    
    // New method: Try to prepare a spell (first trigger press)
    public SpellData TryPrepareSpell(List<string> elementBuffer)
    {
        string bufferString = string.Join("", elementBuffer);
        GD.Print($"SpellSystem: Attempting to prepare spell with buffer: {bufferString}");
        
        if (spellbook.ContainsKey(bufferString))
        {
            SpellData spell = spellbook[bufferString];
            GD.Print($"SpellSystem: Spell '{spell.Name}' prepared and ready to cast!");
            return spell;
        }
        else
        {
            GD.Print($"SpellSystem: No spell found for sequence: {bufferString}");
            return null;
        }
    }
    
    // New method: Cast a prepared spell (second trigger press)
    public void CastPreparedSpell(SpellData spell, Vector2 casterPosition, Vector2 aimDirection)
    {
        GD.Print($"🧙 Casting prepared spell: {spell.Name} in direction {aimDirection}!");
        
        // Create projectiles based on spell data
        for (int i = 0; i < spell.ProjectileCount; i++)
        {
            CreateProjectile(spell, casterPosition, aimDirection, i);
        }
    }
    
    // Legacy method for backward compatibility (if needed elsewhere)
    public bool TrycastSpell(List<string> elementBuffer, Vector2 casterPosition, Vector2 aimDirection)
    {
        var spell = TryPrepareSpell(elementBuffer);
        if (spell != null)
        {
            CastPreparedSpell(spell, casterPosition, aimDirection);
            return true;
        }
        return false;
    }
    
    private void CreateProjectile(SpellData spell, Vector2 startPos, Vector2 direction, int index)
    {
        if (!projectileScenes.ContainsKey(spell.ProjectileType))
        {
            GD.PrintErr($"Projectile type '{spell.ProjectileType}' not found!");
            return;
        }
        
        var projectileScene = projectileScenes[spell.ProjectileType];
        if (projectileScene == null)
        {
            GD.PrintErr($"Projectile scene for '{spell.ProjectileType}' is null!");
            return;
        }
        
        // Calculate spawn position in front of player
        Vector2 spawnOffset = direction * 30; // 30 pixels in front
        Vector2 projectilePos = startPos + spawnOffset;
        
        // Calculate spread for multiple projectiles
        Vector2 projectileDirection = direction;
        if (spell.ProjectileCount > 1)
        {
            float spreadAngle = (index - (spell.ProjectileCount - 1) / 2.0f) * 0.3f;
            projectileDirection = direction.Rotated(spreadAngle);
        }
        
        // Create the projectile and pass the stats from central registry
        var projectile = projectileScene.Instantiate<IceLance>();
        GetParent().AddChild(projectile);
        projectile.Setup(projectilePos, projectileDirection, spell.ProjectileStats);
        
        GD.Print($"❄️ Created {spell.ProjectileType} projectile at {projectilePos}");
    }
    
    public List<string> GetAvailableSpells()
    {
        var spells = new List<string>();
        foreach (var kvp in spellbook)
        {
            spells.Add($"{kvp.Key} = {kvp.Value.Name}");
        }
        return spells;
    }
}

// Data structure for spell information - now just references projectile type
public class SpellData
{
    public string Name { get; }
    public int ProjectileCount { get; }
    public string ProjectileType { get; }
    
    public SpellData(string name, int projectileCount, string projectileType)
    {
        Name = name;
        ProjectileCount = projectileCount;
        ProjectileType = projectileType;
    }
    
    // Helper property to get the actual projectile stats
    public ProjectileStats ProjectileStats => ProjectileStats.GetProjectileStats(ProjectileType);
}