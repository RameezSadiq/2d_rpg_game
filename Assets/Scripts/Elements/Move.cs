using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this class includes data of abilitis that will change during battles
public class Move
{
    public AbilityCore Base { get; set; }
    public int PP { get; set; }

    public Move(AbilityCore pBase)
    {
        Base = pBase;
        PP = pBase.Ap;
    }
}
