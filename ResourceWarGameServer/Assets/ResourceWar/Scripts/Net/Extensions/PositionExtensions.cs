using Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public static class PositionExtensions
    {
        public static Vector3 ToVector3(this Position position) => new Vector3(position.X, position.Y, position.Z);
        public static Vector3 ToVector3() => new Vector3(0f, 0f, 0f);
        public static Vector2 ToVector2(this Position position) => new Vector2(position.X, position.Y);
        public static Position FromVector(this Vector3 vector) => new Position { X = vector.x, Y = vector.y, Z = vector.z };
        public static Vector3 RoundVector3(Vector3 vector, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10, decimalPlaces);
            return new Vector3(
                Mathf.Round(vector.x * multiplier) / multiplier,
                Mathf.Round(vector.y * multiplier) / multiplier,
                Mathf.Round(vector.z * multiplier) / multiplier
            );
        }
    }
}
