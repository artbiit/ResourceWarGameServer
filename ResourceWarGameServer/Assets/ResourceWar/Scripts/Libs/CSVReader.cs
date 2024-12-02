using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server.Lib
{
    public class CSVReader
    {
        public static Dictionary<int, T> ReadCsv<T>(string relativePath) where T : new()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, "CSV", $"{relativePath}.csv");

            if (!File.Exists(fullPath))
            {
                Logger.LogError($"Could not found csv file : {fullPath}");
                return null;
            }

            string[] lines = File.ReadAllLines(fullPath);

            //주석 포함 
            if (lines.Length < 3)
            {
                Logger.LogError($"Data is not enough [{lines.Length}] : {fullPath}");
                return null;
            }

            string[] headers = lines[0].Split(',');
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim();
            }

            Dictionary<int, T> dictionary = new Dictionary<int, T>();

            for (int i = 2; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(",");

                T instance = new T();
                int id = -1;

                for (int j = 0; j < headers.Length; j++)
                {
                    string value = values[j].Trim();
                    string header = headers[j];

                    var property = typeof(T).GetProperty(header);
 

                    if (header.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        id = int.Parse(value);
                    }
                    if (property != null && property.CanWrite)
                    {
                        object convertedValue = Convert.ChangeType(value, property.PropertyType);
                        if (typeof(T).IsValueType)
                        {
                            var boxedInstance = (object)instance;
                            property.SetValue(boxedInstance, convertedValue);
                            instance = (T)boxedInstance;
                        }
                        else
                        {
                            property.SetValue(instance, convertedValue);
                        }
                    }

                }

                if (id != -1)
                {
                    dictionary[id] = instance;
                }
            }
            return dictionary;
        }
    }
}
