using System;

namespace GameServer
{
    // Different types of messages between client and server
    public enum MessageType
    {
        JoinArena,      // Client wants to join the game
        PlayerJoined,   // Server tells everyone a player joined
        PlayerLeft,     // Server tells everyone a player left
        PlayerMove,     // Client sends position update
        CastSpell,      // Client wants to cast a spell
        SpellCast,      // Server tells everyone a spell was cast
        PlayerDamaged,  // Server tells everyone about damage
        ArenaState      // Server sends current game state
    }
    
    // Wrapper for all messages
    public class GameMessage
    {
        public MessageType Type { get; set; }
        public string Data { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    // Simple 2D vector (like Godot's Vector2)
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }
        
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public static float Distance(Vector2 a, Vector2 b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }
        
        public static Vector2 Zero => new Vector2(0, 0);
        
        public override string ToString() => $"({X:F1}, {Y:F1})";
    }
    
    // Represents a player in the game
    public class PlayerState
    {
        public string PlayerId { get; set; } = "";
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Health { get; set; } = 100f;
        public float MaxHealth { get; set; } = 100f;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
        
        public bool IsAlive => Health > 0;
    }
    
    // Data for player movement
    public class PlayerMoveData
    {
        public string PlayerId { get; set; } = "";
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
    }
    
    // Data for spell casting
    public class SpellCastData
    {
        public string PlayerId { get; set; } = "";
        public string SpellType { get; set; } = "";
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public float Speed { get; set; } = 500f;
        public float Damage { get; set; } = 25f;
        public float Radius { get; set; } = 30f;
    }
    
    // Result when a spell hits someone
    public class DamageResult
    {
        public string PlayerId { get; set; } = "";
        public string AttackerId { get; set; } = "";
        public float Damage { get; set; }
        public float NewHealth { get; set; }
        public Vector2 HitPosition { get; set; }
    }
    
    // Complete state of the arena
    public class ArenaState
    {
        public PlayerState[] Players { get; set; } = Array.Empty<PlayerState>();
        public string ArenaId { get; set; } = "";
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }
}