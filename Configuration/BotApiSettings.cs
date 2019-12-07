using System;
using System.ComponentModel.DataAnnotations;

namespace TwitchBotConsole.Configuration
{
    public class BotApiSettings
    {
        public string Protocol { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public Uri FullUri
        {
            get
            {
                return new Uri($"{Protocol}://{Host}:{Port}");
            }
        }
    }
}