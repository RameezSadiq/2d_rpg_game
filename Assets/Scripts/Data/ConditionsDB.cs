using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB : MonoBehaviour
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }

    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.dis,
            new Condition()
            {
                Name = "Poison",
                StartMessage = "has been poisoned",
                OnAfterTurn = (Element element) =>
                {
                    element.UpdateHP(element.MaxHp / 8);
                    element.StatusChanges.Enqueue($"{element.Base.Name} hurt itself due to poision");
                }

            }
        },
        {
            ConditionID.ign,
            new Condition()
            {
                Name = "Burn",
                StartMessage = "has been burnt",
                OnAfterTurn = (Element element) =>
                {
                    element.UpdateHP(element.MaxHp / 16);
                    element.StatusChanges.Enqueue($"{element.Base.Name} hurt itself due to burn");
                }

            }
        },
        {
            ConditionID.slw,
            new Condition()
            {
                Name = "Paralyzed",
                StartMessage = "has been paralyzed",
                OnBeforeMove = (Element element) =>
                {
                    if (Random.Range(1, 5 ) == 1)
                    {
                        element.StatusChanges.Enqueue($"{element.Base.Name}'s is paralyzed");
                        return false;
                    }

                    return true;
                }

            }
        },
        {
            ConditionID.col,
            new Condition()
            {
                Name = "Freeze",
                StartMessage = "has been frozen",
                OnBeforeMove = (Element element) =>
                {
                    if (Random.Range(1, 5 ) == 1)
                    {
                        element.CureStatus();
                        element.StatusChanges.Enqueue($"{element.Base.Name}'s is not frozen anymore");
                        return true;
                    }

                    return false;
                }

            }
        },
        {
            ConditionID.ptr,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has fallen asleep",
                OnStart = (Element element) =>
                {
                    //sleep for 1-3 turns
                    element.StatusTime = Random.Range(1, 4);
                    Debug.Log($"will be asleep for {element.StatusTime} moves");
                },
                OnBeforeMove = (Element element) =>
                {
                    if (element.StatusTime <= 0)
                    {
                        element.CureStatus();
                        element.StatusChanges.Enqueue($"{element.Base.Name} woke up");
                        return true;

                    }

                    element.StatusTime--;
                    element.StatusChanges.Enqueue($"{element.Base.Name} is sleeping");
                    return false;
                }

            }
        },

        //volatile sattus conditions
        {
            ConditionID.confusion,
            new Condition()
            {
                Name = "Confusion",
                StartMessage = "is confused",
                OnStart = (Element element) =>
                {
                    //sleep for 1-3 turns
                    element.StatusTime = Random.Range(1, 4);
                    Debug.Log($"will be confused for {element.VolatileStatusTime} moves");
                },
                OnBeforeMove = (Element element) =>
                {
                    if (element.VolatileStatusTime <= 0)
                    {
                        element.CureVolatileStatus();
                        element.StatusChanges.Enqueue($"{element.Base.Name} is not confused");
                        return true;

                    }

                    element.VolatileStatusTime--;
                    if (Random.Range(1, 3) == 1)
                        return true;

                    element.StatusChanges.Enqueue($"{element.Base.Name} is confused");
                    element.UpdateHP(element.MaxHp / 8);
                    element.StatusChanges.Enqueue($"It hurt itself");
                    return false;
                }

            }
        }
    };
}

public enum ConditionID
{
    none, dis, ign, ptr, slw, col, 
    confusion
}