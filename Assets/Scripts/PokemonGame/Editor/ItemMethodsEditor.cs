using PokemonGame.Battle;
using UnityEngine;

namespace PokemonGame.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(ItemMethods))]
    public class ItemMethodsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ItemMethods battlerTemplate = (ItemMethods)target;
            
            EditorUtility.SetDirty(battlerTemplate);
            if (GUILayout.Button("Try fill info"))
            {
                battlerTemplate.CreateItems();
            }
        }
    }
}