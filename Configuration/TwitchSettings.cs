using TwitchBotConsole.Configuration.Attributes;

namespace TwitchBotConsole.Configuration
{
    public class TwitchSettings
    {
        public string Username { get; set; }
        public string ChannelToJoin { get; set; }
        public string Server { get; set; }

        [ConfigSecret("TwitchOAuthSecret")]
        public string Password { get; set; }
    }
}