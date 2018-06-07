using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MixedEnumAttribute))]
public class MixedEnumDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MixedEnumAttribute flagSettings = (MixedEnumAttribute)attribute;
        string propName = flagSettings.enumName;
        if (string.IsNullOrEmpty(propName))
            propName = property.name;
        property.intValue = EditorGUI.MaskField(
            position,
            label,
            property.intValue,
            property.enumNames
        );
    }
}