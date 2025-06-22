using UnityEditor;

namespace PokemonGame.General
{
    using UnityEngine;
    using System;

    //Original version of the ConditionalHideAttribute created by Brecht Lecluyse (www.brechtos.com)
    //Modified by Techyte

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                    AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideObjectAttribute : ConditionalHideAttribute
    {
        public ConditionalHideObjectAttribute(string conditionalSourceField, object value, bool inverse = false)
        {
            ConditionalSourceField = conditionalSourceField;
            objectValue = value;
            Inverse = inverse;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
        AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        public string ConditionalSourceField = "";

        public bool boolValue;
        public string stringValue;
        public int intValue;
        public int enumValueIndex;
        public float floatValue;
        public object objectValue;
        
        public bool Inverse = false;

        public ConditionalHideAttribute(string conditionalSourceField, bool value, bool inverse = false)
        {
            ConditionalSourceField = conditionalSourceField;
            boolValue = value;
            Inverse = inverse;
        }

        public ConditionalHideAttribute(string conditionalSourceField, string value, bool inverse = false)
        {
            ConditionalSourceField = conditionalSourceField;
            stringValue = value;
            Inverse = inverse;
        }

        public ConditionalHideAttribute(string conditionalSourceField, int value, bool inverse = false)
        {
            ConditionalSourceField = conditionalSourceField;
            intValue = value;
            Inverse = inverse;
        }

        public ConditionalHideAttribute(string conditionalSourceField, Enum value, bool inverse = false)
        {
            ConditionalSourceField = conditionalSourceField;
            enumValueIndex = (int)(object)value;
            Inverse = inverse;
        }

        public ConditionalHideAttribute(string conditionalSourceField, float value, bool inverse = false)
        {
            ConditionalSourceField = conditionalSourceField;
            floatValue = value;
            Inverse = inverse;
        }

        public ConditionalHideAttribute()
        {
            
        }
    }
}