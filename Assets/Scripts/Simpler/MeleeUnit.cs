using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;


public class MeleeUnit : BaseUnit
{


    [HideInInspector()]
    public int numOfSoldiers
    {
        get { return soldiers.Length; }
    }
    [HideInInspector()]
    public MeleeSoldier[] soldiers 
    {
        get; private set;
    }


    protected new void Initialize()
    {
        base.Initialize();


        var res = GetFormationAtPos(transform.position, transform.forward, baseStats.startingNumOfSoldiers, cols, baseStats.soldierDistLateral, baseStats.soldierDistVertical);
        soldiers = new MeleeSoldier[baseStats.startingNumOfSoldiers];
        GameObject g;
        int k = 0;
        foreach (var v in res)
        {
            g = Instantiate(soldierBase, v, transform.rotation, transform);
            g.layer = LayerMask.NameToLayer(soldierLayerName);
            soldiers[k] = new MeleeSoldier(g, baseStats, this);
            g.GetComponent<SoldierUtils>().sRef = soldiers[k++];


        }
    }


    protected new void Start()
    {
        base.Start();




    }









}
