namespace PokemonGame.General
{
    using UnityEngine;
    using System;

    //Original version of the ConditionalHideAttribute created by Brecht Lecluyse (www.brechtos.com)
    //Modified by Techyte
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
        AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        public string ConditionalSourceField = "";
        public bool Inverse = false;
        public bool UseOrLogic = false;
        public bool enumCheck = false;
        public bool intCheck = false;
        public int enumCheckIndex = 0;
        public bool checkEmptyString;
    
        public bool InverseCondition1 = false;
    
    
    	// Use this for initialization
        public ConditionalHideAttribute(string conditionalSourceField)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.Inverse = false;
        }

        public ConditionalHideAttribute(string conditionalSourceField, int enumCheckIndex)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.enumCheck = true;
            this.enumCheckIndex = enumCheckIndex;
            this.Inverse = false;
        }
    
        public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector, bool inverse)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.Inverse = inverse;
        }

        public ConditionalHideAttribute(string conditionalSourceField, int checkIndex, bool inverse)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.enumCheckIndex = checkIndex;
            this.Inverse = inverse;
            intCheck = true;
        }

        public ConditionalHideAttribute()
        {
            
        }
    
        public ConditionalHideAttribute(bool hideInInspector = false)
        {
            this.ConditionalSourceField = "";
            this.Inverse = false;
        }
    }
}