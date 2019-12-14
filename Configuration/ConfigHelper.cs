using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
            builder.AddJsonFile("appsettings.json");

            var environment = EnvironmentHelper.GetEnvironmentString();

            if (!string.IsNullOrEmpty(environment))
            {
                builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
            }

            if (EnvironmentHelper.IsLocal())
            {
                builder.AddUserSecrets<Program>();
            }

            Configuration = builder.Build();

            if (!EnvironmentHelper.IsLocal())
            {
                using (var store = new X509Store(StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certs = store.Certificates
                        .Find(X509FindType.FindByThumbprint,
                            Configuration["AzureADCertThumbprint"], false);

                    builder.AddAzureKeyVault(
                        $"https://{Configuration["KeyVaultName"].ToLowerInvariant()}.vault.azure.net/",
                        Configuration["AzureADApplicationId"],
                        certs.OfType<X509Certificate2>().Single());

                    store.Close();
                }
                Configuration = builder.Build();
            }
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