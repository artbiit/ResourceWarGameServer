using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server.Lib
{
    public static class Logger
    {
        // Log
        public static void Log(object message)
        {
            UnityLog(message);
        }

        public static void Log(object message, UnityEngine.Object context)
        {
            UnityLog(message, context);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        private static void UnityLog(object message, UnityEngine.Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.Log(message);
            }
            else
            {
                UnityEngine.Debug.Log(message, context);
            }
        }

        // LogWarning
        public static void LogWarning(object message)
        {
            UnityLogWarning(message);
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
            UnityLogWarning(message, context);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        private static void UnityLogWarning(object message, UnityEngine.Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                UnityEngine.Debug.LogWarning(message, context);
            }
        }

        // LogError
        public static void LogError(object message)
        {
            UnityLogError(message);
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
            UnityLogError(message, context);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        private static void UnityLogError(object message, UnityEngine.Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogError(message);
            }
            else
            {
                UnityEngine.Debug.LogError(message, context);
            }
        }

        // LogAssertion
        public static void LogAssertion(object message)
        {
            UnityLogAssertion(message);
        }

        public static void LogAssertion(object message, UnityEngine.Object context)
        {
            UnityLogAssertion(message, context);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        private static void UnityLogAssertion(object message, UnityEngine.Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogAssertion(message);
            }
            else
            {
                UnityEngine.Debug.LogAssertion(message, context);
            }
        }

        // LogException
        public static void LogException(Exception exception)
        {
            UnityLogException(exception);
        }

        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            UnityLogException(exception, context);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        private static void UnityLogException(Exception exception, UnityEngine.Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogException(exception);
            }
            else
            {
                UnityEngine.Debug.LogException(exception, context);
            }
        }
    }
}
