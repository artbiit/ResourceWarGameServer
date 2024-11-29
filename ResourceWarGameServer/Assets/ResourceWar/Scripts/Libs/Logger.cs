using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.IO.Compression;

namespace ResourceWar.Server.Lib
{
    public static class Logger
    {

        private static readonly string LogDirectory = Path.Combine(Application.persistentDataPath, "Logs");
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
            ArchiveOldLogs();
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
                if (currentLogTime.Date != now.Date)
                {
                    ArchiveOldLogs();
                }
                lock (FileLock)
                {
                    currentLogTime = now;
                    currentLogFileName = GetLogFileName(now);
                }
            }
        }

        private static void FlushBuffer()
        {
#if UNITY_EDITOR
            return;
#endif

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
#if UNITY_EDITOR
            return;
#endif
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
            WrappedLog(message);

        }

        public static void Log(object message, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.Log, message));
            WrappedLog(message, context);
        }

        private static void WrappedLog(object message, UnityEngine.Object context = null)
        {
#if UNITY_EDITOR
            if (context == null)
            {
                UnityEngine.Debug.Log(message);
            }
            else
            {
                UnityEngine.Debug.Log(message, context);
            }
#else
    // 일반 로그를 기본 색상으로 출력
    Console.ForegroundColor = ConsoleColor.White; // 가독성을 위해 명시적 설정
    if (context == null)
    {
        Console.WriteLine($"[{LogType.Log}] {message}");
    }
    else
    {
        Console.WriteLine($"[{LogType.Log}] {message} (Context: {context?.name ?? "Unknown"})");
    }
    Console.ResetColor(); // 다른 로그에 영향을 주지 않도록 색상 복구
#endif

        }

        // LogWarning
        public static void LogWarning(object message)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.WARNING, message));
            WrappedLogWarning(message);
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.WARNING, message));
            WrappedLogWarning(message, context);
        }

        private static void WrappedLogWarning(object message, UnityEngine.Object context = null)
        {
#if UNITY_EDITOR
            if (context == null)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                UnityEngine.Debug.LogWarning(message, context);
            }
#else
    Console.ForegroundColor = ConsoleColor.Yellow;
    if (context == null)
    {
        Console.WriteLine($"[{LogType.Warning}] {message}");
    }
    else
    {
        Console.WriteLine($"[{LogType.Warning}] {message} (Context: {context?.name ?? "Unknown"})");
    }
    Console.ResetColor(); 
#endif

        }

        // LogError
        public static void LogError(object message)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.ERROR, message));
            WrappedLogError(message);
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
            WriteToBuffer(FormatLogMessage(LogLevel.ERROR, message));
            WrappedLogError(message, context);
        }

        private static void WrappedLogError(object message, UnityEngine.Object context = null)
        {
#if UNITY_EDITOR
            if (context == null)
            {
                UnityEngine.Debug.LogError(message);
            }
            else
            {
                UnityEngine.Debug.LogError(message, context);
            }
#else
             Console.ForegroundColor = ConsoleColor.Red;
             Console.WriteLine($"[{LogType.Error}]{message}");
             Console.ResetColor();
#endif


        }

        // LogAssertion
        public static void LogAssertion(object message)
        {
            WrappedLogAssertion(message);
        }

        public static void LogAssertion(object message, UnityEngine.Object context)
        {
            WrappedLogAssertion(message, context);
        }
        private static void WrappedLogAssertion(object message, UnityEngine.Object context = null)
        {

#if UNITY_EDITOR
            if (context == null)
            {
                UnityEngine.Debug.LogAssertion(message);
            }
            else
            {
                UnityEngine.Debug.LogAssertion(message, context);
            }

#else
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{LogType.Assert}] {message}");
            Console.ResetColor();
#endif
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
        private static void UnityLogException(Exception exception, UnityEngine.Object context = null)
        {
#if UNITY_EDITOR
            if (context == null)
            {
                UnityEngine.Debug.LogException(exception);
            }
            else
            {
                UnityEngine.Debug.LogException(exception, context);
            }
#else

    Console.ForegroundColor = ConsoleColor.Red; 
    Console.WriteLine($"[{LogType.Exception}] {exception.Message}");
    Console.WriteLine($"[{LogType.Exception}] StackTrace: {exception.StackTrace}");
    if (context != null)
    {
        Console.WriteLine($"[CONTEXT] {context?.name ?? "Unknown"}");
    }
    Console.ResetColor(); 
#endif

        }

        public static void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ArchiveOldLogs();
                    FlushBuffer();
                }

                disposedValue = true;
            }
        }

        public static void OpenLogFolder()
        {
            if (!Directory.Exists(LogDirectory))
            {
                WrappedLogWarning("로그 폴더가 존재하지 않습니다.");
                return;
            }

            Application.OpenURL(LogDirectory);
        }


        public static void OpenCurrentLogFile()
        {
            if (!File.Exists(currentLogFileName))
            {
                WrappedLogWarning("현재 로그 파일이 존재하지 않습니다.");
                return;
            }

            Application.OpenURL(currentLogFileName);
        }


        private static void ArchiveOldLogs()
        {
            // 하루 전 날짜
            DateTime yesterday = DateTime.UtcNow.AddDays(-1);
            string yesterdayDirectory = Path.Combine(LogDirectory, yesterday.ToString("yyyy-MM-dd"));
            string zipFileName = Path.Combine(LogDirectory, $"{yesterday:yyyy-MM-dd}.zip");

            // 하루 전 로그 파일을 모두 찾음
            var oldLogFiles = Directory.GetFiles(LogDirectory, $"{yesterday:yyyy-MM-dd}_*.log");

            if (oldLogFiles.Length > 0)
            {
                // 임시 디렉토리 생성
                Directory.CreateDirectory(yesterdayDirectory);

                // 파일 이동
                foreach (var file in oldLogFiles)
                {
                    string destination = Path.Combine(yesterdayDirectory, Path.GetFileName(file));
                    File.Move(file, destination);
                }

                // 압축
                ZipFile.CreateFromDirectory(yesterdayDirectory, zipFileName);

                // 임시 디렉토리 삭제
                Directory.Delete(yesterdayDirectory, true);
            }
        }


    }
}
