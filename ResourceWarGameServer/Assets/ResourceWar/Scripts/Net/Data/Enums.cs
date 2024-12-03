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
    public enum PlayerEquippedItem : uint
    {
        NONE = 0,
        SOLIDWOOD = 1,
        IRONSTONE = 2,
        WOOD = 3,
        IRON = 4,
        GARBAGE = 5,
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
