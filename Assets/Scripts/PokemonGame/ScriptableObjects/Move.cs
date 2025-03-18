using PokemonGame.Battle;

namespace PokemonGame.ScriptableObjects
{
    using System;
    using General;
    using UnityEngine;
    using UnityEngine.Events;
    
    /// <summary>
    /// A move that is used in battles to fight
    /// </summary>
    [CreateAssetMenu(order = 2, fileName = "New Move", menuName = "Pokemon Game/New Move")]
    public class Move : ScriptableObject
    {
        public new string name;
        public Type type;
        public int damage;
        public int basePP;
        public float accuracy;
        public MoveCategory category;
    
        public UnityEvent<MoveMethodEventArgs> MoveMethodEvent;
    
        /// <summary>
        /// Calls the associated function in StatusMoveMethods.cs
        /// </summary>
        /// <param name="e">The MoveMethodArgs that can be used to store additional information to be parsed onto the method</param>
        public void MoveMethod(MoveMethodEventArgs e)
        {
            int PP = e.attacker.movePpInfos[e.moveIndex].CurrentPP;

            if (PP > 0)
            {
                MoveMethodEvent?.Invoke(e);
                if (MoveMethodEvent == null)
                {
                    MovesMethods.GetMoveMethods().DefaultMoveMethod(e);
                }
                
                e.attacker.movePpInfos[e.moveIndex].MoveWasUsed();
            }
            else
            {
                Debug.Log("Move out of PP");
            }
        }
    }
    
    /// <summary>
    /// The category of move, Physical, Special or Status
    /// </summary>
    public enum MoveCategory
    {
        Physical,
        Special,
        Status
    }
    
    /// <summary>
    /// Arguments that can be given to a MoveMethod to give it additional information
    /// </summary>
    public class MoveMethodEventArgs : EventArgs
    {
        public Battler target;
        public Battler attacker;
        public int moveIndex;
        public Move move;
        public ExternalBattleData battleData;
        public int damageDealt = 0;

        public MoveMethodEventArgs(Battler attacker, Battler target, int moveIndex, Move move, ExternalBattleData battleData)
        {
            this.target = target;
            this.attacker = attacker;
            this.moveIndex = moveIndex;
            this.move = move;
            this.battleData = battleData;
        }
    }

    [System.Serializable]
    public class MovePPData
    {
        public int MaxPP;
        public int CurrentPP;

        public MovePPData(int maxPP, int currentPP)
        {
            MaxPP = maxPP;
            CurrentPP = currentPP;
        }

        public void MoveWasUsed()
        {
            CurrentPP--;
        }

        public void Restore()
        {
            CurrentPP = MaxPP;
        }
    }
}