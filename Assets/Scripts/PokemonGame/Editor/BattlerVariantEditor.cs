using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(BattlerVariant))]
    public class BattlerVariantEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            BattlerVariant template = (BattlerVariant)target;
            
            template.rootTemplate = (BattlerTemplate)EditorGUILayout.ObjectField("Root Template", template.rootTemplate, typeof(BattlerTemplate), true);
            
            template.variantPrefix = EditorGUILayout.TextField("Variant Prefix", template.variantPrefix);
            template.variantSuffix = EditorGUILayout.TextField("Variant Suffix", template.variantSuffix);
            
            EditorGUILayout.Space();
            GUILayout.Label("Override variables for the variant:");
            
            base.OnInspectorGUI();
        }
    }
}