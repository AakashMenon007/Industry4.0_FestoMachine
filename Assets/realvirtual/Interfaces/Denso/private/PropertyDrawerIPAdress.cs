
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(realvirtual.IPAddress))]
public class IPAddressDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var ipAddress1Rect = new Rect(position.x, position.y, 50, position.height);
            var label1Rect = new Rect(position.x + 52, position.y, 5, position.height);
            var ipAddress2Rect = new Rect(position.x + 60, position.y, 50, position.height);
            var label2Rect = new Rect(position.x + 112, position.y, 5, position.height);
            var ipAddress3Rect = new Rect(position.x + 120, position.y, 50, position.height);
            var label3Rect = new Rect(position.x + 172, position.y, 5, position.height);
            var ipAddress4Rect = new Rect(position.x + 180, position.y, 50, position.height);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(ipAddress1Rect, property.FindPropertyRelative("Address1"), GUIContent.none);
            EditorGUI.LabelField(label1Rect, ".");
            EditorGUI.PropertyField(ipAddress2Rect, property.FindPropertyRelative("Address2"), GUIContent.none);
            EditorGUI.LabelField(label2Rect, ".");
            EditorGUI.PropertyField(ipAddress3Rect, property.FindPropertyRelative("Address3"), GUIContent.none);
            EditorGUI.LabelField(label3Rect, ".");
            EditorGUI.PropertyField(ipAddress4Rect, property.FindPropertyRelative("Address4"), GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
#endif

    