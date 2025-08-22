// File: src/Nimbus.Framework/Config/ConfigLoader.cs
using NUnit.Framework; // Needed for TestContext.Parameters

namespace Nimbus.Framework.Utils
{
    /// <summary>
    /// Thread-safe configuration loader for Nimbus.
    /// Supports:
    ///  - Environment variables
    ///  - NUnit TestRunParameters (dotnet test -- TestRunParameters.Parameter)
    ///  - config.properties file
    ///
    /// Uses an immutable snapshot strategy:
    ///  - Reads never lock
    ///  - Reload replaces the entire dictionary atomically
    /// </summary>
    public static class ConfigLoader
    {
        private static readonly object _lock = new object();

        // Volatile ensures latest snapshot is always visible across threads
        private static volatile Dictionary<string, string> _config =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static ConfigLoader()
        {
            LoadConfigFile();
        }

        /// <summary>
        /// Retrieve a configuration value by key.
        /// Order of precedence:
        /// 1. Environment variables
        /// 2. NUnit TestRunParameters
        /// 3. config.properties file
        /// </summary>
        public static string Get(string key)
        {
            // 1. Environment variables
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
                return envValue;

            // 2. NUnit TestRunParameters
            try
            {
                if (TestContext.Parameters.Exists(key))
                {
                    var runParam = TestContext.Parameters[key];
                    if (!string.IsNullOrEmpty(runParam))
                        return runParam;
                }
            }
            catch
            {
                // Ignore if not running inside NUnit context
            }

            // 3. config.properties snapshot
            if (_config.TryGetValue(key, out var val))
                return val;

            throw new KeyNotFoundException($"Config key not found: {key}");
        }

        /// <summary>
        /// Reload config.properties into memory.
        /// Creates a new dictionary snapshot and swaps it atomically.
        /// </summary>
        private static void LoadConfigFile()
        {
            var newConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var configPath = Path.Combine(AppContext.BaseDirectory, "config.properties");
            if (File.Exists(configPath))
            {
                foreach (var line in File.ReadAllLines(configPath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    var parts = trimmed.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        newConfig[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            // Atomic swap (only writers lock)
            lock (_lock)
            {
                _config = newConfig;
            }
        }

        /// <summary>
        /// Safe accessor with default fallback.
        /// </summary>
        public static string GetOrDefault(string key, string defaultValue)
        {
            try
            {
                return Get(key);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}