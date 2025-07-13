using System;
using System.Threading.Tasks;

namespace GameServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Spell Slingers Game Server ===");
            Console.WriteLine("Starting server...");
            
            var server = new ArenaGameServer();
            
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nShutting down server...");
                server.Stop();
            };
            
            try
            {
                await server.Start(7000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Server error: {ex.Message}");
            }
            
            Console.WriteLine("Server stopped. Press any key to exit.");
            Console.ReadKey();
        }
    }
}