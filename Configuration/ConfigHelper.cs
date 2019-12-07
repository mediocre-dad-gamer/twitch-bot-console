using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using TwitchBotConsole.Configuration.Attributes;

namespace TwitchBotConsole.Configuration
{
    public static class ConfigHelper
    {
        private static IConfigurationRoot Configuration { get; set; }

        public static T GetConfig<T>(string configSection) where T : new()
        {
            var localConfigObject = new T();

            var section = Configuration.GetSection(configSection);

            if (section.Exists())
            {
                section.Bind(localConfigObject);
            }

            PopulateSecrets(localConfigObject);

            return localConfigObject;
        }

        public static void BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();
            builder.AddJsonFile("appsettings.json");

            var environment = EnvironmentHelper.GetEnvironmentString();

            if (!string.IsNullOrEmpty(environment))
            {
                builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
            }

            Configuration = builder.Build();
        }

        private static void PopulateSecrets<T>(T configObject)
        {
            var type = typeof(T);

            var properties = type.GetProperties();

            foreach (var prop in properties)
            {

                var attribute = Attribute.GetCustomAttribute(prop, typeof(ConfigSecret));

                if (attribute == null)
                {
                    continue;
                }

                var configSecretAttribute = attribute as ConfigSecret;

                var configSecret = Configuration[configSecretAttribute.SecretKey];
                prop.SetValue(configObject, configSecret);
            }
        }
    }
}