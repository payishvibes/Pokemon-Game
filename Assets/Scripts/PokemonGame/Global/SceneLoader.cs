using PokemonGame.Game;
using PokemonGame.Networking;

namespace PokemonGame.Global
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    
    /// <summary>
    /// A custom scene loading class for loading scenes with data
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// The list of variables that you loaded the scene with
        /// </summary>
        private static Dictionary<string, object> _vars = new Dictionary<string, object>();
    
        public static string sceneLoadedFrom;

        private static int sceneIndex;
        private static string sceneName;

        /// <summary>
        /// Loads the scene with the same index as sceneIndex
        /// </summary>
        /// <param name="sceneToLoadIndex">The index of the scene to load</param>
        /// <param name="newVars">Variables to pass along to the next scene</param>
        public static void LoadScene(int sceneToLoadIndex, Dictionary<string, object> newVars)
        {
            ClearLoader();
            _vars = newVars;
            sceneLoadedFrom = SceneManager.GetActiveScene().name;
            if (!BattleNetworkManager.Instance)
                OptionsMenu.instance.CloseMenu();
            SceneManager.LoadScene(sceneToLoadIndex);   
        }

        /// <summary>
        /// Loads the scene with the same index as sceneIndex
        /// </summary>
        /// <param name="sceneToLoadIndex">The index of the scene to load</param>
        public static void LoadScene(int sceneToLoadIndex)
        {
            ClearLoader();
            sceneLoadedFrom = SceneManager.GetActiveScene().name;
            if (!BattleNetworkManager.Instance)
                OptionsMenu.instance.CloseMenu();
            SceneManager.LoadScene(sceneToLoadIndex); 
        }

        /// <summary>
        /// Loads the scene with the same name as sceneName
        /// </summary>
        /// <param name="sceneToLoadName">The name of the scene to load</param>
        public static void LoadScene(string sceneToLoadName)
        {
            ClearLoader();
            sceneLoadedFrom = SceneManager.GetActiveScene().name;
            if (!BattleNetworkManager.Instance)
                OptionsMenu.instance.CloseMenu();
            SceneManager.LoadScene(sceneToLoadName);   
        }
    
        /// <summary>
        /// Loads the scene with the same name as sceneName
        /// </summary>
        /// <param name="sceneToLoadName">The name of the scene to load</param>
        /// <param name="newVars">Variables to pass along to the next scene</param>
        public static void LoadScene(string sceneToLoadName, Dictionary<string, object> newVars)
        {
            ClearLoader();
            _vars = newVars;
            sceneLoadedFrom = SceneManager.GetActiveScene().name;
            if (!BattleNetworkManager.Instance)
                OptionsMenu.instance.CloseMenu();
            SceneManager.LoadScene(sceneToLoadName);
        }
    
        /// <summary>
        /// Clears the scene loader arguments
        /// </summary>
        private static void ClearLoader()
        {
            if(_vars != null)
            {
                _vars.Clear();
            }
            sceneLoadedFrom = null;
        }
    
        /// <summary>
        /// Gets a variable from the carried over variables
        /// </summary>
        /// <param name="variableName">The name of the variable to get</param>
        /// <typeparam name="T">The type of variable to get</typeparam>
        /// <returns></returns>
        public static T GetVariable<T>(string variableName)
        {
            if (_vars.TryGetValue(variableName, out object var))
            {
                return (T)var;
            }
            
            return default;
        }
    }
}