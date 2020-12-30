using NetTopologySuite.Geometries;
using PathCreation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unit;
using static Utils;


[System.Serializable]
public struct UnitStats
{
    public float topSpeed, movementForce, pathSpeed, soldierDistLateral, soldierDistVertical;
    public int startingCols, startingNumOfSoldiers;
    public float meeleRange;
    public float health;
    public float meeleAttack;
    public float meeleDefence;
}

[System.Serializable]
public struct BaseUnitStats
{
    public float topSpeed, movementForce, pathSpeed, soldierDistLateral, soldierDistVertical;
    public int startingCols, startingNumOfSoldiers;
    public float meeleRange;
    public float health;
    public float meeleAttack;
    public float meeleDefence;
}


public abstract class BaseUnit : MonoBehaviour
{
    public BaseUnitStats baseStats;

    [Header("Debug options")]
    public bool DEBUG_MODE;
    public bool updateFormationInEditor;

    [Header("Unit state")]
    public UnitState state;
    public UnitCombactState combactState;
    public UnitMovementState movementState;

    [Header("Unit targets")]
    public Unit fightingTarget;
    public Unit commandTarget;

    [Header("Linking")]
    public GameObject pathCreatorGO;
    public GameObject soldierBase;


    [HideInInspector()]
    public Army army;
    [HideInInspector()]
    public CUnit cunit;
    [HideInInspector()]
    public CombactManager combactManager;
    [HideInInspector()]
    public Vector3 targetPos;
    [HideInInspector()]
    public Vector3 targetDirection;
    [HideInInspector()]
    public int cols;
    [HideInInspector()]
    public GameObject[][] soldiersInFormation;
    [HideInInspector()]
    public Geometry meleeGeom;


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
                    var color = transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
                    color.b = 1;
                    for (int i = 1; i < transform.childCount; i++)
                        transform.GetChild(i).GetChild(0).GetComponent<MeshRenderer>().material.color = color;
                }
                else
                {
                    if (transform.childCount > 0)
                    {
                        var color = transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
                        color.b = 0.21f;
                        for (int i = 1; i < transform.childCount; i++)
                            transform.GetChild(i).GetChild(0).GetComponent<MeshRenderer>().material.color = color;
                    }
                }
                _isSelected = value;
            }
        }
    }
    //public Soldier[] soldiers   TODO : put them in the real class
    //{
    //    get; private set;
    //}
    public Vector3 position
    {
        get; private set;
    }
    public bool isInFight
    {
        get { return _inFight; }
        set
        {
            if (value && !_inFight) // If it was not fighting before but now yes
            {
                targetPos = position;
                targetDirection = transform.forward;
            }
            if (!value && _inFight)
            {
                if (state != UnitState.ESCAPING)
                    state = UnitState.IDLE;
            }
            _inFight = value;
        }
    }


    private bool _inFight, _isSelected;


    private void CleanUnit()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
            DestroyImmediate(child.gameObject);
    }
    protected void Initialize()
    {
        cunit = GetComponent<CUnit>();

        state = UnitState.IDLE;
        combactState = UnitCombactState.DEFENDING;
        movementState = UnitMovementState.WALKING;

        cols = baseStats.startingCols;

        CleanUnit();

        Instantiate(pathCreatorGO, transform.position, Quaternion.identity, transform).GetComponent<PathCreator>();


        // TODO : IN the real class
        //var res = GetFormationAtPos(transform.position, transform.forward, baseStats.startingNumOfSoldiers, cols, baseStats.soldierDistLateral, baseStats.soldierDistVertical);
        //soldiers = new Soldier[baseStats.startingNumOfSoldiers];
        //GameObject g;
        //int k = 0;
        //foreach (var v in res)
        //{
        //    g = Instantiate(soldierBase, v, transform.rotation, transform);
        //    g.layer = LayerMask.NameToLayer(soldierLayerName);
        //    soldiers[k] = new Soldier(g, this);
        //    g.GetComponent<SoldierUtils>().s = soldiers[k++];
        //}
    }

    private void OnDrawGizmos()
    {
        if (DEBUG_MODE)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPos + Vector3.up * 5, 0.5f);
            Gizmos.DrawRay(targetPos + Vector3.up * 5, targetDirection);
        }
    }

    protected void Start()
    {
        army = transform.parent.GetComponent<Army>();
        combactManager = army.combactManager;
        targetPos = transform.position;
        targetDirection = transform.forward;
    }

    void Update()
    {
        if (!Application.isPlaying)
        {

            if (updateFormationInEditor)
                Initialize();

            // TODO : put it in the real one
            //if (soldiers.Length > 0)
            //    position = soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        }
        else
        {
            if (DEBUG_MODE)
            {
                // TODO : put it in the real one
                //foreach (Soldier s in soldiers)
                //    Debug.DrawLine(s.go.transform.position, s.targetPos);
            }

        }
    }

    private void FixedUpdate()
    {
        // TODO : put it in the real one
        //// Checking for dead soldiers
        //var l = _soldiers.ToList();
        //foreach (var s in soldiers)
        //    if (s.health < 0)
        //    {
        //        l.Remove(s);
        //        StartCoroutine(DestroyGO(s.go));
        //    }
        //_soldiers = l.ToArray();


        ////_position = _soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        //_position = Vector3.zero;
        //foreach (var s in soldiers)
        //    _position += s.position;
        //_position /= numOfSoldiers;

    }






}


