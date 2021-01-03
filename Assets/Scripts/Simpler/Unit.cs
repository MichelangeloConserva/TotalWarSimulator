using NetTopologySuite.Geometries;
using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MeleeStats;
using static Utils;
using Random = UnityEngine.Random;


//[System.Serializable]
//public struct UnitStats
//{
//    public float topSpeed, movementForce, meeleRange, health, meeleDefence, meeleAttack, pathSpeed, soldierDistVertical, soldierDistLateral;
//    public int startingNumOfSoldiers, startingCols; 
//}


[ExecuteInEditMode]
public class Unit : MonoBehaviour
{
    public MeleeStats meleeStatReference;
    public MeleeStatsHolder meleeStats;


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
        get { return soldiers == null ? 0 : _soldiers.Length; }        
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
        meleeStats = meleeStatReference.meleeHolder;

        state = UnitState.IDLE;
        combactState = UnitCombactState.DEFENDING;
        movementState = UnitMovementState.WALKING;

        CleanUnit();

        Instantiate(pathCreatorGO, transform.position, Quaternion.identity, transform).GetComponent<PathCreator>();


        var res = GetFormationAtPos(transform.position, transform.forward, meleeStats.startingNumOfSoldiers, meleeStats.startingCols, meleeStats.soldierDistLateral, meleeStats.soldierDistVertical);
        _soldiers = new Soldier[meleeStats.startingNumOfSoldiers];
        GameObject g;
        int k = 0;
        foreach (var v in res)
        {
            g = Instantiate(meleeStatReference.soldierPrefab, v, transform.rotation, transform);
            g.layer = LayerMask.NameToLayer(soldierLayerName);
            _soldiers[k] = new Soldier(g, this, meleeStats);
            g.GetComponent<SoldierUtils>().s = _soldiers[k++];
        }

        CreateMeleeGeometry();
    }

    public void CreateMeleeGeometry()
    {
        float exp = 1.5f;
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
                Initialize();


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

        combactManager = army.combactManager;

        targetPos = transform.position;
        targetDirection = transform.forward;
    }

    protected void Update()
    {
        if (!Application.isPlaying)
        {

            if (updateFormationInEditor)
                InstantiateUnit();

            if (_soldiers != null && _soldiers.Length > 0)
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

        CheckForDeadSoldiers();
        if (numOfSoldiers == 0) return;

        UpdatePosition();
        UpdateMeleeGeom();

        if(commandTarget)
        {
            UpdateTrajectory();
        }


    }

    private void UpdateTrajectory()
    {
        cunit.MoveAt(commandTarget.position);
    }

    private HashSet<Soldier> deads = new HashSet<Soldier>();
    private void CheckForDeadSoldiers()
    {
        if (soldiers == null) return;

        deads.Clear();
        foreach (var s in soldiers)
            if (s.health < 0)
            {
                deads.Add(s);
                StartCoroutine(DestroyGO(s.go));
            }
        if (deads.Count > 0)
        {
            _soldiers = soldiers.Where(s => !deads.Contains(s)).ToArray();
        }

    }

    private void UpdatePosition()
    {
        //_position = _soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        _position = Vector3.zero;
        foreach (var s in soldiers)
            _position += s.position;
        _position /= numOfSoldiers;
    }

    protected Point centroid;
    protected Coordinate[] coords;
    protected Vector3 c, cFront;
    private void UpdateMeleeGeom()
    {
        centroid = meleeGeom.Centroid;
        coords = meleeGeom.Coordinates;

        c = new Vector3((float)centroid.X, 0, (float)centroid.Y);
        int i = coords.Length / 2;
        cFront = new Vector3((float)coords[i].X * 0.5f + (float)coords[i + 1].X * 0.5f, 0,
            (float)coords[i].Y * 0.5f + (float)coords[i + 1].Y * 0.5f);
        var deg = Vector3.SignedAngle(cFront - c, GetVector3Down(targetDirection), Vector3.up);

        //if(Mathf.Abs(deg) > 1 || Mathf.Abs(position.x - c.x) > 1 || Mathf.Abs(position.z - c.z) > 1)
        meleeGeom = UpdateGeometry(meleeGeom, position.x - c.x, position.z - c.z, deg);
    }

    










}
