using UnityEngine;

namespace PokemonGame.Editor
{
    using UnityEditor;
    using ScriptableObjects;
    
    [CustomEditor(typeof(BattlerTemplate))]
    public class BattlerTemplateEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            BattlerTemplate battlerTemplate = (BattlerTemplate)target;
            
            EditorUtility.SetDirty(battlerTemplate);
            if (GUILayout.Button("Try fill info"))
            {
                battlerTemplate.TryFillInfo();
            }
        }
    }
}