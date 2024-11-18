using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.VersionControl;
using UnityEngine;

namespace ResourceWar.Server.Lib
{
    public static class Logger
    {

        private static readonly string LogDirectory = Path.Combine(Application.persistentDataPath, "Logs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "application.log");
        private static readonly object FileLock = new object();
        private static readonly int BufferLimit = 10;
        private static readonly StringBuilder logBuffer = new StringBuilder(BufferLimit);
        private static bool disposedValue = false;
        private static DateTime currentLogTime = DateTime.UtcNow;
        private static string currentLogFileName = GetLogFileName(DateTime.UtcNow);

        public enum LogLevel
        {
            Log,
            WARNING,
            ERROR,
            EXCEPTION
        }

        static Logger()
        {
            // 로그 디렉토리 생성
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
            Application.wantsToQuit += Application_wantsToQuit;
        }

        private static bool Application_wantsToQuit()
        {
            Dispose(true);
            return true;
        }

        private static string GetLogFileName(DateTime dateTime)
        {
            return Path.Combine(LogDirectory, $"{dateTime:yyyy-MM-dd_HH}.log");
        }

        private static void CheckAndRotateLogFile()
        {
            DateTime now = DateTime.UtcNow;
            if (currentLogTime.Hour != now.Hour)
            {
                FlushBuffer();

                lock (FileLock)
                {
                    currentLogTime = now;
                    currentLogFileName = GetLogFileName(now);
                }
            }
        }

        private static void FlushBuffer()
        {
            if (logBuffer.Length > 0)
            {
                lock (FileLock)
                {
                    File.AppendAllText(currentLogFileName, logBuffer.ToString());
                    logBuffer.Clear();
                }
            }
        }

        private static void WriteToBuffer(string message)
        {
            CheckAndRotateLogFile();
            lock (FileLock)
            {
                logBuffer.AppendLine(message);

                if (logBuffer.Length >= BufferLimit)
                {
                    FlushBuffer();
                }
            }
        }


        private static string FormatLogMessage(LogLevel level, object message)
        {
            return $"{DateTime.UtcNow:O} [{level}] {message}";
        }


        // Log
        public static void Log(object message)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.Log, message));
            UnityLog(message);
        }

        public static void Log(object message, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.Log, message));
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
            WriteToBuffer(FormatLogMessage(LogLevel.WARNING, message));
            UnityLogWarning(message);
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.WARNING, message));
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
            WriteToBuffer(FormatLogMessage(LogLevel.ERROR, message));
            UnityLogError(message);
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.ERROR, message));
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
            WriteToBuffer(FormatLogMessage(LogLevel.EXCEPTION, exception.ToString()));
            UnityLogException(exception);
        }

        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.EXCEPTION, exception.ToString()));
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

        public static void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FlushBuffer();
                }

                disposedValue = true;
            }
        }

        public static void OpenLogFolder()
        {
            if (!Directory.Exists(LogDirectory))
            {
                UnityLogWarning("로그 폴더가 존재하지 않습니다.");
                return;
            }

            Application.OpenURL(LogDirectory);
        }


        public static void OpenCurrentLogFile()
        {
            if (!File.Exists(currentLogFileName))
            {
                UnityLogWarning("현재 로그 파일이 존재하지 않습니다.");
                return;
            }

            Application.OpenURL(currentLogFileName);
        }


    }
}
