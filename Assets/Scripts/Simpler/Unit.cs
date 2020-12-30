using NetTopologySuite.Geometries;
using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class Unit : MonoBehaviour
{

    public UnitStats stats;

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
    public Vector3 soldierLocalScale;
    [HideInInspector()]
    public int cols;
    [HideInInspector()]
    public int numOfSoldiers
    {
        get { return _soldiers.Length; }        
    }

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
    public Soldier[] soldiers
    {
        get { return _soldiers; }
    }
    public Vector3 position
    {
        get { return _position; }
    }
    public Geometry hull 
    {
        get { return _hull; }
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
    public Vector3 lastPos
    {
        get;
        private set;
    }
    
    public Point[] points; // TODO remove points as soldiers die
    private void CalculateHull()
    {
        //_hull = new MultiPoint(_soldiers.Select(s => new Point(s.go.transform.position.x, s.go.transform.position.z)).ToArray()).ConvexHull().Buffer(1); // TODO : encode this
        for (int i = 0; i < numOfSoldiers; i++)
            points[i] = new Point(soldiers[i].position.x, soldiers[i].position.z);

        _hull = new MultiPoint(points).ConvexHull().Buffer(1); // TODO : encode this

    }
    private void UpdateSoldiersList() // TODO : implement for when soldier die
    {
        throw new NotImplementedException("UpdateSoldiersList");
    }
    private void CleanUnit()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
            DestroyImmediate(child.gameObject);
    }
    private void InstantiateUnit()
    {
        cunit = GetComponent<CUnit>();

        state = UnitState.IDLE;
        combactState = UnitCombactState.DEFENDING;
        movementState = UnitMovementState.WALKING;

        cols = stats.startingCols;
        points = new Point[stats.startingNumOfSoldiers];

        CleanUnit();

        Instantiate(pathCreatorGO, transform.position, Quaternion.identity, transform).GetComponent<PathCreator>();


        var res = GetFormationAtPos(transform.position, transform.forward, stats.startingNumOfSoldiers, cols, stats.soldierDistLateral, stats.soldierDistVertical);
        _soldiers = new Soldier[stats.startingNumOfSoldiers];
        GameObject g;
        int k = 0;
        foreach (var v in res)
        {
            g = Instantiate(soldierBase, v, transform.rotation, transform);
            g.layer = LayerMask.NameToLayer(soldierLayerName);
            _soldiers[k] = new Soldier(g, this);
            g.GetComponent<SoldierUtils>().s = _soldiers[k++];
        }
    }

    public Geometry _hull;
    private Soldier[] _soldiers;      // TODO : update when soldiers die
    private Dictionary<Unit, bool> bfightingUnit;
    private Vector3 _position;            
    private bool _inFight, _isSelected;

    public void Initialize()
    {
        InstantiateUnit();
        soldierLocalScale = _soldiers[0].go.transform.localScale;
    }
    
    private void OnDrawGizmos()
    {
        if(DEBUG_MODE)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPos + Vector3.up * 5, 0.5f);
            Gizmos.DrawRay(targetPos + Vector3.up * 5, targetDirection);


            if(!Application.isPlaying)
                CalculateHull();
            Gizmos.color = Color.green;
            if(isInFight)
                Gizmos.color = Color.red;

            if (_hull == null) return;

            var points = _hull.Coordinates.ToArray();
            for (int i = 0; i < points.Length - 1; i++)
                Gizmos.DrawLine(new Vector3((float)points[i].X, 1, (float)points[i].Y), new Vector3((float)points[i + 1].X, 1, (float)points[i + 1].Y));
            Gizmos.DrawLine(new Vector3((float)points[0].X, 1, (float)points[0].Y), new Vector3((float)points[points.Length - 1].X, 1, (float)points[points.Length - 1].Y));
        }
    }
    protected void Start()
    {
        army = transform.parent.GetComponent<Army>();

        if (!Application.isPlaying)
            bfightingUnit = army.enemy.transform.GetComponentsInChildren<Unit>().ToDictionary(u => u, u => false);

        combactManager = army.combactManager;

        targetPos = transform.position;
        targetDirection = transform.forward;
    }
    void Update()
    {
        if (!Application.isPlaying)
        {
            bfightingUnit = transform.parent.GetComponent<Army>().enemy.transform.GetComponentsInChildren<Unit>().ToDictionary(u => u, u => false);

            if (updateFormationInEditor)
                InstantiateUnit();

            if (_soldiers.Length > 0)
                _position = _soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        }
        else
        {
            if (DEBUG_MODE)
            {
                foreach (Soldier s in soldiers)
                    Debug.DrawLine(s.go.transform.position, s.targetPos);
            }

        }
    }

    private void FixedUpdate()
    {

        // Checking for dead soldiers
        var l = _soldiers.ToList();
        foreach (var s in soldiers)
            if (s.health < 0)
            {
                l.Remove(s);
                StartCoroutine(DestroyGO(s.go));
            }
        _soldiers = l.ToArray();


        //_position = _soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        _position = Vector3.zero;
        foreach (var s in soldiers)
            _position += s.position;
        _position /= numOfSoldiers;

    }




}
