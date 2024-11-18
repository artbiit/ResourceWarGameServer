using ResourceWar.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DotEnvTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DotEnv.Config();

        Debug.Log(DotEnv.Get<int>("intTest"));
        Debug.Log(DotEnv.Get<bool>("boolTest"));
        Debug.Log(DotEnv.Get<float>("floatTest"));
        Debug.Log(DotEnv.Get<string>("stringTest"));
        Debug.Log(DotEnv.Get<string>("emptyTest"));


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
