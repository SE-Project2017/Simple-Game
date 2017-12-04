#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Utils
{
    public class OverrideLabel : PropertyAttribute
    {
        private readonly string mLabel;

        public OverrideLabel(string label)
        {
            mLabel = label;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(OverrideLabel))]
        public class CustomDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                label.text = ((OverrideLabel) attribute).mLabel;
                EditorGUI.PropertyField(position, property, label);
            }
        }
#endif
    }
}
