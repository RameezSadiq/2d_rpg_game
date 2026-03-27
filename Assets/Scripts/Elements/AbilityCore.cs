using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Element/Create new move")]
public class AbilityCore : ScriptableObject
{
    [SerializeField] string name;

    [TextArea]
    [SerializeField] string abilityInfo;

    [SerializeField] ElementType type;
    [SerializeField] int damage;
    [SerializeField] int hitRating;
    [SerializeField] int ap; //number of times a move can be perfromed
    [SerializeField] AbilityCategory abilityCategory;
    [SerializeField] AbilityEffects abilityEffects;
    [SerializeField] AbilityTarget abilityTarget;

    public string Name
    {
        get { return name; }
    }

    public string AbilityInfo
    {
        get { return abilityInfo; }
    }

    public ElementType Type
    {
        get { return type; }
    }

    public int Damage
    {
        get { return damage; }
    }

    public int HitRating
    {
        get { return hitRating; }
    }

    public int Ap
    {
        get { return ap; }
    }




    public AbilityCategory AbilityCategory {
        get { return abilityCategory; }
    }

    public AbilityEffects AbilityEffects {
        get { return abilityEffects; }
    }
    
    public AbilityTarget AbilityTarget {
        get { return abilityTarget; }
    }
}

[System.Serializable]

public class AbilityEffects
{
    [SerializeField] List<StatBoost> buffs;
    [SerializeField] ConditionID status;
    [SerializeField] ConditionID harmfulEffect;

    public List<StatBoost> Buffs {
        get { return buffs; }
    }

    public ConditionID Status {
        get { return status; }
    }

    public ConditionID HarmfulEffect{
        get { return harmfulEffect; }
    }
}

[System.Serializable]

public class StatBoost
{
    public Stat stat;
    public int boost;
}

public enum AbilityCategory
{
    Physical, Magic, Status
}

public enum AbilityTarget
{
    Foe, Self
}
