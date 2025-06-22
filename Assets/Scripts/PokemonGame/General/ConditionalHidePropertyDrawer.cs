namespace PokemonGame.General
{
    using UnityEngine;
    using UnityEditor;

    //Original version of the ConditionalHideAttribute created by Brecht Lecluyse (www.brechtos.com)
    //Modified by Techyte
    
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    [CustomPropertyDrawer(typeof(ConditionalHideObjectAttribute))]
    public class ConditionalHidePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property); 
    
            bool wasEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }        
    
            GUI.enabled = wasEnabled;
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);
    
            if (enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                //The property is not being drawn
                //We want to undo the spacing added before and after the property
                return -EditorGUIUtility.standardVerticalSpacing;
                //return 0.0f;
            }

            /*
            //Get the base height when not expanded
            var height = base.GetPropertyHeight(property, label);
    
            // if the property is expanded go through all its children and get their height
            if (property.isExpanded)
            {
                var propEnum = property.GetEnumerator();
                while (propEnum.MoveNext())
                    height += EditorGUI.GetPropertyHeight((SerializedProperty)propEnum.Current, GUIContent.none, true);
            }
            return height;*/
        }
    
        private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
        {
            bool enabled = true;
    
            //Handle primary property
            SerializedProperty sourcePropertyValue = null;
            //Get the full relative property path of the sourcefield so we can have nested hiding.Use old method when dealing with arrays
            if (!property.isArray)
            {
                string propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
                string conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField); //changes the path to the conditionalsource property path
                sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);
    
                //if the find failed->fall back to the old system
                if(sourcePropertyValue==null)
                {
                    //original implementation (doesn't work with nested serializedObjects)
                    sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);
                }
            }
            else
            {
                //original implementation (doesn't work with nested serializedObjects)
                sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);
            }

            if (sourcePropertyValue != null)
            {
                switch (sourcePropertyValue.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        enabled = sourcePropertyValue.boolValue == condHAtt.boolValue;
                        break;
                    case SerializedPropertyType.String:
                        enabled = sourcePropertyValue.stringValue == condHAtt.stringValue;
                        break;
                    case SerializedPropertyType.Integer:
                        enabled = sourcePropertyValue.intValue == condHAtt.intValue;
                        break;
                    case SerializedPropertyType.Enum:
                        enabled = sourcePropertyValue.enumValueIndex == condHAtt.enumValueIndex;
                        break;
                    case SerializedPropertyType.Float:
                        enabled = Mathf.Approximately(sourcePropertyValue.floatValue, condHAtt.floatValue);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        // realistically, this could actually be used for everything i just don't think it would be quite as fast
                        enabled = sourcePropertyValue.objectReferenceValue == (Object)condHAtt.objectValue;
                        break;
                }
            }

            //wrap it all up
            if (condHAtt.Inverse) enabled = !enabled;
    
            return enabled;
        }
    }
}