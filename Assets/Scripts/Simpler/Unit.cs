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



    [System.Serializable]
    public class Soldier
    {
        public GameObject go;
        public Rigidbody rb;

        public bool isCharging;
        public float distFromFront;
        public UnitStats stats;
        public Vector3 targetPos;
        public Vector3 targetLookAt;
        public Dictionary<Soldier, float> soldiersFightingAgainstDistance = new Dictionary<Soldier, float>();
        public Vector3 frontPos
        {
            get
            {
                return front.position;
            }
        }
        public Vector3 rightPos
        {
            get
            {
                return right.position;
            }
        }
        public Vector3 leftPos
        {
            get
            {
                return left.position;
            }
        }
        public Vector3 position
        {
            get
            {
                return go.transform.position;
            }
        }
        public Vector3 boxCastCenter
        {
            get
            {
                return position + go.transform.forward * 2 * distFromFront + Vector3.up * 0.25f;
            }
        }
        public Vector3 boxCastHalfExtends
        {
            get
            {
                return Vector3.one * 0.3f + Vector3.forward - Vector3.up * 0.2f;
            }
        }


        private readonly Transform front, right, left;
        private readonly Unit unit;

        public Soldier(GameObject g, Unit unit)
        {
            this.stats = unit.stats;
            this.unit = unit;
            go = g;
            rb = go.GetComponent<Rigidbody>();
            front = go.transform.GetChild(1); // get the transform of the front handler
            right = go.transform.GetChild(2); // get the transform of the front handler
            left = go.transform.GetChild(3); // get the transform of the front handler
            distFromFront = Vector3.Distance(frontPos, position);
        }
        public void Move()
        {
            Vector3 p = rb.position;

            if (rb.velocity.magnitude < stats.topSpeed)
            {
                float dt = 0.02f;

                Vector3 v = rb.velocity;

                Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;  // TODO : Damping is to taken into account

                rb.AddForce(Vector3.ClampMagnitude(force, stats.movementForce),
                            isCharging ? ForceMode.Impulse : ForceMode.Force);


                if (unit.isInFight)
                {
                    var rotation = Quaternion.LookRotation(targetLookAt - position);
                    go.transform.rotation = Quaternion.Lerp(go.transform.rotation, rotation, Time.deltaTime);
                }
                else
                    go.transform.LookAt(targetLookAt);  
            }

        }


        //public void Move(float speed, float distance)
        //{
        //    if (rb.velocity.magnitude < stats.topSpeed)
        //    {
        //        float dt = distance / speed;

        //        Vector3 p = rb.position;
        //        Vector3 v = rb.velocity;

        //        Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;  // TODO : Damping is to taken into account

        //        rb.AddForce(isCharging ? Vector3.ClampMagnitude(force, stats.chargeForce) : Vector3.ClampMagnitude(force, stats.movementForce),
        //                    isCharging ? ForceMode.Impulse : ForceMode.Force);

        //        go.transform.LookAt(GetVector3Down(targetPos) * Random.Range(0.99f, 1.01f) + Vector3.up * 0.5f);  // TODO : Remove this hard coding here
        //    }
        //}

    }

    [System.Serializable]
    public class UnitStats
    {
        public float topSpeed, movementForce, pathSpeed, soldierDistLateral, soldierDistVertical;
        public int startingCols, startingNumOfSoldiers;
    }
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
    public int numOfSoldiers;
    [HideInInspector()]
    public bool hullAlreadyUpdated;
    [HideInInspector()]
    public GameObject[][] soldiersInFormation;


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
                    var color = transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color;
                    color.b = 0.21f;
                    for (int i = 1; i < transform.childCount; i++)
                        transform.GetChild(i).GetChild(0).GetComponent<MeshRenderer>().material.color = color;
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

        numOfSoldiers = stats.startingNumOfSoldiers;
        cols = stats.startingCols;
        points = new Point[numOfSoldiers];

        CleanUnit();

        Instantiate(pathCreatorGO, transform.position, Quaternion.identity, transform).GetComponent<PathCreator>();


        var res = GetFormationAtPos(transform.position, transform.forward, numOfSoldiers, cols, stats.soldierDistLateral, stats.soldierDistVertical);
        _soldiers = new Soldier[numOfSoldiers];
        GameObject g;
        int k = 0;
        foreach (var v in res)
        {
            g = Instantiate(soldierBase, v, transform.rotation, transform);
            g.layer = LayerMask.NameToLayer(soldierLayerName);
            _soldiers[k++] = new Soldier(g, this);
        }
    }

    public Geometry _hull;
    private HashSet<Soldier> _soldiersFightingAgainst;
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
    private void Start()
    {
        army = transform.parent.GetComponent<Army>();

        if (!Application.isPlaying)
            bfightingUnit = army.enemy.transform.GetComponentsInChildren<Unit>().ToDictionary(u => u, u => false);

        combactManager = army.combactManager;

        _soldiersFightingAgainst = new HashSet<Soldier>();
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
        //_position = _soldiers.Aggregate(Vector3.zero, (acc, s) => acc + s.go.transform.position) / _soldiers.Length;
        _position = Vector3.zero;
        foreach (var s in soldiers)
            _position += s.position;
        _position /= numOfSoldiers;
    }







}
