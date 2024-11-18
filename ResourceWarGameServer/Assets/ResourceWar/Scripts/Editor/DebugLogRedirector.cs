using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;

public static class DebugLogRedirector
{
    private static readonly Regex StackTraceRegex = new Regex(@"\(at (.+)\)");

    // 에디터 콘솔 로그를 더블 클릭했을 때 호출되는 함수
    private static void OnOpenDebugLog(int instance, int line)
    {
        // 에디터 콘솔 윈도우의 활성화된 텍스트를 찾는다.
        var assembly = Assembly.GetAssembly(typeof(EditorWindow));
        if (assembly == null) return;

        var consoleWindowType = assembly.GetType("UnityEditor.ConsoleWindow");
        if (consoleWindowType == null) return;

        var consoleWindowField = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
        if (consoleWindowField == null) return;

        var consoleWindowInstance = consoleWindowField.GetValue(null);
        if (consoleWindowInstance == null) return;

        if (consoleWindowInstance != (object)EditorWindow.focusedWindow) return;

        // 콘솔 윈도우 인스턴스의 활성화된 텍스트를 찾는다.
        var activeTextField = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
        if (activeTextField == null) return;

        string activeTextValue = activeTextField.GetValue(consoleWindowInstance).ToString();
        if (string.IsNullOrEmpty(activeTextValue)) return;

        // 스택 트레이스에서 "Logger" 관련 라인을 건너뛴다.
        MatchCollection matches = StackTraceRegex.Matches(activeTextValue);

        string targetPath = null;
        foreach (Match match in matches)
        {
            string path = match.Groups[1].Value;
            if (!path.Contains("Logger")) // Logger 관련 라인이 아닌 경우 찾기
            {
                targetPath = path;
                break;
            }
        }

        if (targetPath != null)
        {
            var split = targetPath.Split(':');
            string filePath = split[0];
            int lineNum = Convert.ToInt32(split[1]);

            // 실제 파일 경로와 라인 번호를 이용해 편집기로 연다.
            string dataPath = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("Assets"));
            InternalEditorUtility.OpenFileAtLineExternal(dataPath + filePath, lineNum);
            return;
        }

        return;
    }

    // Unity 에디터가 시작될 때 이벤트 등록
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        var editorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
        var logEntriesType = editorAssembly.GetType("UnityEditor.LogEntries");
        var startListeningMethod = logEntriesType?.GetMethod("startListening", BindingFlags.Static | BindingFlags.NonPublic);
        var stopListeningMethod = logEntriesType?.GetMethod("stopListening", BindingFlags.Static | BindingFlags.NonPublic);

        if (startListeningMethod != null && stopListeningMethod != null)
        {
            stopListeningMethod.Invoke(null, null);
            startListeningMethod.Invoke(null, new object[] { (Action<int, int>)OnOpenDebugLog });
        }
    }
}
