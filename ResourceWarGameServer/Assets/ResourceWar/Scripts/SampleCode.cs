using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class SampleCode : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            Logger.Log("테스트2");
            Logger.OpenLogFolder();
            Logger.OpenCurrentLogFile();
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
