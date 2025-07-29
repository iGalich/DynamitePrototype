using UnityEditor;
using UnityEngine;
using Galich.Other;

namespace Galich.EditorScripts
{
    /// <summary>
    /// Allows you to add '[ReadOnly]' before a variable so that it is shown but not editable in the inspector.
    /// Small but useful script, to make your inspectors look pretty and useful :D
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}