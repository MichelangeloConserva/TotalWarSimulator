using NetTopologySuite.Geometries;
using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;
using Random = UnityEngine.Random;


[System.Serializable]
public struct UnitStats
{
    public float topSpeed, movementForce, meeleRange, health, meeleDefence, meeleAttack, pathSpeed, soldierDistVertical, soldierDistLateral;
    public int startingNumOfSoldiers, startingCols; 
}





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
    [HideInInspector()]
    public Polygon meleeGeom;


    
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
                    try
                    {
                        if (transform.childCount > 0)
                        {
                            var color = transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
                            color.b = 0.21f;
                            for (int i = 1; i < transform.childCount; i++)
                                transform.GetChild(i).GetChild(0).GetComponent<MeshRenderer>().material.color = color;
                        }
                    }
                    finally { }
                    
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

        CreateMeleeGeometry();
    }

    public void CreateMeleeGeometry()
    {
        float exp = 2f;
        var points = _soldiers.Select(s => new Point(s.position.x + exp * s.go.transform.forward.x, s.position.z + exp * s.go.transform.forward.z))
             .Concat(_soldiers.Select(s => new Point(s.position.x + exp * s.go.transform.right.x, s.position.z + exp * s.go.transform.right.z)))
             .Concat(_soldiers.Select(s => new Point(s.position.x - exp * s.go.transform.right.x, s.position.z - exp * s.go.transform.right.z)))
             .Concat(_soldiers.Select(s => new Point(s.position.x - exp * s.go.transform.forward.x, s.position.z - exp * s.go.transform.forward.z)))
             .Concat(_soldiers.Select(s => new Point(s.position.x + exp * s.go.transform.forward.x, s.position.z - exp * s.go.transform.forward.z)))
             .Concat(_soldiers.Select(s => new Point(s.position.x - exp * s.go.transform.forward.x, s.position.z + exp * s.go.transform.forward.z)));

        meleeGeom = (Polygon)new MultiPoint(points.ToArray()).ConvexHull();//.Buffer(1.5f);
    }


    private Soldier[] _soldiers;      // TODO : update when soldiers die
    private Dictionary<Unit, bool> bfightingUnit;
    private Vector3 _position;            
    private bool _inFight, _isSelected;

    public void Initialize()
    {
        InstantiateUnit();
        soldierLocalScale = _soldiers[0].go.transform.localScale;
    }
    
    protected void OnDrawGizmos()
    {
        if(DEBUG_MODE)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPos + Vector3.up * 5, 0.5f);
            Gizmos.DrawRay(targetPos + Vector3.up * 5, targetDirection);


            if (!Application.isPlaying)
                Start();


            if (isInFight)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.green;

            int i = 0;
            for (i = 0; i < meleeGeom.Coordinates.Length - 1; i++)
            {
                Gizmos.DrawLine(new Vector3((float)meleeGeom.Coordinates[i].X, 0, (float)meleeGeom.Coordinates[i].Y),
                                new Vector3((float)meleeGeom.Coordinates[i + 1].X, 0, (float)meleeGeom.Coordinates[i + 1].Y));
            }

            i = meleeGeom.Coordinates.Length/2;
            Gizmos.DrawSphere(new Vector3((float)meleeGeom.Coordinates[i].X * 0.5f + (float)meleeGeom.Coordinates[i + 1].X * 0.5f, 0, 0.5f * (float)meleeGeom.Coordinates[i + 1].Y + 0.5f * (float)meleeGeom.Coordinates[i].Y), 0.5f);


        }
    }
    public void Start()
    {
        army = transform.parent.GetComponent<Army>();

        if (!Application.isPlaying)
            bfightingUnit = army.enemy.transform.GetComponentsInChildren<Unit>().ToDictionary(u => u, u => false);

        combactManager = army.combactManager;

        targetPos = transform.position;
        targetDirection = transform.forward;
    }

    protected void Update()
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

    protected void FixedUpdate()
    {
        CheckForDeadSoldiers();
        UpdatePosition();
        UpdateMeleeGeom();
    }

    private void CheckForDeadSoldiers()
    {
        var l = _soldiers.ToList();
        foreach (var s in soldiers)
            if (s.health < 0)
            {
                l.Remove(s);
                StartCoroutine(DestroyGO(s.go));
            }
        _soldiers = l.ToArray();
    }

    private void UpdatePosition()
    {
        //_position = _soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        _position = Vector3.zero;
        foreach (var s in soldiers)
            _position += s.position;
        _position /= numOfSoldiers;
    }

    private void UpdateMeleeGeom()
    {
        var c = new Vector3((float)meleeGeom.Centroid.X, 0, (float)meleeGeom.Centroid.Y);
        int i = meleeGeom.Coordinates.Length / 2;
        var cFront = new Vector3((float)meleeGeom.Coordinates[i].X * 0.5f + (float)meleeGeom.Coordinates[i + 1].X * 0.5f, 0,
            (float)meleeGeom.Coordinates[i].Y * 0.5f + (float)meleeGeom.Coordinates[i + 1].Y * 0.5f);


        //var deg = Vector3.SignedAngle(cFront - c, GetVector3Down(targetDirection), Vector3.up);

        var deg = 0;
        meleeGeom = UpdateGeometry(meleeGeom, position.x - c.x, position.z - c.z, deg);
    }

    










}
