using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;


public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, BattleOver}

public enum BattleAction { Move, SwitchElement, UseItem, Flee }

//This script controls the entire battle
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleBoxText dialogBox;
    [SerializeField] TeamDisplayer partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] public AudioSource clickSoundEffect;
    [SerializeField] public AudioSource confirmSoundEffect;

    // events

    public event Action<bool> OnBattleOver; // will be using this to let the gamecontroller now if the battle is over or not

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;



    // variables for party and random enemy elementals

    Element wildElement;
    Party playerParty;
    Party trainerParty;

    int escapeAttempts;
    bool isTrainerBattle = false; // will be used to check if the encounter is a npc battle or not

    // need reference to both player and trainer controller to show details for both
    playerMovement player;
    NPCBattle trainer;


    bool AllPlayerElementsFainted() // will be used to check if all the player elementals is dead
    {
        return playerParty.Elements.All(element => element.HP <= 0);
    }

    public void StartBattle(Party playerParty, Element wildElement) // calling the startBattle function with wildElement and party parameters
    {
        isTrainerBattle = false;

        this.playerParty = playerParty; // using this because we have the same variable names
        this.wildElement = wildElement; // same here
        player = playerParty.GetComponent<playerMovement>();
        StartCoroutine(SetupBattle());
    }

    // function to start npc battle
    public void StartTrainerBattle(Party playerParty, Party trainerParty) // calling the start npc battle with player party and npc party
    {
        this.playerParty = playerParty; // using this because we have the same variable names
        this.trainerParty = trainerParty;
        isTrainerBattle = true; // setting it to true

        player = playerParty.GetComponent<playerMovement>(); // getting the components
        trainer = trainerParty.GetComponent<NPCBattle>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {

        
        //clearing the hud
        playerUnit.FunctionToDisableHudInBattle();
        enemyUnit.FunctionToDisableHudInBattle();
        


        // if its not a npc battle

        if (!isTrainerBattle)
        {
            playerUnit.FunctionToDefineElements(playerParty.GetAliveElement()); // with the GetAliveElement function as parameter
            enemyUnit.FunctionToDefineElements(wildElement); //with the party and wilElement parameters in battle

            escapeAttempts = 0;

            dialogBox.FunctionToSetabilityNames(playerUnit.Element.Moves);

            yield return dialogBox.thisFunctionWillTypeDialogText($"A wild Element, {enemyUnit.Element.Base.Name} appeared.");
            yield return new WaitForSeconds(1f);

        }
        else // if its a npc battle
        {
            // we disable the elemental sprites first

            playerUnit.gameObject.SetActive(false); // disabeling the player elemental
            enemyUnit.gameObject.SetActive(false); // diabeling the enemy unit

            // then we show the player and npc image

            playerImage.gameObject.SetActive(true); // activating the player character imgage sprite
            trainerImage.gameObject.SetActive(true); // same for enemy

            playerImage.sprite = player.Sprite; // sett the sprite for player
            trainerImage.sprite = trainer.Sprite; // same for npc

            yield return dialogBox.thisFunctionWillTypeDialogText($"{trainer.Name} is ready to battle you"); // showing a dialog before the battle


            // sending first elemental in the party
            playerImage.gameObject.SetActive(false); // disabeling player image sprite
            playerUnit.gameObject.SetActive(true); // enabeling player elemental sprite in battle
            var playerElemental = playerParty.GetAliveElement();
            playerUnit.FunctionToDefineElements(playerElemental);
            yield return dialogBox.thisFunctionWillTypeDialogText($"{playerElemental.Base.Name}");
            dialogBox.FunctionToSetabilityNames(playerUnit.Element.Moves); // setting up the UI for moves


            // for npc

            trainerImage.gameObject.SetActive(false); // turning off the sprite
            enemyUnit.gameObject.SetActive(true); // show the enemey elemental sprite
            var enemyElemental = trainerParty.GetAliveElement();
            enemyUnit.FunctionToDefineElements(enemyElemental);



        }


        partyScreen.Init();
        ActionSelection();




    }



    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Elements.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        StartCoroutine(ShowActionDialog());

        //StartCoroutine(dialogBox.TypeDialog("Choose an action"));
        //dialogBox.EnableActionSelector(true);

    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.functionToSetUpelementalTeamData(playerParty.Elements);
        partyScreen.gameObject.SetActive(true);

    }

    private bool battleTextIsTyping = false;
    IEnumerator ShowActionDialog()
    {
        battleTextIsTyping = true; // Sett til true når dialogen starter
        yield return StartCoroutine(dialogBox.thisFunctionWillTypeDialogText("Choose an action"));
        yield return new WaitForSeconds(1f); // Pause for 1 second
        dialogBox.ActivateChoiceSelector(true);
        battleTextIsTyping = false; // Sett til false når dialogen er ferdig

    }


    //After the playeraction, the player needs to choose a move
    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.ActivateChoiceSelector(false);
        dialogBox.ActivateDialogText(false);
        dialogBox.ActivateAbilitySelector(true);
    }



    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Element.CurrentMove = playerUnit.Element.Moves[currentMove];
            enemyUnit.Element.CurrentMove = enemyUnit.Element.GetRandomMove();

            //Check who goes first
            bool playerGoesFirst = playerUnit.Element.Speed >= enemyUnit.Element.Speed;

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondElement = secondUnit.Element;

            //first turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Element.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            if (secondElement.HP > 0)
            {
                //second turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Element.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }

        }
        else
        {
            if (playerAction == BattleAction.SwitchElement)
            {
                var selectedElement = playerParty.Elements[currentMember];
                state = BattleState.Busy;
                yield return SwitchElement(selectedElement);
            }
            else if (playerAction == BattleAction.Flee)
            {
                yield return TryToEscape();
            }

            //enemy gets turn
            var enemyMove = enemyUnit.Element.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }

        if (state != BattleState.BattleOver)
            ActionSelection();

    }


    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Element.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Element);
            yield return sourceUnit.BothHuds.FunctionToUpdateElementalsHP();
            yield break;
        }

        move.PP--;
        //This will show the player element used move on the screen
        yield return dialogBox.thisFunctionWillTypeDialogText($"{sourceUnit.Element.Base.Name} used {move.Base.Name}");

        sourceUnit.FunctionToShowAttackAnimation();
        yield return new WaitForSeconds(1f); //wait for 1 second before the enemy recive damage
        targetUnit.FunctionToShowHitAnimation();

        if (move.Base.AbilityCategory == AbilityCategory.Status)
        {
            yield return RunMoveEffects(move, sourceUnit.Element, targetUnit.Element);
        }
        else
        {
            //bool isFainted = enemyUnit.Element.TakeDamage(move, enemyUnit.Element); //This will apply damage to the enemy
            var damageInformation = targetUnit.Element.TakeDamage(move, sourceUnit.Element); //This will apply damage to the enemy
            yield return targetUnit.BothHuds.FunctionToUpdateElementalsHP();
            yield return ShowDamageInformation(damageInformation);
        }

        if (targetUnit.Element.HP <= 0)
        {
            yield return HandleElementFainted(targetUnit);
        }


    }

    IEnumerator RunMoveEffects(Move move, Element source, Element target)
    {
        //status Condition
        var effects = move.Base.AbilityEffects;
        if (move.Base.AbilityEffects.Buffs != null)
        {
            if (move.Base.AbilityTarget == AbilityTarget.Self)
                source.ApplyBoosts(effects.Buffs);
            else
                target.ApplyBoosts(effects.Buffs);
        }

        //stat boosting
        if (effects.Buffs != null)
        {
            if (move.Base.AbilityTarget == AbilityTarget.Self)
                source.ApplyBoosts(effects.Buffs);
            else
                target.ApplyBoosts(effects.Buffs);
        }

        //stauts condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        //volatile status condition
        if (effects.HarmfulEffect != ConditionID.none)
        {
            target.SetVolatileStatus(effects.HarmfulEffect);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator ShowStatusChanges(Element element)
    {
        while (element.StatusChanges.Count > 0)
        {
            var message = element.StatusChanges.Dequeue();
            yield return dialogBox.thisFunctionWillTypeDialogText(message);
        }

    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        //statuses like burn or psn will hurt the element after the turn
        sourceUnit.Element.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Element);
        yield return sourceUnit.BothHuds.FunctionToUpdateElementalsHP();
        if (sourceUnit.Element.HP <= 0)
        {
            yield return HandleElementFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);

        }
    }

    IEnumerator HandleElementFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.thisFunctionWillTypeDialogText($"{faintedUnit.Element.Base.Name} Fainted");
        faintedUnit.FunctionToShowDeathAnimation();
        // if the enemy is dead we wait for 2 secunds and the we call the battleFinished even
        yield return new WaitForSeconds(2f); //waiting for 2 sec

        if (!faintedUnit.CheckForPlayerUnit)
        {
            //Exp gain
            int expYield = faintedUnit.Element.Base.ExpAmountGiven;
            int enemyLevel = faintedUnit.Element.Level;
            //float trainerBonus = (isTrainerBattle)? 1.5f : 1f;

            int expGain = Mathf.FloorToInt(expYield * enemyLevel /* * trainerBonus*/) / 7;
            playerUnit.Element.Exp += expGain;
            yield return dialogBox.thisFunctionWillTypeDialogText($"{playerUnit.Element.Base.Name} got {expGain} exp");
            yield return playerUnit.BothHuds.FunctionForExpAnimation();

            //check level up
            while (playerUnit.Element.CheckForLevelUp())
            {
                playerUnit.BothHuds.FunctionToSetElementLevel();
                yield return dialogBox.thisFunctionWillTypeDialogText($"{playerUnit.Element.Base.Name} increased to level {playerUnit.Element.Level}"); 

                yield return playerUnit.BothHuds.FunctionForExpAnimation(true);

            }


        }

        CheckForBattleOver(faintedUnit);
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.CheckForPlayerUnit)
        {
            var nextElement = playerParty.GetAliveElement();
            if (nextElement != null)
                OpenPartyScreen();

            else
                if (AllPlayerElementsFainted())
            {
                BattleOver(false); // battleFinished will be set to false indication that t
                // Bytt til Game Over-scenen
                SceneManager.LoadScene("GameOverScene"); // Endre "GameOverScene" til navnet på din Game Over-scene

                
            }

        }
        else

            if (!isTrainerBattle)
        {
            BattleOver(true); // ending the battle if its not an npc battle


        }
        else

        {

            // check if there is anymore healty elementals in the npc party
            var nextElemental = trainerParty.GetAliveElement();
            if (nextElemental != null)
                StartCoroutine(SendNextTrainerElemental(nextElemental)); // if there is still alive elemental in the npcs group

            else
                BattleOver(true); // if not then the battle is over



        }




    }

    IEnumerator ShowDamageInformation(DamageInformation damageInformation)
    {
        if (damageInformation.Critical)
            yield return dialogBox.thisFunctionWillTypeDialogText("Critical hit");

        if (damageInformation.Type > 1f)
        {
            yield return dialogBox.thisFunctionWillTypeDialogText("Its weak to that element");
            yield return new WaitForSeconds(1.5f);
        }


        else if (damageInformation.Type < 1f)
        {
            yield return dialogBox.thisFunctionWillTypeDialogText("Its resistant to that element");
            yield return new WaitForSeconds(1.5f);
        }


    }

    //After the playerAction, the player need to choose an action
    public void handleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
    }


    void HandleActionSelection() //the selected action will change based on the user input
    {

        if (battleTextIsTyping)
        {
            // Dialog er aktiv, ignorere tastetrykk
            return;
        }


        if (Input.GetKeyDown(KeyCode.D))
        {
            clickSoundEffect.Play(); // play sound effect
            ++currentAction;

        }
            
        else if (Input.GetKeyDown(KeyCode.A))
        {
            clickSoundEffect.Play(); // play sound effect
            --currentAction;
        }
           
        else if (Input.GetKeyDown(KeyCode.S))
        {
            clickSoundEffect.Play(); // play sound effect
            currentAction += 2;
        }
           
        else if (Input.GetKeyDown(KeyCode.W))
        {
            clickSoundEffect.Play(); // play sound effect
            currentAction -= 2;
        }
            

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateChoices(currentAction);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            confirmSoundEffect.Play(); // play confirm sound effect
            if (currentAction == 0)
            {
                //Fight is selected
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                //Purse is selected
            }
            else if (currentAction == 2)
            {
                //Element is selected
                prevState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                //flee is selected
                StartCoroutine(RunTurns(BattleAction.Flee));

            }
        }
    }


    void HandleMoveSelection()
    {
        if (battleTextIsTyping)
        {
            // Dialog er aktiv, ignorere tastetrykk
            return;
        }


        if (Input.GetKeyDown(KeyCode.D))
        {
            clickSoundEffect.Play();
            ++currentMove;
        }
            
        else if (Input.GetKeyDown(KeyCode.A))
        {
            clickSoundEffect.Play();
            --currentMove;
        }
            
        else if (Input.GetKeyDown(KeyCode.S))
        {
            clickSoundEffect.Play();
            currentMove += 2;
        }
            
        else if (Input.GetKeyDown(KeyCode.W))
        {
            clickSoundEffect.Play();
            currentMove -= 2;
        }
            

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Element.Moves.Count - 1);

        dialogBox.UpdateAbilitySelection(currentMove, playerUnit.Element.Moves[currentMove]);

        //perform a move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            confirmSoundEffect.Play();
            dialogBox.ActivateAbilitySelector(false);
            dialogBox.ActivateDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move)); //This will perform the move
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            confirmSoundEffect.Play();
            dialogBox.ActivateAbilitySelector(false);
            dialogBox.ActivateDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        if (battleTextIsTyping)
        {
            // Dialog er aktiv, ignorere tastetrykk
            return;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            clickSoundEffect.Play();
            ++currentMember;
        }
            
        else if (Input.GetKeyDown(KeyCode.A))
        {
            clickSoundEffect.Play();
            --currentMember;
        }
           
        else if (Input.GetKeyDown(KeyCode.S))
        {
            clickSoundEffect.Play();
            currentMember += 2;
        }
            
        else if (Input.GetKeyDown(KeyCode.W))
        {
            clickSoundEffect.Play();
            currentMember -= 2;
        }
            

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Elements.Count - 1);

        partyScreen.ElementPickFunction(currentMember);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            confirmSoundEffect.Play();
            var selectedMember = playerParty.Elements[currentMember];
            if (selectedMember.HP <= 0)
            {
                partyScreen.FunctionToShowInfoText("Element is knocked out, pick another");
                return;
            }
            if (selectedMember == playerUnit.Element)
            {
                partyScreen.FunctionToShowInfoText("This is the same one, cant pick this one");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchElement));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchElement(selectedMember));

            }


        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            confirmSoundEffect.Play(); // new
            partyScreen.gameObject.SetActive(false);
            ActionSelection();
        }


    }

    IEnumerator SwitchElement(Element newElement)
    {

        if (playerUnit.Element.HP > 0)
        {

            yield return dialogBox.thisFunctionWillTypeDialogText($"Return! {playerUnit.Element.Base.name}");
            playerUnit.FunctionToShowDeathAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.FunctionToDefineElements(newElement);
        dialogBox.FunctionToSetabilityNames(newElement.Moves);
        yield return dialogBox.thisFunctionWillTypeDialogText($"Get in {newElement.Base.Name}!");

        state = BattleState.RunningTurn;
    }

    IEnumerator SendNextTrainerElemental(Element nextElemental)
    {

        state = BattleState.Busy;
        enemyUnit.FunctionToDefineElements(nextElemental); // sending out the next elemental
        yield return dialogBox.thisFunctionWillTypeDialogText($"{trainer.Name}: your turn {nextElemental.Base.Name}");

        state = BattleState.RunningTurn; // so the battle can continue



    }



    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;
        if (isTrainerBattle)
        {
            yield return dialogBox.thisFunctionWillTypeDialogText($"you cant get away!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;

        int playerSpeed = playerUnit.Element.Speed;
        int enemySpeed = enemyUnit.Element.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.thisFunctionWillTypeDialogText($"Got away safely");
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.thisFunctionWillTypeDialogText($"Got away safely");
                BattleOver(true);

            }
            else
            {
                yield return dialogBox.thisFunctionWillTypeDialogText($"gotta stay!");
                state = BattleState.RunningTurn;
            }
        }

    }

}

