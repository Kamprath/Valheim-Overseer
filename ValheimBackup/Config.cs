namespace ValheimBackup
{
    public class Config
    {
        public string ServerName { get; set; }
        
        public string World { get; set; }

        public string Password { get; set; }

        public int Port { get; set; }

        public int SteamAppId { get; set; }

        public static Config GetDefault()
        {
            return new Config
            {
                ServerName = "New Valheim Server",
                Password = "secret",
                Port = 2456,
                SteamAppId = 892970
            };
        }
    }
}