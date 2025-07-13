using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameServer
{
    public class ClientHandler
    {
        private string clientId;
        private TcpClient tcpClient;
        private NetworkStream stream;
        private ArenaGameServer server;
        private bool isConnected = true;
        
        public ClientHandler(string clientId, TcpClient tcpClient, ArenaGameServer server)
        {
            this.clientId = clientId;
            this.tcpClient = tcpClient;
            this.server = server;
            this.stream = tcpClient.GetStream();
        }
        
        public async Task HandleClient()
        {
            var buffer = new byte[4096];
            var messageBuffer = new StringBuilder();
            
            try
            {
                while (isConnected && tcpClient.Connected)
                {
                    // Read data from client
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }
                    
                    // Convert bytes to string
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(data);
                    
                    // Process complete messages (separated by newlines)
                    string messages = messageBuffer.ToString();
                    string[] lines = messages.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    // Process all complete messages
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            // If this is the last line and doesn't end with newline, 
                            // it might be incomplete
                            if (i == lines.Length - 1 && !messages.EndsWith('\n'))
                            {
                                // Keep this incomplete message for next iteration
                                messageBuffer.Clear();
                                messageBuffer.Append(line);
                                break;
                            }
                            
                            try
                            {
                                // Parse and handle the message
                                var message = JsonConvert.DeserializeObject<GameMessage>(line);
                                if (message != null)
                                {
                                    server.HandleMessage(clientId, message);
                                }
                            }
                            catch (JsonException ex)
                            {
                                Console.WriteLine($"⚠️ Invalid JSON from {clientId}: {ex.Message}");
                                Console.WriteLine($"Data: {line}");
                            }
                        }
                    }
                    
                    // If we processed all messages, clear the buffer
                    if (messages.EndsWith('\n'))
                    {
                        messageBuffer.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error handling client {clientId}: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }
        
        public void SendMessage(GameMessage message)
        {
            if (!isConnected) return;
            
            try
            {
                // Convert message to JSON and add newline
                string json = JsonConvert.SerializeObject(message) + "\n";
                byte[] data = Encoding.UTF8.GetBytes(json);
                
                // Send to client
                stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending message to {clientId}: {ex.Message}");
                Disconnect();
            }
        }
        
        public void Disconnect()
        {
            if (!isConnected) return;
            
            isConnected = false;
            
            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error closing connection for {clientId}: {ex.Message}");
            }
            
            // Tell server this client is gone
            server.RemoveClient(clientId);
        }
    }
}