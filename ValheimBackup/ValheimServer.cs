using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ValheimBackup
{
    public class ValheimServer
    {
        public string FilePath { get; set; }
        
        private string ConnectingPlayerId { get; set; }

        private readonly Dictionary<string, string> _players = new Dictionary<string, string>();

        private Config Config { get; } = new Config();

        private Dictionary<string, string> Players => _players;

        public void Start()
        {
            ConsoleWrite("Starting Valheim server...");
            RunExecutable();
        }

        private void RunExecutable()
        {
            using var process = new Process();
            
            // stop process on CTRL+C
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                ConsoleWrite("Stopping server...");
                    
                process.CloseMainWindow();
                process.WaitForExit();
                process.Close();
            };
            
            ConfigureStartInfo(process.StartInfo);
                
            process.OutputDataReceived += (sender, e) => HandleOutputDataReceived(e.Data);
            process.ErrorDataReceived += (sender, e) => ConsoleWrite($"[ERROR] {e.Data}");

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        private void ConfigureStartInfo(ProcessStartInfo startInfo)
        {
            startInfo.Arguments = $"-nographics -batchmode -name \"{Config.ServerName}\" -port {Config.Port} -world \"{Config.World}\" -password \"{Config.Password}\"";
            startInfo.FileName = FilePath;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.Environment.Add("SteamAppId", Config.SteamAppId.ToString());
        }
        
        private bool HandleOutputDataReceived(string output)
        {
            if (output == null) return false;
            
            // todo: write output to log file
            
            if (output.Contains("(Filename: ")) return false;
            if (output.Trim() == "") return false;
            
            // todo: make these calls async
            if (output.Contains("Game server connected")) return OnWorldReady(output);
            if (output.Contains("Got handshake from client ")) return OnPlayerConnecting(output);
            if (output.Contains(": Got character ZDOID from ")) return OnPlayerConnected(output);
            if (output.Contains(": Closing socket ")) return OnPlayerDisconnect(output);
            if (output.Contains(":  Connections ")) return OnConnectionCount(output);
            if (output.Contains("World saved (")) return OnWorldSave();
            
            return true;
        }
        
        private bool OnWorldReady(string output)
        {
            ConsoleWrite("Server ready!");
            return true;
        }

        private bool OnPlayerConnecting(string output)
        {
            var searchStr = "Got handshake from client ";
            
            ConnectingPlayerId = output.Substring(
                output.IndexOf(searchStr, StringComparison.Ordinal) + searchStr.Length
            ).Trim();

            return true;
        }
        
        private bool OnPlayerConnected(string output)
        {
            var startStr = "Got character ZDOID from ";

            var tempString = output.Substring(
                output.IndexOf(startStr, StringComparison.Ordinal) + startStr.Length
            );
            var playerName = tempString.Substring(
                0, 
                tempString.IndexOf(" : ", StringComparison.Ordinal)
            );

            if (Players.ContainsValue(playerName)) return false;
            
            Players.Add(ConnectingPlayerId, playerName);
            ConnectingPlayerId = null;

            ConsoleWrite($"Player {playerName} connected");
            return true;
        }

        private bool OnPlayerDisconnect(string output)
        {
            var startStr = ": Closing socket ";
            var playerId = output.Substring(
                output.IndexOf(startStr, StringComparison.Ordinal) + startStr.Length
            ).Trim();

            if (playerId == "0") return false;

            // if connecting player disconnects, reset the ID
            if (playerId == ConnectingPlayerId)
            {
                ConnectingPlayerId = null;
                return false;
            }

            if (!Players.ContainsKey(playerId))
            {
                ConsoleWrite($"Unknown player disconnected ({playerId})");
                return false;
            }

            ConsoleWrite($"Player {Players[playerId]} disconnected");

            Players.Remove(playerId);

            return true;
        }

        private bool OnConnectionCount(string output)
        {
            return true;
        }

        private bool OnWorldSave()
        {
            // todo: create world backup
            ConsoleWrite("World saved");
            return true;
        }
        
        private void ConsoleWrite(string message)
        {
            var now = DateTime.Now;
            Console.WriteLine($"[{now.Month}/{now.Day}/{now.Year} {now.Hour}:{now.Minute}:{now.Second}]  {message}");
        }
    }
}