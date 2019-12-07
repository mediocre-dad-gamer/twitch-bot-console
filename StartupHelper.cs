using System;
using TwitchBotConsole.BotApi;
using TwitchBotConsole.Configuration;
using TwitchBotConsole.Irc;

namespace TwitchBotConsole
{
    public static class StartupHelper
    {
        public static void SetEnvironment()
        {
            var environmentName = Environment.GetEnvironmentVariable("ENV");
            EnvironmentHelper.SetEnvironment(environmentName);
        }

        public static TwitchChatClient ConnectToIrc(TwitchSettings twitchSettings, BotApiClient apiClient)
        {
            return new TwitchChatClient(twitchSettings, apiClient);
        }
    }
}