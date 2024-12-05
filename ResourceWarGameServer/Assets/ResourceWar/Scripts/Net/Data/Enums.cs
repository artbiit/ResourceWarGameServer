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

    public enum FurnaceResultCode : uint
    {
        SUCCESS = 0,
        INVALID_ITEM = 1,
        RUNNING_STATE = 2,
        FAIL = 3,
    }

    public enum SyncFurnaceStateCode : uint
    {
        WAITING = 0,        // 플레이어가 아이템을 넣을 수 있는 대기 상태
        RUNNING = 1,        // 템을 제작 중인 상태 (1~99)
        PRODUCING = 2,      // 제작이 완료된 상태 (100~149)
        OVERFLOW = 3,       // 제작 완료 후 초과 상태, 쓰레기가 나오는 상태 (150 이상)
    }
}
