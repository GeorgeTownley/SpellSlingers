using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameServer
{
    public class ArenaGameServer
    {
        private TcpListener listener;
        private bool isRunning = false;
        private Dictionary<string, ClientHandler> clients = new();
        private ArenaRoom mainArena;
        private int nextClientId = 1;
        
        public async Task Start(int port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;
                
                // Create main arena room
                mainArena = new ArenaRoom("main_arena");
                
                Console.WriteLine($"✅ Server started on port {port}");
                Console.WriteLine($"📍 Players can connect to: {GetLocalIPAddress()}:{port}");
                Console.WriteLine("Waiting for players...\n");
                
                // Accept client connections
                while (isRunning)
                {
                    try
                    {
                        var tcpClient = await listener.AcceptTcpClientAsync();
                        var clientId = $"player_{nextClientId++}";
                        
                        var clientHandler = new ClientHandler(clientId, tcpClient, this);
                        clients[clientId] = clientHandler;
                        
                        Console.WriteLine($"🎮 {clientId} connected ({clients.Count} total players)");
                        
                        // Start handling this client in the background
                        _ = Task.Run(() => clientHandler.HandleClient());
                    }
                    catch (ObjectDisposedException)
                    {
                        // Expected when server is stopping
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (isRunning)
                        {
                            Console.WriteLine($"❌ Error accepting client: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start server: {ex.Message}");
            }
        }
        
        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            
            // Disconnect all clients
            foreach (var client in clients.Values)
            {
                client.Disconnect();
            }
            
            clients.Clear();
            Console.WriteLine("🛑 Server stopped");
        }
        
        public void RemoveClient(string clientId)
        {
            if (clients.ContainsKey(clientId))
            {
                mainArena.RemovePlayer(clientId);
                clients.Remove(clientId);
                Console.WriteLine($"👋 {clientId} disconnected ({clients.Count} total players)");
                
                // Tell other players this player left
                BroadcastToAllExcept(clientId, new GameMessage
                {
                    Type = MessageType.PlayerLeft,
                    Data = clientId
                });
            }
        }
        
        public void HandleMessage(string clientId, GameMessage message)
        {
            try
            {
                switch (message.Type)
                {
                    case MessageType.JoinArena:
                        HandleJoinArena(clientId);
                        break;
                    case MessageType.PlayerMove:
                        HandlePlayerMove(clientId, message.Data);
                        break;
                    case MessageType.CastSpell:
                        HandleCastSpell(clientId, message.Data);
                        break;
                    default:
                        Console.WriteLine($"⚠️ Unknown message type: {message.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error handling message from {clientId}: {ex.Message}");
            }
        }
        
        private void HandleJoinArena(string clientId)
        {
            mainArena.AddPlayer(clientId, clients[clientId]);
            
            Console.WriteLine($"⚔️ {clientId} joined the arena");
            
            // Send current arena state to new player
            var arenaState = new GameMessage
            {
                Type = MessageType.ArenaState,
                Data = JsonConvert.SerializeObject(mainArena.GetArenaState())
            };
            clients[clientId].SendMessage(arenaState);
            
            // Tell other players about new player
            BroadcastToAllExcept(clientId, new GameMessage
            {
                Type = MessageType.PlayerJoined,
                Data = JsonConvert.SerializeObject(mainArena.GetPlayer(clientId))
            });
        }
        
        private void HandlePlayerMove(string clientId, string data)
        {
            var moveData = JsonConvert.DeserializeObject<PlayerMoveData>(data);
            mainArena.UpdatePlayerPosition(clientId, moveData.Position, moveData.Velocity);
            
            // Send position update to other players (not back to sender)
            BroadcastToAllExcept(clientId, new GameMessage
            {
                Type = MessageType.PlayerMove,
                Data = data
            });
        }
        
        private void HandleCastSpell(string clientId, string data)
        {
            var spellData = JsonConvert.DeserializeObject<SpellCastData>(data);
            spellData.PlayerId = clientId; // Make sure we have the right player ID
            
            Console.WriteLine($"✨ {clientId} cast {spellData.SpellType} at {spellData.Position}");
            
            // Process spell collision and damage
            var results = mainArena.ProcessSpell(spellData);
            
            // Tell all players about the spell
            BroadcastToAll(new GameMessage
            {
                Type = MessageType.SpellCast,
                Data = JsonConvert.SerializeObject(spellData)
            });
            
            // Send damage results if anyone got hit
            foreach (var result in results)
            {
                BroadcastToAll(new GameMessage
                {
                    Type = MessageType.PlayerDamaged,
                    Data = JsonConvert.SerializeObject(result)
                });
                
                if (result.NewHealth <= 0)
                {
                    Console.WriteLine($"💀 {result.PlayerId} was defeated by {clientId}");
                }
            }
        }
        
        private void BroadcastToAll(GameMessage message)
        {
            foreach (var client in clients.Values)
            {
                client.SendMessage(message);
            }
        }
        
        private void BroadcastToAllExcept(string excludeClientId, GameMessage message)
        {
            foreach (var kvp in clients)
            {
                if (kvp.Key != excludeClientId)
                {
                    kvp.Value.SendMessage(message);
                }
            }
        }
        
        private string GetLocalIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "localhost";
            }
            catch
            {
                return "localhost";
            }
        }
    }
}