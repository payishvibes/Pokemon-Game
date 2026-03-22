using PokemonGame.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(order = 2, fileName = "New Battler Variant", menuName = "Pokemon Game/New Battler Variant")]
public class BattlerVariant : BattlerTemplate
{
    [HideInInspector] public BattlerTemplate rootTemplate;
    [HideInInspector] public string variantPrefix;
    [HideInInspector] public string variantSuffix;
}
