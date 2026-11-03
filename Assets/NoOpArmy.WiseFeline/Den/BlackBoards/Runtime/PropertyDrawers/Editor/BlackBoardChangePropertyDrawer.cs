using UnityEditor;
using UnityEngine;
using NoOpArmy.WiseFeline.BlackBoards;

[CustomPropertyDrawer(typeof(BlackBoardChange))]
public class BlackBoardChangePropertyDrawer : PropertyDrawer
{
    // The height of each property field
    private const float fieldHeight = 18f;

    // The height of the space between fields
    private const float spaceHeight = 2f;

    // The label for the key type
    private static readonly GUIContent keyTypeLabel = new GUIContent("Key Type");

    // The label for the key name
    private static readonly GUIContent keyNameLabel = new GUIContent("Key Name");

    // The label for the add instead of set toggle
    private static readonly GUIContent addInsteadOfSetLabel = new GUIContent("Add Instead Of Set");
    private static readonly GUIContent multiplyDTLabel = new GUIContent("Multiply by deltaTime for adds");

    // The labels for the different value types
    private static readonly GUIContent vectorValueLabel = new GUIContent("Vector3 Value");
    private static readonly GUIContent intValueLabel = new GUIContent("Int Value");
    private static readonly GUIContent minIntValueLabel = new GUIContent("Min Int Value");
    private static readonly GUIContent maxIntValueLabel = new GUIContent("Max Int Value");
    private static readonly GUIContent floatValueLabel = new GUIContent("Float Value");
    private static readonly GUIContent minFloatValueLabel = new GUIContent("Min Float Value");
    private static readonly GUIContent maxFloatValueLabel = new GUIContent("Max Float Value");
    private static readonly GUIContent boolValueLabel = new GUIContent("Bool Value");
    private static readonly GUIContent stringValueLabel = new GUIContent("string Value");
    private static readonly GUIContent gameObjectValueLabel = new GUIContent("GameObject Value");
    private static readonly GUIContent unityObjectValueLabel = new GUIContent("UnityObject Value");
    private static readonly GUIContent objectValueLabel = new GUIContent("Object Value");

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Draw the label for the whole property
        EditorGUI.PrefixLabel(position, label);

        // Get the indent level of the property
        int indentLevel = EditorGUI.indentLevel;

        // Increase the indent level to make the sub-properties look nested
        EditorGUI.indentLevel++;

        // Get the rect for the key type field
        Rect keyTypeRect = new Rect(position.x, position.y + fieldHeight + spaceHeight, position.width, fieldHeight);

        // Get the property for the key type
        SerializedProperty keyTypeProperty = property.FindPropertyRelative("key.keyType");

        // Draw the field for the key type with a label
        EditorGUI.PropertyField(keyTypeRect, keyTypeProperty, keyTypeLabel);

        // Get the rect for the key name field
        Rect keyNameRect = new Rect(position.x, keyTypeRect.y + fieldHeight + spaceHeight, position.width, fieldHeight);

        // Get the property for the key name
        SerializedProperty keyNameProperty = property.FindPropertyRelative("key.keyName");

        // Draw the field for the key name with a label
        EditorGUI.PropertyField(keyNameRect, keyNameProperty, keyNameLabel);

        // Set-only fields always replace the value, so the add and deltaTime toggles would do nothing there
        bool setOnly = IsSetOnly();

        // The last row drawn so far, the value fields go below it
        Rect multiplyDTRect = keyNameRect;

        if (!setOnly)
        {
            // Get the rect for the add instead of set toggle
            Rect addInsteadOfSetRect = new Rect(position.x, keyNameRect.y + fieldHeight + spaceHeight, position.width, fieldHeight);

            // Get the property for the add instead of set toggle
            SerializedProperty addInsteadOfSetProperty = property.FindPropertyRelative("addInsteadOfSet");

            // Draw the toggle for the add instead of set with a label
            EditorGUI.PropertyField(addInsteadOfSetRect, addInsteadOfSetProperty, addInsteadOfSetLabel);

            // Get the rect for the multiply by dt toggle
            multiplyDTRect = new Rect(position.x, addInsteadOfSetRect.y + fieldHeight + spaceHeight, position.width, fieldHeight);

            // Get the property for the multiply deltatime toggle
            SerializedProperty multiplyDTProperty = property.FindPropertyRelative("multiplyByDeltaTimeForAdds");

            // Draw the toggle for the multiply by dt with a label
            EditorGUI.PropertyField(multiplyDTRect, multiplyDTProperty, multiplyDTLabel);
        }

        // Get the rect for the value field based on the selected key type
        Rect valueRect = new Rect(position.x, multiplyDTRect.y + fieldHeight + spaceHeight, position.width, fieldHeight);

        // Get the value type from the key type enum
        KeyDefinition.KeyType valueType = (KeyDefinition.KeyType)keyTypeProperty.enumValueIndex;

        // Draw the appropriate value field based on the value type with a label
        switch (valueType)
        {
            case KeyDefinition.KeyType.Vector3:
                //valueRect = new Rect(position.x, addInsteadOfSetRect.y + fieldHeight + spaceHeight, position.width, fieldHeight * 2);
                SerializedProperty vectorValueProperty = property.FindPropertyRelative("vectorValue");
                EditorGUI.PropertyField(valueRect, vectorValueProperty, vectorValueLabel);
                break;
            case KeyDefinition.KeyType.Int:
                SerializedProperty intValueProperty = property.FindPropertyRelative("intValue");
                EditorGUI.PropertyField(valueRect, intValueProperty, intValueLabel);
                if (setOnly)
                    break;

                valueRect = new Rect(position.x, multiplyDTRect.y + (2 * fieldHeight) + (2 * spaceHeight), position.width, fieldHeight);
                SerializedProperty minIntValueProperty = property.FindPropertyRelative("minIntValue");
                EditorGUI.PropertyField(valueRect, minIntValueProperty, minIntValueLabel);

                valueRect = new Rect(position.x, multiplyDTRect.y + (3 * fieldHeight) + (3 * spaceHeight), position.width, fieldHeight);
                SerializedProperty maxIntValueProperty = property.FindPropertyRelative("maxIntValue");
                EditorGUI.PropertyField(valueRect, maxIntValueProperty, maxIntValueLabel);
                break;
            case KeyDefinition.KeyType.Float:
                SerializedProperty floatValueProperty = property.FindPropertyRelative("floatValue");
                EditorGUI.PropertyField(valueRect, floatValueProperty, floatValueLabel);
                if (setOnly)
                    break;

                valueRect = new Rect(position.x, multiplyDTRect.y + (2 * fieldHeight) + (2 * spaceHeight), position.width, fieldHeight);
                SerializedProperty minFloatValueProperty = property.FindPropertyRelative("minFloatValue");
                EditorGUI.PropertyField(valueRect, minFloatValueProperty, minFloatValueLabel);

                valueRect = new Rect(position.x, multiplyDTRect.y + (3 * fieldHeight) + (3 * spaceHeight), position.width, fieldHeight);
                SerializedProperty maxFloatValueProperty = property.FindPropertyRelative("maxFloatValue");
                EditorGUI.PropertyField(valueRect, maxFloatValueProperty, maxFloatValueLabel);

                break;
            case KeyDefinition.KeyType.Bool:
                SerializedProperty boolValueProperty = property.FindPropertyRelative("boolValue");
                EditorGUI.PropertyField(valueRect, boolValueProperty, boolValueLabel);
                break;
            case KeyDefinition.KeyType.String:
                SerializedProperty stringValueProperty = property.FindPropertyRelative("stringValue");
                EditorGUI.PropertyField(valueRect, stringValueProperty, stringValueLabel);
                break;
            case KeyDefinition.KeyType.GameObject:
                SerializedProperty gameObjectValueProperty = property.FindPropertyRelative("gameObjectValue");
                EditorGUI.PropertyField(valueRect, gameObjectValueProperty, gameObjectValueLabel);
                break;
            case KeyDefinition.KeyType.UnityObject:
                SerializedProperty unityObjectValueProperty = property.FindPropertyRelative("unityObjectValue");
                EditorGUI.PropertyField(valueRect, unityObjectValueProperty, unityObjectValueLabel);
                break;
            case KeyDefinition.KeyType.Object:
                //This type cannot be shown in the inspector
                break;
            default:
                break;
        }

        // Restore the indent level to the original value
        EditorGUI.indentLevel = indentLevel;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Get the height of the label for the whole property
        float labelHeight = base.GetPropertyHeight(property, label);

        // Get the height of the key type field
        float keyTypeHeight = fieldHeight + spaceHeight;

        // Get the height of the key name field
        float keyNameHeight = fieldHeight + spaceHeight;

        bool setOnly = IsSetOnly();

        // Get the height of the add instead of set toggle
        float addInsteadOfSetHeight = setOnly ? 0 : fieldHeight + spaceHeight;

        // Get the height of the multiply by DT toggle
        float multiplyDtHeight = setOnly ? 0 : fieldHeight + spaceHeight;

        // Get the height of the value field
        float valueHeight = fieldHeight + spaceHeight;

        // Get the property for the key type
        SerializedProperty keyTypeProperty = property.FindPropertyRelative("key.keyType");

        // Get the value type from the key type enum
        KeyDefinition.KeyType valueType = (KeyDefinition.KeyType)keyTypeProperty.enumValueIndex;

        float extraHeight = 0;
        // Draw the appropriate value field based on the value type with a label
        switch (valueType)
        {
            case KeyDefinition.KeyType.Vector3:
                extraHeight = 20;
                break;
            case KeyDefinition.KeyType.Int:
            case KeyDefinition.KeyType.Float:
                // The min and max rows, which set-only fields do not show
                extraHeight = setOnly ? 0 : 40;
                break;
            case KeyDefinition.KeyType.Bool:
                break;
            case KeyDefinition.KeyType.String:
                break;
            case KeyDefinition.KeyType.GameObject:
                break;
            case KeyDefinition.KeyType.UnityObject:
                break;
            case KeyDefinition.KeyType.Object:
                break;
            default:
                break;
        }

        // Return the total height of the property drawer
        return labelHeight + keyTypeHeight + keyNameHeight + addInsteadOfSetHeight + multiplyDtHeight + valueHeight + extraHeight;
    }

    // True when the field being drawn carries BlackBoardSetOnly, like the starting values of BlackboardInitialValues
    private bool IsSetOnly()
    {
        return fieldInfo != null && fieldInfo.IsDefined(typeof(BlackBoardSetOnlyAttribute), true);
    }
}