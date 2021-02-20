using System;
using System.IO;
using System.Diagnostics;

namespace ValheimBackup
{
    class Program
    {
        // todo: store this info in a config file
        private const string ServerName = "Test Server";
        private const string World = "Dedicated";
        private const string Password = "secret";
        private const int Port = 2456;
        private const string SteamAppId = "892970";
        
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Usage: ValheimManager.exe <path to valheim_server.exe>");
                return;
            }

            var filePath = args[0];

            if (!IsStartupFileValid(filePath))
            {
                Console.WriteLine("Invalid startup file. Please specify filepath to valheim_server.exe");
                return;
            }

            ConsoleWrite("Starting Valheim server...");

            StartServerExecutable(filePath);
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

        static void StartServerExecutable(string filePath)
        {
            using (Process process = new Process())
            {
                ConfigureStartInfo(process.StartInfo, filePath);

                process.OutputDataReceived += (sender, e) => HandleOutputDataReceived(e.Data);
                process.ErrorDataReceived += (sender, e) => ConsoleWrite($"[ERROR] {e.Data}");
                
                // kill process when Ctrl + C is used to stop the console app
                Console.CancelKeyPress += (sender, e) =>
                {
                    ConsoleWrite("Stopping server...");
                    process.Close();
                    process.Dispose();
                };

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
        }

        static void ConfigureStartInfo(ProcessStartInfo startInfo, string filePath)
        {
            startInfo.Arguments = $"-nographics -batchmode -name \"{ServerName}\" -port {Port} -world \"{World}\" -password \"{Password}\"";
            startInfo.FileName = filePath;
            
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            
            startInfo.Environment.Add("SteamAppId", SteamAppId);
        }

        static bool HandleOutputDataReceived(string output)
        {
            // todo: write output to log file
            
            if (output.Contains("(Filename: ")) return false;
            if (output.Trim() == "") return false;
            
            if (output.Contains("Game server connected")) return OnWorldReady(output);
            if (output.Contains(": Got character ZDOID from ")) return OnPlayerConnect(output);
            if (output.Contains(": Closing socket ")) return OnPlayerDisconnect(output);
            if (output.Contains(":  Connections ")) return OnConnectionCount(output);
            if (output.Contains("World saved (")) return OnWorldSave();
            
            Console.WriteLine($"Debug: {output}");
            return true;
        }

        static bool OnWorldReady(string output)
        {
            ConsoleWrite("Server ready!");
            return true;
        }

        static bool OnPlayerConnect(string output)
        {
            var startStr = "Got character ZDOID from ";
            var startPos = output.IndexOf(startStr, StringComparison.Ordinal) + startStr.Length;

            var tempString = output.Substring(startPos);
            var playerName = tempString.Substring(0, tempString.IndexOf(" : ", StringComparison.Ordinal));

            ConsoleWrite($"Player {playerName} connected");
            return true;
        }

        static bool OnPlayerDisconnect(string output)
        {
            var startStr = ": Closing socket ";
            var playerId = output.Substring(
                output.IndexOf(startStr, StringComparison.Ordinal) + startStr.Length
            ).Trim();

            if (playerId == "0") return false;
            
            ConsoleWrite($"Player disconnected ({playerId})");
            return true;
        }

        static bool OnConnectionCount(string output)
        {
            return true;
        }

        static bool OnWorldSave()
        {
            // todo: create world backup
            ConsoleWrite("World saved.");
            return true;
        }

        static void ConsoleWrite(string message)
        {
            var now = DateTime.Now;
            Console.WriteLine($"[{now.Month}/{now.Day}/{now.Year} {now.Hour}:{now.Minute}:{now.Second}]  {message}");
        }
    }
}