using System;
using System.IO;
using System.Diagnostics;

namespace ValheimBackup
{
    class Program
    {
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
                Console.WriteLine("Invalid file. Please specify filepath to valheim_server.exe");
                return;
            }

            var server = new ValheimServer { FilePath = filePath };
            server.Start();
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