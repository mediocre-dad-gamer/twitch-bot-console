using System;

namespace TwitchBotConsole.Configuration.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConfigSecret : Attribute
    {
        public string SecretKey { get; set; }

        public ConfigSecret(string secretKey)
        {
            SecretKey = secretKey;
        }
    }
}