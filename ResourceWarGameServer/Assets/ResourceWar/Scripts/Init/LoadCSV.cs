using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class LoadCSV
    {
        /// <summary>
        /// Item 데이터를 저장하는 정적 Dictionary
        /// Key: ItemCode, Value: Item 객체
        /// </summary>
        public static Dictionary<int, ItemClass> Items { get; private set; } = new Dictionary<int, ItemClass>();

        public static void LoadItemsFromCSV()
        {
            // Resources 폴더 내 경로 (파일 확장자 제외)
            string path = "Items/ItemData";

            // CSV 파일 읽기
            TextAsset csvData = Resources.Load<TextAsset>(path);
            if (csvData == null)
            {
                Logger.LogError($"Failed to load CSV file at {path}. Check if the file exists in Resources.");
                return;
            }

            // 줄 단위로 분리
            string[] lines = csvData.text.Split('\n');

            // 데이터 파싱 (헤더 및 주석 무시)
            Items.Clear(); // 기존 데이터 초기화
            for (int i = 3; i <lines.Length; i++)
            {
                string line = lines[i].Trim();

                // 빈 줄은 건너뜀
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] values = line.Split(',');

                try
                {
                    int itemCode = int.Parse(values[0]); // ItemCode를 Key로 사용

                    var item = new ItemClass
                    {
                        ItemCode = itemCode,
                        Name = values[1],
                        Type = (ItemType)int.Parse(values[2]),
                        Prefab = values[3]
                    };

                    // Dictionary에 추가
                    if (!Items.ContainsKey(itemCode))
                    {
                        Items.Add(itemCode, item);
                    }
                    else
                    {
                        Logger.LogWarning($"Duplicate ItemCode found: {itemCode}. Skipping this entry.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error parsing line {i + 1}: {line}. Exception: {ex.Message}");
                }
            }

            Logger.Log($"Loaded {Items.Count} unique items from CSV.");
        }
    }
}
