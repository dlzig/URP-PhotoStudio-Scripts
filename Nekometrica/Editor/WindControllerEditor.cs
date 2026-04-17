using UnityEditor;
using UnityEngine;

namespace Nekometrica
{

[CustomEditor(typeof(WindController))]
public class WindControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("windZone"));

        var windZoneProp = serializedObject.FindProperty("windZone");
        if (windZoneProp.objectReferenceValue == null)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("windBaseOrientation"));
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseStrength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("strengthFactor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("arrowLength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("arrowColor"));

        serializedObject.ApplyModifiedProperties();
    }
}

}
