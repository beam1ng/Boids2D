using UnityEditor;
using UnityEngine;

public class CustomEditor : EditorWindow
{
    private string _myString = "Hello World";
    private bool _groupEnabled;
    private bool _myBool = true;
    private float _myFloat = 1.23f;
    
    [MenuItem("Window/CustomEditor")]
    public static void ShowWindow()
    {
        GetWindow(typeof(CustomEditor));
    }
    private void OnGUI()
    {
        GUILayout.Label ("Base Settings", EditorStyles.boldLabel);
        _myString = EditorGUILayout.TextField("Text Field", _myString);
        _groupEnabled = EditorGUILayout.BeginToggleGroup((string) "Optional Settings", (bool) _groupEnabled);
        _myBool = EditorGUILayout.Toggle("Toggle", _myBool);
        _myFloat = EditorGUILayout.Slider("Slider", _myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup ();
    }
    
}
