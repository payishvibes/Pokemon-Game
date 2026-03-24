using System;
using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(BattlerVariant))]
    public class BattlerVariantEditor : Editor
    {
        SerializedProperty texture;
        SerializedProperty possibleMoves;
        SerializedProperty evolutions;

        private void OnEnable()
        {
            texture = serializedObject.FindProperty("texture");
            possibleMoves = serializedObject.FindProperty("possibleMoves");
            evolutions = serializedObject.FindProperty("evolutions");
        }

        public override void OnInspectorGUI()
        {
            BattlerVariant template = (BattlerVariant)target;
            
            EditorUtility.SetDirty(template);
            template.rootTemplate = (BattlerTemplate)EditorGUILayout.ObjectField("Root Template", template.rootTemplate, typeof(BattlerTemplate), true);
            
            template.variantPrefix = EditorGUILayout.TextField("Variant Prefix", template.variantPrefix);
            template.variantSuffix = EditorGUILayout.TextField("Variant Suffix", template.variantSuffix);
            
            EditorGUILayout.Space();
            GUILayout.Label("Override variables for the variant:", EditorStyles.boldLabel);

            template.overrideName = StringField("Name", template.overrideName, template.name, out template.name);
            
            template.overridePrimaryType = EnumField("Primary Type", template.overridePrimaryType, template.primaryType, out Enum primaryResult);
            
            template.overrideSecondaryType = EnumField("Secondary Type", template.overrideSecondaryType, template.secondaryType, out Enum secondaryResult);
            
            template.overrideTexture = PropertyField("Texture", template.overrideTexture, texture);
            
            template.overrideDexNo = IntField("Dex No", template.overrideDexNo, template.dexNo, out template.dexNo);

            template.overridePossibleMoves = PropertyField("Possible Moves", template.overridePossibleMoves, possibleMoves);
            
            template.overrideEvolutions = PropertyField("Evolutions", template.overrideEvolutions, evolutions);
            
            template.overrideExpGroup = EnumField("Exp Group", template.overrideExpGroup, template.expGroup, out Enum expResult);
            
            template.overrideCatchRate = IntField("Catch Rate", template.overrideCatchRate, template.catchRate, out template.catchRate);
            
            template.overrideBaseFriendship = IntField("Base Friendship", template.overrideBaseFriendship, template.baseFriendship, out template.baseFriendship);
            
            // base stats
            
            template.overrideExpYield = IntField("Exp Yield", template.overrideExpYield, template.expYield, out template.expYield);
            
            // yields
            template.primaryType = (BasicType)primaryResult;
            template.secondaryType = (BasicType)secondaryResult;
            template.expGroup = (ExperienceGroup)expResult;
            
            serializedObject.ApplyModifiedProperties();
        }

        private bool StringField(string labelText, bool enabled, string display, out string output)
        {
            EditorGUILayout.BeginHorizontal();
            bool returnBool = EditorGUILayout.Toggle(enabled, GUILayout.MaxWidth(50));
            if (!returnBool)
            {
                EditorGUI.BeginDisabledGroup(true);
            }
            output = EditorGUILayout.TextField(labelText, display);
            if (!returnBool)
            {
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            return returnBool;
        }
        
        private bool IntField(string labelText, bool enabled, int display, out int output)
        {
            EditorGUILayout.BeginHorizontal();
            bool returnBool = EditorGUILayout.Toggle(enabled, GUILayout.MaxWidth(50));
            if (!returnBool)
            {
                EditorGUI.BeginDisabledGroup(true);
            }
            output = EditorGUILayout.IntField(labelText, display);
            if (!returnBool)
            {
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            return returnBool;
        }
        
        private bool EnumField(string labelText, bool enabled, Enum display, out Enum output)
        {
            EditorGUILayout.BeginHorizontal();
            bool returnBool = EditorGUILayout.Toggle(enabled, GUILayout.MaxWidth(50));
            if (!returnBool)
            {
                EditorGUI.BeginDisabledGroup(true);
            }
            output = EditorGUILayout.EnumFlagsField(labelText, display);
            if (!returnBool)
            {
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            return returnBool;
        }
        
        private bool PropertyField(string labelText, bool enabled, SerializedProperty display)
        {
            EditorGUILayout.BeginHorizontal();
            bool returnBool = EditorGUILayout.Toggle(enabled, GUILayout.MaxWidth(50));
            if (!returnBool)
            {
                EditorGUI.BeginDisabledGroup(true);
            }
            EditorGUILayout.PropertyField(display, new GUIContent(labelText));
            if (!returnBool)
            {
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
            
            return returnBool;
        }
    }
}