using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;
namespace ResourceWar.Server
{
    public static class DotEnv
    {
        private static readonly Dictionary<System.Type, Dictionary<string, object>> envs = new();
        public static bool isConfiged { get; private set; }

        public static void Config(bool forceConfig = false)
        {
            if (isConfiged&& !forceConfig) {
                return;
            }

            string filePath = Path.Combine(Application.streamingAssetsPath, ".env");
            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"[DotEnv] .env file not found at : {filePath}");
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                if(string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var split = line.Split('=',2);
                if (split.Length != 2)
                {
                    Logger.LogError($"[DotEnv] Syntax Error. length is over 2 : {line}");
                    continue;
                }

                var key = split[0].Trim();
                var value = split[1].Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    Logger.LogError($"[DotEnv] Key can not be null : {line}");
                    continue;
                }

                StoreValue(key, value);

            }
           isConfiged = true;
        }

        private static void StoreValue(string key, string value)
        {
            if(int.TryParse(key, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var intValue))
            {
                Add(key, intValue);
            }else if(float.TryParse(key, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
            {
                Add(key, floatValue);
            }else if(bool.TryParse(key, out var boolValue))
            {
                Add(key, boolValue);
            }
            else
            {
                Add(key, value);
            }
        }

       public static void Add<T>(string key, T value)
        {
            Logger.Log($"{typeof(T)} Add => {key} / {value}");
            if (!envs.ContainsKey(typeof(T)))
            {
                envs.Add(typeof(T), new Dictionary<string, object>());
            }

            envs[typeof(T)].Add(key, value);
            
       }

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            if(envs.TryGetValue(typeof(T), out var typeDictionary) && typeDictionary.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            return defaultValue;
        }
    }
}
