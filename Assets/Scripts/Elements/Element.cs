using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // to show classes in the inspector
public class Element
{

    // making serializeField for element 

    [SerializeField] ElementCore _base; // the base for element
    [SerializeField] int level; // the level


    public ElementCore Base 
    { 
        get { return _base; }
    
    } 

    public int Level 
    { 
        get { return  level; }
    } 

    public int Exp { get; set; }

    public int HP { get; set; }

    public List<Move> Moves { get; set; }
    public Dictionary<Stat, int> Stats { get; private set; }

    public Dictionary<Stat, int> StatBoosts { get; private set; }

    public Condition Status { get; private set; }
    public int StatusTime { get; set; }

    public Condition VolatileStatus { get; private set; }
    public int VolatileStatusTime { get; set; }

    public Move CurrentMove { get; set; }

    public Queue<string> StatusChanges { get; private set; } = new Queue<string>();
    public bool HpChanged { get; set; }
    public event System.Action OnStatusChanged;

    public void initialization()
    {

        //Generate moves
        Moves = new List<Move>();
        foreach (var move in Base.LearnableMoves)
        {
            if (move.Level <= Level)
                Moves.Add(new Move(move.Base));

            if (Moves.Count >= 4)
                break;
        }

        Exp = Base.FunctionTogetExp(level);

        CalculateStats();
        HP = MaxHp;

        ResetStatBoost();
        Status = null;
        VolatileStatus = null;

    }

    void CalculateStats()
    {
        Stats = new Dictionary<Stat, int>();
        Stats.Add(Stat.Attack, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5 );
        Stats.Add(Stat.Defense, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5 );
        Stats.Add(Stat.Magic, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5 );
        Stats.Add(Stat.MagicDefence, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5 );
        Stats.Add(Stat.Speed, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5 );
    
        MaxHp = Mathf.FloorToInt((Base.Speed * Level) / 100f) + 10 + Level; 
    }

    void ResetStatBoost()
    {

        StatBoosts = new Dictionary<Stat, int>()
        {
            {Stat.Attack, 0 },
            {Stat.Defense, 0 },
            {Stat.Magic, 0 },
            {Stat.MagicDefence, 0 },
            {Stat.Speed, 0 },
        };
    }

    int GetStat(Stat stat)
    {
        int statVal = Stats[stat];

        int boost = StatBoosts[stat];
        var boostValues = new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };

        if (boost >= 0)
            statVal = Mathf.FloorToInt(statVal + boostValues[boost]);
        else
            statVal = Mathf.FloorToInt(statVal / boostValues[-boost]);


        return statVal;
    }

    public void ApplyBoosts(List<StatBoost> statBoosts)
    {
        foreach (var statBoost in statBoosts)
        {
            var stat = statBoost.stat;
            var boost = statBoost.boost;

            StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] + boost, -6, 6);

            if (boost > 0)
                StatusChanges.Enqueue($"{Base.Name}'s {stat} rose!");
            else
                StatusChanges.Enqueue($"{Base.Name}'s {stat} fell!");

            Debug.Log($"{stat} has been boosted to {StatBoosts[stat]}");
        }
    }

    //calcute stats for element in current level

    public bool CheckForLevelUp()
    {
        if (Exp > Base.FunctionTogetExp(level + 1))
        {
            ++level;
            return true;
        }

        return false;
    }
    public int Attack
    {
        get { return GetStat(Stat.Attack);  } //formel eksempel fra pokemon
    }
    public int Defense
    {
        get { return GetStat(Stat.Defense); }
    }
    public int SpAttack
    {
        get { return GetStat(Stat.Magic); }
    }
    public int SpDefense
    {
        get { return GetStat(Stat.MagicDefence); }
    }
    public int Speed {
        get { 
            return GetStat(Stat.Speed); 
        }
    }


    public int MaxHp { get; private set; }
    

    //Function for taking damage
    public DamageInformation TakeDamage(Move move, Element attacker)
    {


        // adding chance to get crits
        float critChance = 1f;
        bool crit = false;
        if (Random.value * 100f <= 7.23f)
            critChance = 2f;
        

        // using the effectivness function from the table
        //skill1 and 2 is the type of elements (need to change name later) and using two because an element can have two types
        float typeEffectivness = TypeTable.GetElementalEffectiveness(move.Base.Type, this.Base.type1) * TypeTable.GetElementalEffectiveness(move.Base.Type, this.Base.type2);

        var damageInformation = new DamageInformation()
        {
            Type = typeEffectivness,
            Critical = crit,
            Fainted = false
        };

        float attack = (move.Base.AbilityCategory == AbilityCategory.Magic) ? attacker.SpAttack : attacker.Attack;
        float defense = (move.Base.AbilityCategory == AbilityCategory.Magic) ? SpDefense: Defense;
     

        //This is a formula for taking damage @ bulbapedia with type effectivness and crit chance implemented
        float modifiers = Random.Range(0.85f, 1f) * typeEffectivness * critChance;
        float a = (2 * attacker.Level + 10) / 250f;
        float d = a * move.Base.Damage * ((float)attacker.Attack / Defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        UpdateHP(damage);

        // return false;
        return damageInformation;

    }

    public void UpdateHP(int damage)
    {
        HP = Mathf.Clamp(HP - damage, 0, MaxHp);
        HpChanged = true;

    }

    public void SetStatus(ConditionID conditionId)
    {
        if (Status != null) return;

        Status = ConditionsDB.Conditions[conditionId];
        Status?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.Name} {Status.StartMessage}");
        OnStatusChanged?.Invoke();
    }

    public void CureStatus()
    {
        Status = null;
        OnStatusChanged?.Invoke();
    }

    public void SetVolatileStatus(ConditionID conditionId)
    {
        if (VolatileStatus != null) return;

        VolatileStatus = ConditionsDB.Conditions[conditionId];
        VolatileStatus?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.Name} {VolatileStatus.StartMessage}");
        OnStatusChanged?.Invoke();
    }

    public void CureVolatileStatus()
    {
        VolatileStatus = null;
        
    }

    public Move GetRandomMove() //This is for enemy player to perform a random move
    {
        int r = Random.Range(0, Moves.Count);
        return Moves[r];
    }

    public bool OnBeforeMove()
    {
        bool canPerformMove = true;
        if (Status?.OnBeforeMove != null)
        {
            if (!Status.OnBeforeMove(this))
                canPerformMove = false;
        }

        if (VolatileStatus?.OnBeforeMove != null)
        {
            if (!VolatileStatus.OnBeforeMove(this))
                canPerformMove = false;
        }

        return canPerformMove;
    }
    public void OnAfterTurn()
    {
        Status?.OnAfterTurn?.Invoke(this);
        VolatileStatus?.OnAfterTurn?.Invoke(this);
    }

    public void OnBattleOver()
    {
        VolatileStatus = null;
        ResetStatBoost();
    }
}

public class DamageInformation
{

    public bool Critical { get; set; }
    public float Type { get; set; }
    public bool Fainted { get; set; }


}


