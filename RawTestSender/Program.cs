using System;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace RawTestSender
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var client = new TcpClient("localhost", 2525))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    Console.WriteLine(reader.ReadLine()); // Banner

                    writer.WriteLine("HELO test");
                    Console.WriteLine(reader.ReadLine());

                    writer.WriteLine("MAIL FROM:<iain@lennoxfamily.net>");
                    Console.WriteLine(reader.ReadLine());

                    writer.WriteLine("RCPT TO:<iain@lab.lennoxfamily.net>");
                    Console.WriteLine(reader.ReadLine());

                    writer.WriteLine("DATA");
                    Console.WriteLine(reader.ReadLine());

                    // Send body without headers
                    writer.WriteLine("This is a manual test email.");
                    writer.WriteLine(".");

                    Console.WriteLine(reader.ReadLine()); // Should be 250 OK or 554 Error
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
