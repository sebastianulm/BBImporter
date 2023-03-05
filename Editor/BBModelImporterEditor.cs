using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BBImporter
{
    [CustomEditor(typeof(BBModelImporter))]
    [CanEditMultipleObjects]
    public class BBModelImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty m_materialTemplate;
        private SerializedProperty m_importMode;
        private SerializedProperty m_filterHidden;
        private SerializedProperty m_ignoreName;
        private SerializedProperty m_addAnimation;

        public override void OnEnable()
        {
            base.OnEnable();
            // Once in OnEnable, retrieve the serializedObject property and store it.
            m_materialTemplate = serializedObject.FindProperty("materialTemplate");
            m_importMode = serializedObject.FindProperty("importMode");
            m_filterHidden = serializedObject.FindProperty("filterHidden");
            m_ignoreName = serializedObject.FindProperty("ignoreName");
            m_addAnimation = serializedObject.FindProperty("addAnimation");
        }
    
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
        
            EditorGUILayout.PropertyField(m_materialTemplate);
            EditorGUILayout.PropertyField(m_importMode);
            EditorGUILayout.PropertyField(m_filterHidden);
            EditorGUILayout.PropertyField(m_ignoreName);
            EditorGUILayout.PropertyField(m_addAnimation);
            
            // Apply the changes so Undo/Redo is working
            serializedObject.ApplyModifiedProperties();
        
            if (GUILayout.Button("Reimport"))
            {
                ApplyAndImport();
            }

            // Call ApplyRevertGUI to show Apply and Revert buttons.
            ApplyRevertGUI();
        }
    }
}