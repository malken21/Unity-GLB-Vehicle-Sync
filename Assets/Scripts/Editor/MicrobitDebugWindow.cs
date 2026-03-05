using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity Editor上で micro:bit の各種入力をシミュレートするためのウィンドウ。
/// </summary>
public class MicrobitDebugWindow : EditorWindow
{
    private bool buttonA = false;
    private bool buttonB = false;
    private bool jump = false;
    private float roll = 0f;

    [MenuItem("Window/Microbit Debugger")]
    public static void ShowWindow()
    {
        GetWindow<MicrobitDebugWindow>("Microbit Debugger");
    }

    private void OnGUI()
    {
        GUILayout.Label("Microbit Input Simulator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        if (MicrobitBLEManager.Instance == null)
        {
            EditorGUILayout.HelpBox("MicrobitBLEManager がシーン内に存在しません。再生モード中のみ有効です。", MessageType.Warning);
            return;
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("このツールは再生モード中のみ動作します。", MessageType.Info);
        }

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        // ボタン入力
        EditorGUILayout.BeginHorizontal();
        buttonA = GUILayout.Toggle(buttonA, "Button A", "Button", GUILayout.Height(30));
        buttonB = GUILayout.Toggle(buttonB, "Button B", "Button", GUILayout.Height(30));
        EditorGUILayout.EndHorizontal();

        // ジャンプ入力
        if (GUILayout.Button("Trigger Jump", GUILayout.Height(40)))
        {
            jump = true;
            SendSignal();
            jump = false;
        }

        // 傾き入力 (Roll)
        EditorGUILayout.LabelField($"Roll (Rotation): {roll:F1}");
        roll = EditorGUILayout.Slider(roll, -180f, 180f);

        if (GUILayout.Button("Reset Roll"))
        {
            roll = 0f;
        }

        EditorGUILayout.Space();

        // 状態が変更されたら信号を送信（ジャンプ以外。ジャンプはボタン押下時に即時送信）
        if (GUI.changed)
        {
            SendSignal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actual Connection (Native)", EditorStyles.boldLabel);
        
        // 接続状態の表示
        GUIColorScope(MicrobitBLEManager.Instance.isConnected ? Color.green : Color.gray, () => {
            EditorGUILayout.LabelField("Status:", MicrobitBLEManager.Instance.statusMessage);
        });

        EditorGUILayout.LabelField("Last Received:", MicrobitBLEManager.Instance.lastReceivedData);

        if (GUILayout.Button("Manual Scan / Reconnect"))
        {
            MicrobitBLEManager.Instance.RestartScanning();
        }

        EditorGUI.EndDisabledGroup();
    }

    private void GUIColorScope(Color color, System.Action action)
    {
        Color oldColor = GUI.contentColor;
        GUI.contentColor = color;
        action();
        GUI.contentColor = oldColor;
    }

    private void SendSignal()
    {
        if (MicrobitBLEManager.Instance == null) return;

        int a = buttonA ? 1 : 0;
        int b = buttonB ? 1 : 0;
        int j = jump ? 1 : 0;
        
        // フォーマット: A,B,J,R
        string data = $"{a},{b},{j},{roll:F1}";
        MicrobitBLEManager.Instance.InjectDebugData(data);
    }
}
