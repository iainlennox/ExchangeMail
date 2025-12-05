using System;
using System.IO;
using System.Linq;
using ExchangeMail.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace TempLogReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = @"C:\ExchangeMailData\exchangemail.db";
            Console.WriteLine($"Looking for DB at: {dbPath}");

            if (!File.Exists(dbPath))
            {
                Console.WriteLine("Database file not found!");
                return;
            }

            var connectionString = $"Data Source={dbPath}";
            var options = new DbContextOptionsBuilder<ExchangeMailContext>()
                .UseSqlite(connectionString)
                .Options;

            using var context = new ExchangeMailContext(options);
            try
            {
                var log = context.Logs
                    .OrderByDescending(l => l.Id)
                    .FirstOrDefault();

                if (log != null)
                {
                    Console.WriteLine($"[{log.Date}] {log.Level} - {log.Source}: {log.Message}");
                    if (!string.IsNullOrEmpty(log.Exception))
                    {
                        Console.WriteLine("Exception:");
                        Console.WriteLine(log.Exception);
                    }
                }
                else
                {
                    Console.WriteLine("No logs found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading logs: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
