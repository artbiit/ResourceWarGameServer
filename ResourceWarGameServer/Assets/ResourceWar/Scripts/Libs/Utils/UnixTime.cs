using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    /// <summary>
    /// NodeJS의 Date.Now()처럼 ms기준 unix 타임으로 계산하을 돕기위한 클래스
    /// </summary>
    public static class UnixTime
    {
       public static long Now() =>  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); 

        public static DateTimeOffset ToDateTimeOffset(long unixTime) => DateTimeOffset.FromUnixTimeMilliseconds(unixTime);

        public static DateTime ToDateTime(long unixTime) => ToDateTimeOffset(unixTime).UtcDateTime;

    }
}
