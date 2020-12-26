using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Army : MonoBehaviour
{

    public ArmyRole role;
    public CombactManager combactManager;
    public Army enemy;
    public Unit[] units;
    public string enemySoldierLayer
    {
        get { return enemy.units[0].soldierLayerName; }
    }

    public void Initialize()
    {

        units = GetComponentsInChildren<Unit>();

        foreach (var u in units)
            u.Initialize();
    }




}
