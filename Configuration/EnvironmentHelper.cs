using System;
using System.Collections.Generic;
using System.Linq;

namespace TwitchBotConsole
{
    public static class EnvironmentHelper
    {
        private const string LOCAL_ENVIRONMENT = "local";

        private static List<string> _validEnvironments = new List<string>
        {
            "local",
            "docker",
            "qa",
            "staging",
            "production"
        };

        private static string _currentEnvironment { get; set; }

        public static bool IsLocal()
        {
            return _currentEnvironment == LOCAL_ENVIRONMENT;
        }

        public static void SetEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                environment = LOCAL_ENVIRONMENT;
                Console.WriteLine($"WARNING: No environment was set. Defaulting to {LOCAL_ENVIRONMENT}.");
            }

            if (!IsValidEnvironment(environment))
            {
                throw new ArgumentException(
                    $"Environment {environment} is not in the list of valid environments"
                );
            }

            _currentEnvironment = environment.ToLowerInvariant();
        }

        public static string GetEnvironmentString()
        {
            return _currentEnvironment;
        }

        private static bool IsValidEnvironment(string environment)
        {
            return _validEnvironments.Any(v => v == environment.ToLowerInvariant());
        }
    }
}