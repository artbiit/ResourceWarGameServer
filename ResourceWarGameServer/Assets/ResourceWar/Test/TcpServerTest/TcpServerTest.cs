using ResourceWar.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TcpServerTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TcpServer.Instance.Init("0.0.0.0", 5555);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
