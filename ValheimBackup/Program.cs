using System;
using System.IO;
using System.Diagnostics;

namespace ValheimBackup
{
    class Program
    {
        private const string ServerName = "Test Server";
        private const string World = "Dedicated";
        private const string Password = "secret";
        private const int Port = 2456;
        private const string SteamAppId = "892970";
        
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Usage: ValheimManager.exe <path to Valheim executable>");
                return;
            }

            var filePath = args[0];

            if (!IsStartupFileValid(filePath))
            {
                Console.WriteLine("Invalid startup file. Please specify the path to your Valheim server startup file.");
                return;
            }
            
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName = filePath;
                process.StartInfo.Arguments = $"-nographics -batchmode -name \"{ServerName}\" -port {Port} -world \"{World}\" -password \"{Password}\"";
                process.StartInfo.Environment.Add("SteamAppId", SteamAppId);

                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Console.WriteLine($"ERROR: {e.Data}");

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.Read();
        }

        static bool IsStartupFileValid(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist.");
                return false;
            }

            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Extension != ".exe")
            {
                Console.WriteLine("File is not an executable.");
                return false;
            }

            return true;
        }
    }
}