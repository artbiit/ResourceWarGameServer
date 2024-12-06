using Cysharp.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Protocol;
using ResourceWar.Server.Lib;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class Object
    {

        public int ObjectId;
        public Vector3 position = Vector3.zero;

        public Object(int ObjectId)
        {
            
        }
    }
}
