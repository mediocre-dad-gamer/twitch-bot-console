using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IrcDotNet;
using TwitchBotConsole.BotApi;
using TwitchBotConsole.Configuration;
using TwitchBotConsole.Irc;

namespace TwitchBotConsole
{
    class Program
    {
        private const string _twitchSettingSection = "TwitchSettings";

        private const string _botApiSection = "BotApi";

        private static TwitchSettings _twitchSettings { get; set; }

        private static BotApiSettings _botSettings { get; set; }

        private static BotApiClient _botApiClient { get; set; }

        private static TwitchChatClient _chatClient { get; set; }

        static void Main(string[] args)
        {
            Startup();

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        private static void Startup()
        {
            StartupHelper.SetEnvironment();

            if (EnvironmentHelper.IsLocal())
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            ConfigHelper.BuildConfiguration();
            _twitchSettings = ConfigHelper.GetConfig<TwitchSettings>(_twitchSettingSection);
            _botSettings = ConfigHelper.GetConfig<BotApiSettings>(_botApiSection);
            _botApiClient = new BotApiClient(_botSettings);

            _chatClient = StartupHelper.ConnectToIrc(_twitchSettings, _botApiClient);
        }
    }
}
