namespace PokemonGame.Battle
{
    using UnityEngine;
    using ScriptableObjects;

    /// <summary>
    /// Contains all of the logic for every AI
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy AI Methods", menuName = "All/New Enemy AI Methods")]
    public class EnemyAIMethods : ScriptableObject
    {
        public void SwitchBattler(AISwitchEventArgs e)
        {
            for (int i = e.currentIndex; i < e.usableParty.Count + e.currentIndex; i++)
            {
                int index = i % e.usableParty.Count;

                if (!e.usableParty[index].isFainted)
                {
                    e.newBattlerIndex = index;
                    return;
                }
            }
        }
        
        public static void WildPokemon(AIMethodEventArgs e)
        {
            int moveToDo = Random.Range(0, e.battlerToUse.moves.Count);
            
            Battle.Singleton.playerTwoMoveToDo = e.battlerToUse.moves[moveToDo];
        }
        
        public void DefaultAI(AIMethodEventArgs e)
        {
            switch (e.battlerToUse.name)
            {
                case "Bulbasaur":
                    break;
            }

            int moveToDo = Random.Range(0, e.battlerToUse.moves.Count);

            Battle.Singleton.playerTwoMoveToDo = e.battlerToUse.moves[moveToDo];
        }
    }   
}