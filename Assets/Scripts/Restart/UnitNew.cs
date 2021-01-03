﻿using PathCreation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MeleeStats;
using static Utils;

public class UnitNew : MonoBehaviour
{



    [Header("Unit state")]
    public UnitState state;
    public UnitCombactState combactState;
    public UnitMovementState movementState;

    [Header("Unit targets")]
    public UnitNew fightingTarget;
    public UnitNew commandTarget;




    public List<SoldierNew> soldiers;
    public HashSet<UnitNew> fightingAgainst = new HashSet<UnitNew>();

    public ArmyNew army;

    public CUnitNew cunit;

    public int numCols;
    public float soldierDistLateral;
    public float soldierDistVertical;
    public float pathSpeed;

    private bool _isSelected, _inFight;

    public int numOfSoldiers 
    { get { return soldiers.Count; } }
    public string soldierLayerName
    {
        get { return "unitSoldier" + ((int)army.role + 1); }  // 1 is the attacker and 2 is the defender
    }
    public bool isSelected
    {
        get { return _isSelected; }
        set
        {
            if (isSelected != value)
            {
                if (value)
                {
                    var color = soldiers.First().transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
                    color.b = 1;
                    foreach(var s in soldiers)
                        s.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = color;
                }
                else
                {
                    var color = soldiers.First().transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
                    color.b = 0.21f;
                    foreach (var s in soldiers)
                        s.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = color;

                }
                _isSelected = value;
            }
        }
    }
    public bool isInFight
    {
        get { return _inFight; }
        set
        {
            if (value && !_inFight) // If it was not fighting before but now yes
            {
                //targetPos = position;
                //targetDirection = transform.forward;
            }
            if (!value && _inFight)
            {
                if (state != UnitState.ESCAPING)
                    state = UnitState.IDLE;
            }
            _inFight = value;
        }
    }
    public Vector3 position
    {
        get { return _position; }
        set { transform.position = value; }
    }
    public Vector3 direction
    {
        get { return _direction; }
        set { transform.rotation = Quaternion.LookRotation(value); }
    }
    private Vector3 _position, _direction;

    protected void OnDrawGizmos()
    {
        if (!army.DEBUG_MODE) return;

        Gizmos.DrawSphere(position + Vector3.up * 5, 0.1f);
        Gizmos.DrawRay(position + Vector3.up * 5, direction);

        Gizmos.color = Color.red;
        foreach (var s in soldiers)
        {
            Gizmos.DrawRay(s.position, s.direction);
            Gizmos.DrawLine(s.position, s.targetPos);
        }     
    }


    #region INSTATIONATION STUFF    
    public void Instantiate(Vector3 pos, Vector3 dir, MeleeStatsHolder meleeStats, Transform soldiersHolder, GameObject soldierPrefab, ArmyNew army)
    {
        position = pos;
        direction = dir;

        SetLinks(meleeStats, army);
        InstantiateSoldiers(pos, dir, meleeStats, soldierPrefab, soldiersHolder);

        var pC = new GameObject("PathCreator").AddComponent<PathCreator>();
        pC.transform.parent = army.pathCreatorsHolder.transform;
        //pC.transform.localPosition = Vector3.zero;
        //pC.transform.localRotation = Quaternion.identity;

        cunit = gameObject.AddComponent<CUnitNew>();
        cunit.Initialize(this, pC);
    }
    private void SetLinks(MeleeStatsHolder meleeStats, ArmyNew army)
    {
        this.army = army;
        numCols = meleeStats.startingCols;
        soldierDistLateral = meleeStats.soldierDistLateral;
        soldierDistVertical = meleeStats.soldierDistVertical;
        pathSpeed = meleeStats.pathSpeed;
    }
    private void InstantiateSoldiers(Vector3 pos, Vector3 dir, MeleeStatsHolder meleeStats, GameObject soldierPrefab, Transform soldiersHolder)
    {
        soldiers = new List<SoldierNew>(meleeStats.startingNumOfSoldiers);
        var res = GetFormationAtPos(pos, dir, meleeStats.startingNumOfSoldiers, numCols, soldierDistLateral, soldierDistVertical);
        GameObject g;
        foreach (Vector3 p in res)
        {
            g = Instantiate(soldierPrefab, p, Quaternion.LookRotation(dir), soldiersHolder);
            g.layer = LayerMask.NameToLayer(soldierLayerName);
            var s = g.GetComponent<SoldierNew>();
            s.Initialize(this, meleeStats, pos, dir);
            soldiers.Add(s);
        }
    }
    #endregion



    private void Update()
    {
        _position = transform.position;
        _direction = transform.forward;
    }








}