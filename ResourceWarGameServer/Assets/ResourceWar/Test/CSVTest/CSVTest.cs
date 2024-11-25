using ResourceWar.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSVTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // CSV 데이터 로드
        LoadCSV.LoadItemsFromCSV();

        // 로드된 데이터 확인 (디버깅용)
        foreach (var kvp in LoadCSV.Items)
        {
            Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
        }
    }
}
