using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
public class IndependentClass
{
    Dictionary<int, object> datas = new Dictionary<int, object>()
    {
        {0, NanoidDotNet.Nanoid.Generate() },
        {1,  Random.Range(int.MinValue, int.MaxValue) }
    };

    public IndependentClass()
    {
        string log = nameof(IndependentClass);
        foreach (var kvp in datas)
        {
            log += $"\n{kvp.Key}-{kvp.Value}";
        }
        Debug.Log(log);

        DataDispatcher<int, object>.Instance.SetProvider((key) => UniTask.FromResult(datas[key]));
    }
}
public class DataDispatcherTest : MonoBehaviour
{
    public int key = 0;
    IndependentClass independentClass;
    private void Awake()
    {
        independentClass = new IndependentClass();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GetData().Forget();
        }
    }

    private async UniTask GetData()
    {
        Debug.Log(await DataDispatcher<int, object>.Instance.RequestDataAsync(key));
    }
}
