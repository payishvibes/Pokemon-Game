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
            MovesMethods moveMethods = (MovesMethods)target;
            
            if (GUILayout.Button("Update Moves"))
            {
                moveMethods.UpdateMoves();
            }
        }
    }
}