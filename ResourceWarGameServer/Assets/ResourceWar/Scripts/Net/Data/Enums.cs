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

    public enum PlayerActionType : byte
    {
        STOP = 0,
        MOVE = 1,
        DASH = 2,
    }
    public enum PlayerEquippedItem : int
    {
        NONE = 1000,
        SOLIDWOOD = 1001,
        IRONSTONE = 1002,
        WOOD = 1003,
        IRON = 1004,
        GARBAGE = 1005,
    }

    public enum TeamChangeResultCode : uint
    {
        SUCCESS = 0,
        NONE_CHANGE = 1,
        FAIL = 2,
    }

    public enum PlayerIsReadyChangeResultCode : uint
    {
        SUCCESS = 0,
        FAIL = 1,
    }

    public enum GameSessionState : int
    {
        CREATING = 0,
        DESTROY,
        LOBBY,
        LOADING,
        PLAYING,
        GAMEOVER,
        ERROR = -1,
    }

    public enum SurrenderResultCode : uint
    {
        SUCCESS = 0,
        FAIL = 1,
    }
}
