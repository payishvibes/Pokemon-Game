using System;
using PokemonGame.Game;
using PokemonGame.Game.Party;
using PokemonGame.General;
using PokemonGame.ScriptableObjects;
using PokemonGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Type = PokemonGame.ScriptableObjects.Type;

public class PartyMenu : MonoBehaviour
{
    [SerializeField] private MenuBattlerDisplay displayPrefab;
    [SerializeField] private Transform[] partyDisplayPositions;
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject detailsScreen;
    [SerializeField] private GameObject detailsBackButton;
    [SerializeField] private UIPolygon polygon;
    [SerializeField] private TextMeshProUGUI healthStat;
    [SerializeField] private TextMeshProUGUI attackStat;
    [SerializeField] private TextMeshProUGUI defenseStat;
    [SerializeField] private TextMeshProUGUI specialAttackStat;
    [SerializeField] private TextMeshProUGUI specialDefenseStat;
    [SerializeField] private TextMeshProUGUI speedStat;
    [SerializeField] private GameObject type1;
    [SerializeField] private GameObject type2;
    [SerializeField] private TextMeshProUGUI battlerName;
    [SerializeField] private TextMeshProUGUI battlerLevel;
    [SerializeField] private GameObject[] moves;

    private void Start()
    {
        Party currentPlayerParty = PartyManager.GetParty();
        
        if(currentPlayerParty != null)
        {
            for (int i = 0; i < currentPlayerParty.Count; i++)
            {
                Battler currentBattler = currentPlayerParty[i];
                MenuBattlerDisplay display = Instantiate(displayPrefab, partyDisplayPositions[i]);
                display.Init(currentBattler.name, currentBattler.currentHealth, currentBattler.stats.maxHealth,
                    currentBattler.statusEffect, currentBattler.exp,
                    ExperienceCalculator.RequiredForNextLevel(currentBattler),
                    currentBattler.GetSpriteFront());
                int index = i;
                display.GetComponentInChildren<Button>().onClick.AddListener((() =>
                {
                    OpenDetails(index);
                }));
            }
        }
        
        BackToMainScreen();
    }

    private void OpenDetails(int i)
    {
        mainScreen.SetActive(false);
        detailsScreen.SetActive(true);

        Battler currentBattler = PartyManager.GetParty()[i];

        polygon.VerticesDistances[0] = Mathf.Sqrt(currentBattler.stats.maxHealth / 260f);
        healthStat.text = $"{currentBattler.currentHealth}/{currentBattler.stats.maxHealth}";

        polygon.VerticesDistances[1] = Mathf.Sqrt(currentBattler.stats.attack / 260f);
        attackStat.text = currentBattler.stats.attack.ToString();

        polygon.VerticesDistances[2] = Mathf.Sqrt(currentBattler.stats.defense / 260f);
        defenseStat.text = currentBattler.stats.defense.ToString();

        polygon.VerticesDistances[3] = Mathf.Sqrt(currentBattler.stats.specialAttack / 260f);
        specialAttackStat.text = currentBattler.stats.specialAttack.ToString();

        polygon.VerticesDistances[4] = Mathf.Sqrt(currentBattler.stats.specialDefense / 260f);
        specialDefenseStat.text = currentBattler.stats.specialDefense.ToString();

        polygon.VerticesDistances[5] = Mathf.Sqrt(currentBattler.stats.speed / 260f);
        speedStat.text = currentBattler.stats.speed.ToString();

        type1.GetComponentInChildren<Image>().color = Type.FromBasic(currentBattler.source.GetPrimaryType()).color;
        type1.GetComponentInChildren<TextMeshProUGUI>().text = currentBattler.source.GetPrimaryType().ToString();
        if (currentBattler.source.GetSecondaryType() != BasicType.None)
        {
            type2.SetActive(true);
            type2.GetComponentInChildren<Image>().color = Type.FromBasic(currentBattler.source.GetSecondaryType()).color;
            type2.GetComponentInChildren<TextMeshProUGUI>().text = currentBattler.source.GetSecondaryType().ToString();
        }
        else
        {
            type2.SetActive(false);
        }

        battlerName.text = currentBattler.name;
        battlerName.color = currentBattler.currentHealth == 0 ? Color.red : Color.black;
        battlerLevel.text = $"Lv. {currentBattler.level}";

        for (int j = 0; j < moves.Length; j++)
        {
            if (currentBattler.moves.Count <= j)
            {
                moves[j].SetActive(false);
                continue;
            }
            if (currentBattler.moves[j] == null)
            {
                moves[j].SetActive(false);
                continue;
            }
            moves[j].SetActive(true);
            TextMeshProUGUI[] texts = moves[j].GetComponentsInChildren<TextMeshProUGUI>();
            texts[0].text = currentBattler.moves[j].name;
            texts[1].text = $"{currentBattler.movePpInfos[j].CurrentPP}/{currentBattler.movePpInfos[j].MaxPP}";
            moves[j].GetComponent<Image>().color = currentBattler.moves[j].type.color;
            moves[j].GetComponentsInChildren<Image>()[1].sprite = currentBattler.moves[j].type.sprite;
            moves[j].GetComponent<StatsScreenMoveIcon>().move = currentBattler.moves[j];
        }

        if (currentBattler.moves.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(moves[0]);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(detailsBackButton);
        }
    }
    
    public void BackToMainScreen()
    {
        mainScreen.SetActive(true);
        detailsScreen.SetActive(false);
        
        EventSystem.current.SetSelectedGameObject(partyDisplayPositions[0].GetChild(0).gameObject);
    }

    private void Back()
    {
        if (detailsScreen.activeSelf)
        {
            BackToMainScreen();
        }
        else
        {
            OptionsMenu.instance.CloseCurrentMenu();
        }
    }

    private void OnEnable()
    {
        InputSystem.actions.FindAction("Escape").performed += OnEscapePressed;
    }

    private void OnEscapePressed(InputAction.CallbackContext obj)
    {
        Back();
    }

    private void OnDisable()
    {
        InputSystem.actions.FindAction("Escape").performed -= OnEscapePressed;
    }
}
