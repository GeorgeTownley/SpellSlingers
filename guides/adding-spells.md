# Adding New Spells

Developer guide for extending the spell casting system.

## Overview

The spell system uses a two-file approach: define projectile physics once, then reference them in spell definitions. This guide covers adding new spells and projectile types.

## Quick Start

### Adding a Spell with Existing Projectile

```csharp
// In SpellSystem.cs spellbook dictionary:
{"XB", new SpellData("Ice Fire Lance", 1, "ice_shard")}
```

### Adding a New Projectile Type

1. **Define physics** in `ProjectileStats.cs`
2. **Add spell** in `SpellSystem.cs`
3. **Create projectile scene/script** (if completely new type)

## Step-by-Step Guide

### 1. Define Projectile Physics

**File:** `ProjectileStats.cs`

Add to the `ProjectileTypes` dictionary:

```csharp
public static Dictionary<string, ProjectileStats> ProjectileTypes = new()
{
    {"ice_shard", new ProjectileStats(400f, 1.5f, 0.8f, 1.0f, true)},
    {"fireball", new ProjectileStats(300f, 1.2f, 0.5f, 0.8f, true)},
    {"lightning", new ProjectileStats(800f, 1.0f, 0.0f, 0.0f, false)},
};
```

**Parameters:**

- `speed` - Base projectile speed
- `initialSpeedMult` - Starting speed multiplier (1.5 = 50% faster)
- `decayRate` - Speed decay rate (higher = faster decay)
- `gravityMult` - Gravity multiplier (0 = none, 1 = normal, 2 = heavy)
- `gravity` - Boolean for gravity effects

### 2. Add Spell Definition

**File:** `SpellSystem.cs`

Add to the `spellbook` dictionary:

```csharp
private Dictionary<string, SpellData> spellbook = new()
{
    {"XA", new SpellData("Ice Lance 1", 1, "ice_shard")},
    {"BB", new SpellData("Fireball", 1, "fireball")},
    {"AAA", new SpellData("Lightning Storm", 3, "lightning")},
};
```

**Parameters:**

- `"BB"` - Element combination (player input sequence)
- `"Fireball"` - Display name
- `1` - Number of projectiles
- `"fireball"` - Projectile type (must match ProjectileStats key)

### 3. Register Projectile Scene (New Types Only)

**File:** `SpellSystem.cs` in `LoadProjectileScenes()`

```csharp
private void LoadProjectileScenes()
{
    projectileScenes["ice_shard"] = GD.Load<PackedScene>("res://IceLance.tscn");
    projectileScenes["fireball"] = GD.Load<PackedScene>("res://Fireball.tscn");
}
```

### 4. Create Projectile Assets (New Types Only)

1. Create projectile scene (e.g., `Fireball.tscn`)
2. Create projectile script (copy `IceLance.cs` as template)
3. Ensure script accepts `ProjectileStats` in `Setup()` method

## Examples

### Multi-Projectile Spell

```csharp
{"XAXA", new SpellData("Ice Lance Barrage", 3, "ice_shard")}
```

### Fast, No-Gravity Projectile

```csharp
// ProjectileStats.cs
{"laser", new ProjectileStats(1000f, 1.0f, 0.0f, 0.0f, false)}

// SpellSystem.cs
{"YY", new SpellData("Laser Beam", 1, "laser")}
```

### Shotgun-Style Spell

```csharp
{"BBBB", new SpellData("Fire Blast", 5, "fireball")}
```

## File Summary

| File                 | When to Edit   | Purpose                   |
| -------------------- | -------------- | ------------------------- |
| `ProjectileStats.cs` | Always         | Define projectile physics |
| `SpellSystem.cs`     | Always         | Add spell definitions     |
| `NewProjectile.cs`   | New types only | Projectile behavior       |
| `NewProjectile.tscn` | New types only | Projectile scene          |

**Never edit:** `Player.cs`, `AimIndicator.cs`, `ElementBufferUI.cs` - these handle all spells automatically.

## Testing

1. Input element combination
2. Press cast trigger (spell prepares)
3. Aim with right stick (trajectory appears)
4. Press cast trigger again (projectiles fire)

## Notes

- Element combinations must be unique
- Projectile type names must match exactly between files
- Multi-projectile spells automatically create spread patterns
- Trajectory prediction works automatically for all projectile types
