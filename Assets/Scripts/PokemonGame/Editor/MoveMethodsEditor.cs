using PokemonGame.Battle;
using UnityEngine;

namespace PokemonGame.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(MovesMethods))]
    public class MoveMethodsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MovesMethods battlerTemplate = (MovesMethods)target;
            
            EditorUtility.SetDirty(battlerTemplate);
            if (GUILayout.Button("Update Moves"))
            {
                battlerTemplate.UpdateMoves();
            }
        }
    }
}