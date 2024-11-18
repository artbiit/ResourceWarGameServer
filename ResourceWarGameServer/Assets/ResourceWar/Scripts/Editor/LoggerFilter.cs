using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
namespace ResourceWar.Server.Editor
{
    [InitializeOnLoad]
    public static class LoggerFilter 
    {
       static LoggerFilter()
        {
            Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
        }

        private static void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            var filteredStackTranc = FilterLoggerStackTrace(stackTrace);

            UpdateConsole(condition, filteredStackTranc,type);
        }

        private static string FilterLoggerStackTrace( string stackTrace)
        {
            var lines = stackTrace.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

            return string.Join("\n", lines.Where(line => !line.Contains("Logger.cs")));
        }


        private static void UpdateConsole(string condition, string stackTrace, LogType logType)
        {
            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var addEntryMethod = logEntriesType?.GetMethod("AddEntry", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Debug.Log(addEntryMethod);
            addEntryMethod?.Invoke(null, new object[] { condition, stackTrace, (int)logType });
            
        }
    }
}
