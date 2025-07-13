using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer
{
    public class ArenaRoom
    {
        public string Id { get; }
        private Dictionary<string, ClientHandler> clients = new();
        private Dictionary<string, PlayerState> players = new();
        
        // Arena boundaries
        private const float ArenaWidth = 800f;
        private const float ArenaHeight = 600f;
        
        // Spawn positions for players
        private readonly Vector2[] spawnPoints = {
            new Vector2(100, 100),    // Top-left
            new Vector2(700, 100),    // Top-right
            new Vector2(100, 500),    // Bottom-left
            new Vector2(700, 500),    // Bottom-right
            new Vector2(400, 300)     // Center (for 5th player)
        };
        
        public ArenaRoom(string id)
        {
            Id = id;
        }
        
        public void AddPlayer(string playerId, ClientHandler client)
        {
            clients[playerId] = client;
            
            // Choose spawn position based on number of existing players
            var spawnIndex = players.Count % spawnPoints.Length;
            var spawnPos = spawnPoints[spawnIndex];
            
            // Create new player state
            players[playerId] = new PlayerState
            {
                PlayerId = playerId,
                Position = spawnPos,
                Velocity = Vector2.Zero,
                Health = 100f,
                MaxHealth = 100f
            };
            
            Console.WriteLine($"🎯 {playerId} spawned at {spawnPos}");
        }
        
        public void RemovePlayer(string playerId)
        {
            clients.Remove(playerId);
            players.Remove(playerId);
        }
        
        public bool HasPlayer(string playerId)
        {
            return players.ContainsKey(playerId);
        }
        
        public PlayerState? GetPlayer(string playerId)
        {
            return players.GetValueOrDefault(playerId);
        }
        
        public void UpdatePlayerPosition(string playerId, Vector2 position, Vector2 velocity)
        {
            if (players.ContainsKey(playerId))
            {
                // Keep player within arena bounds
                position.X = Math.Clamp(position.X, 0, ArenaWidth);
                position.Y = Math.Clamp(position.Y, 0, ArenaHeight);
                
                // Update player state
                players[playerId].Position = position;
                players[playerId].Velocity = velocity;
                players[playerId].LastUpdate = DateTime.UtcNow;
            }
        }
        
        public List<DamageResult> ProcessSpell(SpellCastData spellData)
        {
            var results = new List<DamageResult>();
            
            // Check if spell hits any players
            foreach (var kvp in players)
            {
                var targetPlayer = kvp.Value;
                
                // Skip the player who cast the spell and dead players
                if (targetPlayer.PlayerId == spellData.PlayerId || !targetPlayer.IsAlive)
                    continue;
                
                // Simple distance-based collision detection
                var distance = Vector2.Distance(spellData.Position, targetPlayer.Position);
                
                if (distance <= spellData.Radius)
                {
                    // Apply damage
                    var newHealth = Math.Max(0, targetPlayer.Health - spellData.Damage);
                    targetPlayer.Health = newHealth;
                    
                    // Create damage result
                    var damageResult = new DamageResult
                    {
                        PlayerId = targetPlayer.PlayerId,
                        AttackerId = spellData.PlayerId,
                        Damage = spellData.Damage,
                        NewHealth = newHealth,
                        HitPosition = targetPlayer.Position
                    };
                    
                    results.Add(damageResult);
                    
                    Console.WriteLine($"💥 {spellData.PlayerId} hit {targetPlayer.PlayerId} for {spellData.Damage} damage (HP: {newHealth:F0})");
                    
                    if (newHealth <= 0)
                    {
                        Console.WriteLine($"💀 {targetPlayer.PlayerId} was defeated!");
                    }
                }
            }
            
            return results;
        }
        
        public ArenaState GetArenaState()
        {
            return new ArenaState
            {
                ArenaId = Id,
                Players = players.Values.ToArray(),
                LastUpdate = DateTime.UtcNow
            };
        }
        
        public void BroadcastToAll(GameMessage message)
        {
            foreach (var client in clients.Values)
            {
                client.SendMessage(message);
            }
        }
        
        public void BroadcastToOthers(string excludePlayerId, GameMessage message)
        {
            foreach (var kvp in clients)
            {
                if (kvp.Key != excludePlayerId)
                {
                    kvp.Value.SendMessage(message);
                }
            }
        }
        
        // Get stats for debugging
        public int PlayerCount => players.Count;
        public int AlivePlayerCount => players.Values.Count(p => p.IsAlive);
    }
}