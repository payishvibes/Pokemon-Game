using PokemonGame.ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokemonGame.UI
{
    public class StatsScreenMoveIcon : MoveIcon
    {
        [SerializeField] StatsScreenMoveInfo moveInfo;
        [SerializeField] private Transform toolTipAnchor;
        
        [HideInInspector] public Move move;

        public void Clicked()
        {
            moveInfo.UpdateInfo(move, toolTipAnchor);
        }
    }
}