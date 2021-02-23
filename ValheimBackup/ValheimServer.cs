using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace ValheimBackup
{
    public class ValheimServer
    {
        public string FilePath { get; set; }
        
        private string ConnectingPlayerId { get; set; }

        private readonly Dictionary<string, string> _players = new Dictionary<string, string>();

        private Config Config { get; } = GetConfig();

        private Dictionary<string, string> Players => _players;

        public void Start()
        {
            var ip = GetPublicIp();

            if (ip != null)
            {
                ConsoleWrite($"Starting Valheim server at {ip}:{Config.Port}");
            }
            else
            {
                ConsoleWrite($"Starting Valheim server on port {Config.Port}");
            }
            
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
            if (output.Contains("Game server connected")) return OnServerReady(output);
            if (output.Contains("Got handshake from client ")) return OnPlayerConnecting(output);
            if (output.Contains(": Got character ZDOID from ")) return OnPlayerConnected(output);
            if (output.Contains(": Closing socket ")) return OnPlayerDisconnect(output);
            if (output.Contains(":  Connections ")) return OnConnectionCount(output);
            if (output.Contains("World saved (")) return OnWorldSave();
            
            return true;
        }
        
        private bool OnServerReady(string output)
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
            ConsoleWrite("World saved");
            SaveBackup();
            
            return true;
        }

        private void SaveBackup()
        {
            var worldDirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                .Replace("\\Roaming", "") + "\\LocalLow\\IronGate\\Valheim\\worlds";
            var backupDirPath = FilePath.Replace("\\valheim_server.exe", "\\backups");

            if (!Directory.Exists(worldDirPath))
            {
                ConsoleWrite($"ERROR: Backup failed. No worlds directory found at {worldDirPath}");
                return;
            }

            ConsoleWrite("Creating world backup...");

            // create backups directory if it doesn't exist
            if (!Directory.Exists(backupDirPath))
            {
                Directory.CreateDirectory(backupDirPath);
                ConsoleWrite("Backups directory created");
            }

            var worldDbPath = $"{worldDirPath}\\{Config.World}.db";
            var worldFwlPath = $"{worldDirPath}\\{Config.World}.fwl";

            // check if world files exist
            if (!File.Exists(worldDbPath) || !File.Exists(worldFwlPath))
            {
                ConsoleWrite($"ERROR: Backup failed. No world file found at {worldDbPath}");
                return;
            }

            var currentBackupDirPath =
                $"{backupDirPath}\\{DateTime.Now.Year}-{PrependZero(DateTime.Now.Month)}-{PrependZero(DateTime.Now.Day)} {PrependZero(DateTime.Now.Hour)}{PrependZero(DateTime.Now.Minute)}{PrependZero(DateTime.Now.Second)}";

            // todo: add files to Zip archive instead of folder
            Directory.CreateDirectory(currentBackupDirPath);
            File.Copy(worldDbPath, $"{currentBackupDirPath}\\{Config.World}.db");
            File.Copy(worldFwlPath, $"{currentBackupDirPath}\\{Config.World}.fwl");

            if (File.Exists($"{currentBackupDirPath}\\{Config.World}.db") &&
                File.Exists($"{currentBackupDirPath}\\{Config.World}.fwl"))
            {
                ConsoleWrite("Backup saved");
            }
            else
            {
                ConsoleWrite($"ERROR: Backup failed. Unable to copy world files to {currentBackupDirPath}.");
                return;
            }
            
            DeleteOldBackups(backupDirPath);
        }
        
        private void DeleteOldBackups(string backupDirPath)
        {
            var backupDirs = Directory.EnumerateDirectories(backupDirPath);
            backupDirs = backupDirs.OrderByDescending(x => x);
            
            while (backupDirs.Count() > Config.MaxWorldBackups)
            {
                var dirToDelete = backupDirs.Last();
                
                Directory.Delete(dirToDelete, true);

                if (Directory.Exists(dirToDelete))
                {
                    ConsoleWrite($"ERROR: Failed to delete old backup at {dirToDelete}");
                    return;
                }
                
                backupDirs = backupDirs.Where(x => x != dirToDelete).ToList();
            }
        }

        private static string GetPublicIp()
        {
            try
            {
                return new WebClient().DownloadString("http://icanhazip.com").Trim();
            }
            catch (Exception e)
            {
                ConsoleWrite("ERROR: Could not determine public IP address");
                return null;
            }
        }

        private static Config GetConfig()
        {
            if (!File.Exists("config.json"))
            {
                ConsoleWrite("ERROR: No config.json file found. Using default configuration.");
                return Config.GetDefault();
            }

            return JsonSerializer.Deserialize<Config>(
                File.ReadAllText("config.json")
            );
        }
        
        private static void ConsoleWrite(string message)
        {
            var now = DateTime.Now;
            Console.WriteLine($"* [{PrependZero(now.Month)}/{PrependZero(now.Day)}/{now.Year} {PrependZero(now.Hour)}:{PrependZero(now.Minute)}:{PrependZero(now.Second)}]  {message}");
        }

        private static string PrependZero(int number)
        {
            return number < 10 ? $"0{number}" : number.ToString();
        }
    }
}