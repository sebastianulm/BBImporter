using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;


[CustomEditor(typeof(BBModelImporter))]
[CanEditMultipleObjects]
public class BBModelImporterEditor : ScriptedImporterEditor
{
    SerializedProperty m_materialTemplate;

    public override void OnEnable()
    {
        base.OnEnable();
        // Once in OnEnable, retrieve the serializedObject property and store it.
        m_materialTemplate = serializedObject.FindProperty("materialTemplate");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        if (GUILayout.Button("Reimport"))
        {
            ApplyAndImport();
        }
        EditorGUILayout.PropertyField(m_materialTemplate);

        // Apply the changes so Undo/Redo is working
        serializedObject.ApplyModifiedProperties();

        // Call ApplyRevertGUI to show Apply and Revert buttons.
        ApplyRevertGUI();
    }
}