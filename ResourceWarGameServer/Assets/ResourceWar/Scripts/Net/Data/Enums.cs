using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
   public enum AuthorizeResultCode : uint
    {
        SUCCESS = 0,
        ALREADY_AUTHORIZED = 1,
        FAIL = 2,
    }

    public enum ServerState : int
    {
        CREATING = 0,
        DESTROY,
        LOBBY,
        LOADING,
        PLAYING
    }
}
