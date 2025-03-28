using PokemonGame.Global;

namespace PokemonGame.Game
{
    using UnityEngine;

    public class Boot : MonoBehaviour
    {
        [SerializeField] private GameObject[] DontDestroyObjects;
        
        private void Start()
        {
            // Application.targetFrameRate = 75;
            Bag.Add(Registry.GetItem("Potion"), 2);
            Bag.Add(Registry.GetItem("Revive"), 2);
            Bag.Add(Registry.GetItem("Max Revive"), 2);
            Bag.Add(Registry.GetItem("Poke Ball"),50);
            Bag.GainMoney(1);
            
            foreach (var objectToNotDestroy in DontDestroyObjects)
            {
                DontDestroyOnLoad(objectToNotDestroy);
            }
            SceneLoader.LoadScene(1);
        }
    }
}