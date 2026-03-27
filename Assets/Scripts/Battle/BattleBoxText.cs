using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleBoxText : MonoBehaviour
{
    [SerializeField] int lettersPerSecond;
    [SerializeField] Color highlightedColor;

    [SerializeField] TextMeshProUGUI dialogText; //referenc to the text
    [SerializeField] GameObject choiceSelector;
    [SerializeField] GameObject abilitySelector;
    [SerializeField] GameObject abilityDetails;

    [SerializeField] List<TextMeshProUGUI> choiceTexts; //reference
    [SerializeField] List<TextMeshProUGUI> abilityNames; //

    [SerializeField] TextMeshProUGUI apText;
    [SerializeField] TextMeshProUGUI elemTypeText;

    public void DefineDialog(string defDialog)
    {
        dialogText.text = defDialog;
    }

    //This will show the dialogtext letters one by one and not all instant
    public IEnumerator thisFunctionWillTypeDialogText(string dialogInGame)
    {
        dialogText.text = "";
        foreach (var lettersInText in dialogInGame.ToCharArray())
        {
            dialogText.text += lettersInText;
            yield return new WaitForSeconds(1f / lettersPerSecond); //1 second show 30 letters

        }

        yield return new WaitForSeconds(1f);

    }

    public void ActivateDialogText(bool active)
    {
        dialogText.enabled = active;

    }

    public void ActivateChoiceSelector(bool choiceActive)
    {
        choiceSelector.SetActive(choiceActive);
    }

    public void ActivateAbilitySelector(bool abiActive)
    {
        abilitySelector.SetActive(abiActive);
        abilityDetails.SetActive(abiActive);
    }

    public void UpdateChoices(int choiceSelected) 
    {
        for (int i = 0; i < choiceTexts.Count; ++i)
        {
            if (i == choiceSelected)
                choiceTexts[i].color = highlightedColor;
            else
                choiceTexts[i].color = Color.black;
        }
    }

    public void UpdateAbilitySelection(int abilitySelected, Move ability)
    {
        for (int i = 0; i < abilityNames.Count; ++i)
        {
            if (i == abilitySelected)
                abilityNames[i].color = highlightedColor;
            else
                abilityNames[i].color = Color.black;
        }

        apText.text = $"PP {ability.PP}/{ability.Base.Ap}"; 
        elemTypeText.text = ability.Base.Type.ToString();
    }

    public void FunctionToSetabilityNames(List<Move> ability) // function to set ability names
    {
        //loops through ability text

        for (int i = 0; i < abilityNames.Count; ++i)
        {
            if (i < ability.Count)
                abilityNames[i].text = ability[i].Base.Name;
            else
                abilityNames[i].text = "-";
        }
    }

}
