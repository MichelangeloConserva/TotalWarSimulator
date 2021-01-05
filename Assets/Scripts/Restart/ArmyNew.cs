using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MeleeStats;
using static RangedStats;
using static Utils;


[System.Serializable]
public struct RangedUnitStats
{
    public MeleeStats meleeStats;
    public RangedStats rangedStats;
}




public class ArmyNew : MonoBehaviour
{

    public ArmyRole role;
    public GameObject selectionCirclePrefab;
    public CombactManager combactManager;
    public Transform soldiersHolder;
    public Transform pathCreatorsHolder;
    public ArmyNew enemy;

    [Header("ArmyComposition")]
    public List<MeleeStats> infantry;
    public List<MeleeStats> cavalry;
    public List<RangedUnitStats> archers;

    public float expansion = 1f;
    public bool DEBUG_MODE;

    public List<UnitNew> units;

    private Vector3[] res;


    public string enemySoldierLayer
    {
        get { return "unitSoldier" + ((int)enemy.role + 1); }
    }
    public string allyUnitLayer
    {
        get { return "unit" + ((int)role + 1); }
    }



    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
            InstantiateArmy(true);
    }



    public GameObject[] infantryUnits, archerUnits, cavalryUnits;

    public void InstantiateArmy(bool debug = false)
    {
        units = new List<UnitNew>(infantry.Count + archers.Count + cavalry.Count);
        infantryUnits = new GameObject[infantry.Count];
        archerUnits = new GameObject[archers.Count];
        cavalryUnits = new GameObject[cavalry.Count];

        float infantryLineDepth = 2 * GetHalfLenght(infantry.First().meleeHolder.soldierDistVertical, infantry.First().meleeHolder.startingCols);

        float infantryLineLength = 0;
        foreach (var i in infantry)
            infantryLineLength += 2 * GetHalfLenght(i.meleeHolder.soldierDistLateral, i.meleeHolder.startingNumOfSoldiers / i.meleeHolder.startingCols);

        Vector3 start = transform.position - transform.right * infantryLineLength;
        Vector3 end = transform.position + transform.right * infantryLineLength;

        float c = infantry.Count - 1;
        for (int i = 0; i < infantry.Count; i++)
        {
            Gizmos.color = Color.green;
            Vector3 curPos = start * (i / c) + end * (1 - i / c);
            var u = infantry.ElementAt(i);
            if (debug)
                DrawMeleeAtPos(curPos, u);
            else
                AddMeleeAtPos(curPos, "Infantry", u, i, infantryUnits);

        }


        Vector3 leftCavPos = start - transform.right * 3 * infantryLineLength / infantry.Count - transform.forward * infantryLineDepth;
        Vector3 RightCavPos = end + transform.right * 3 * infantryLineLength / infantry.Count - transform.forward * infantryLineDepth;
        var leftCav = cavalry.First();
        var rightCav = cavalry.Last();

        if (debug)
        {
            Gizmos.color = Color.blue;
            DrawMeleeAtPos(RightCavPos, rightCav);
            Gizmos.color = Color.blue;
            DrawMeleeAtPos(leftCavPos, leftCav);
        }
        else
        {
            AddMeleeAtPos(leftCavPos, "Cavalry", leftCav, 0, cavalryUnits);
            AddMeleeAtPos(RightCavPos, "Cavalry", rightCav, 1, cavalryUnits);

        }




        float archersLineLength = 0;
        foreach (var i in archers)
        {
            var a = i.meleeStats;
            archersLineLength += 2 * GetHalfLenght(a.meleeHolder.soldierDistLateral, a.meleeHolder.startingNumOfSoldiers / a.meleeHolder.startingCols);
        }

        start = transform.position - transform.right * archersLineLength + transform.forward * 2 * infantryLineDepth;
        end = transform.position + transform.right * archersLineLength + transform.forward * 2 * infantryLineDepth;
        c = archers.Count - 1;
        for (int i = 0; i < archers.Count; i++)
        {
            Vector3 curPos = start * (i / c) + end * (1 - i / c);
            var u = archers.ElementAt(i);

            if (debug)
                DrawArcherAtPos(curPos, u);
            else
                AddArcherAtPos(curPos, u, i);
        }


    }


    private void DrawMeleeAtPos(Vector3 pos, MeleeStats u)
    {
        res = GetFormationAtPos(pos, transform.forward, u.meleeHolder.startingNumOfSoldiers, u.meleeHolder.startingCols, u.meleeHolder.soldierDistLateral, u.meleeHolder.soldierDistVertical);
        foreach (var p in res)
            Gizmos.DrawSphere(p + Vector3.up * 2, 0.4f);

        Gizmos.color = Color.red;

        float curLength = GetHalfLenght(u.meleeHolder.soldierDistLateral, u.meleeHolder.startingCols);
        float frontExp = GetHalfLenght(u.meleeHolder.soldierDistVertical, u.meleeHolder.startingNumOfSoldiers / u.meleeHolder.startingCols) + expansion;
        float latExp = curLength + expansion;

        Gizmos.color = Color.red;
        Vector3 frontLeft = pos + transform.forward * frontExp - transform.right * latExp;
        Vector3 frontRight = pos + transform.forward * frontExp + transform.right * latExp;
        Vector3 backLeft = pos - transform.forward * frontExp - transform.right * latExp;
        Vector3 backRight = pos - transform.forward * frontExp + transform.right * latExp;
        Gizmos.DrawLine(backLeft + Vector3.up, backRight + Vector3.up);
        Gizmos.DrawLine(frontRight + Vector3.up, frontLeft + Vector3.up);
        Gizmos.DrawLine(backLeft + Vector3.up, frontLeft + Vector3.up);
        Gizmos.DrawLine(backRight + Vector3.up, frontRight + Vector3.up);
    }

    private void AddMeleeAtPos(Vector3 pos, string name, MeleeStats u, int i, GameObject[] list)
    {
        var curUnit = new GameObject(name+" (" + i + ")");
        curUnit.transform.position = pos;
        curUnit.transform.rotation = Quaternion.Euler(transform.forward);
        curUnit.transform.parent = transform;

        var unitMB = curUnit.AddComponent<UnitNew>();
        unitMB.Instantiate(pos, transform.forward, u.meleeHolder, soldiersHolder, u.soldierPrefab, this);
        list[i] = curUnit;
        units.Add(unitMB);

        var frontExp = CalculateFrontalExpansion(u.meleeHolder.soldierDistVertical, u.meleeHolder.startingNumOfSoldiers, u.meleeHolder.startingCols, expansion);
        var latExp = CalculateLateralExpansion(u.meleeHolder.soldierDistLateral, u.meleeHolder.startingCols, expansion);
        AddMeleeCollider(curUnit, unitMB, latExp, frontExp);
    }



    private void DrawArcherAtPos(Vector3 pos, RangedUnitStats u)
    {
        res = GetFormationAtPos(pos, transform.forward, u.meleeStats.meleeHolder.startingNumOfSoldiers, u.meleeStats.meleeHolder.startingCols, u.meleeStats.meleeHolder.soldierDistLateral, u.meleeStats.meleeHolder.soldierDistVertical);
        Gizmos.color = Color.cyan;
        foreach (var p in res)
            Gizmos.DrawSphere(p + Vector3.up * 2, 0.4f);

        float curLength = GetHalfLenght(u.meleeStats.meleeHolder.soldierDistLateral, u.meleeStats.meleeHolder.startingCols);
        float frontExp = GetHalfLenght(u.meleeStats.meleeHolder.soldierDistVertical, u.meleeStats.meleeHolder.startingNumOfSoldiers / u.meleeStats.meleeHolder.startingCols) + expansion;
        float latExp = curLength + expansion;

        Gizmos.color = Color.red;
        Vector3 frontLeft = pos + transform.forward * frontExp - transform.right * latExp;
        Vector3 frontRight = pos + transform.forward * frontExp + transform.right * latExp;
        Vector3 backLeft = pos - transform.forward * frontExp - transform.right * latExp;
        Vector3 backRight = pos - transform.forward * frontExp + transform.right * latExp;
        Gizmos.DrawLine(backLeft + Vector3.up, backRight + Vector3.up);
        Gizmos.DrawLine(frontRight + Vector3.up, frontLeft + Vector3.up);
        Gizmos.DrawLine(backLeft + Vector3.up, frontLeft + Vector3.up);
        Gizmos.DrawLine(backRight + Vector3.up, frontRight + Vector3.up);

        DrawGizmoDisk(pos, u.rangedStats.rangedHolder.range);
    }

    private void AddArcherAtPos(Vector3 pos, RangedUnitStats u, int i)
    {
        var curUnit = new GameObject("Archer (" + i + ")");
        curUnit.transform.position = pos;
        curUnit.transform.rotation = Quaternion.LookRotation(transform.forward);
        curUnit.transform.parent = transform;
        archerUnits[i] = curUnit;

        var unitMB = AddArcherComponent(curUnit, pos, u.meleeStats.meleeHolder, u.meleeStats.soldierPrefab, u.rangedStats.rangedHolder, u.rangedStats.arrow);

        AddRangedCollider(curUnit, unitMB, u.rangedStats.rangedHolder.range);

        float frontExp = CalculateFrontalExpansion(u.meleeStats.meleeHolder.soldierDistVertical, u.meleeStats.meleeHolder.startingNumOfSoldiers, u.meleeStats.meleeHolder.startingCols, expansion);
        float latExp = CalculateLateralExpansion(u.meleeStats.meleeHolder.soldierDistLateral, u.meleeStats.meleeHolder.startingCols, expansion);
        AddMeleeCollider(curUnit, unitMB, latExp, frontExp);


        var sc = Instantiate(selectionCirclePrefab, curUnit.transform);
        unitMB.rangeShader = sc.GetComponent<Projector>();
        unitMB.rangeShader.orthographicSize = u.rangedStats.rangedHolder.range + 5;
        unitMB.rangeShader.enabled = false;
    }


    private ArcherNew AddArcherComponent(GameObject go, Vector3 pos, MeleeStatsHolder meleeHolder, GameObject soldierPrefab, RangedStatsHolder rangedHolder, GameObject arrow)
    {
        var unitMB = go.AddComponent<ArcherNew>();
        unitMB.Instantiate(pos, transform.forward, meleeHolder, soldiersHolder, soldierPrefab, this);
        units.Add(unitMB);
        unitMB.range = rangedHolder.range;
        unitMB.arrowDamage = rangedHolder.arrowDamage;
        unitMB.fireInterval = rangedHolder.fireInterval;
        unitMB.freeFire = rangedHolder.freeFire;
        unitMB.arrow = arrow;
        return unitMB;
    }
    
    private void AddRangedCollider(GameObject go, ArcherNew u, float range)
    {
        var rangedCollider = new GameObject("RangedCollider");
        rangedCollider.transform.parent = go.transform;
        rangedCollider.transform.localPosition = Vector3.zero;
        rangedCollider.AddComponent<RangedCollider>().unit = u;
        rangedCollider.layer = LayerMask.NameToLayer(allyUnitLayer);
        var cColl = rangedCollider.AddComponent<SphereCollider>();
        cColl.radius = range;
        cColl.isTrigger = true;
        var rb = rangedCollider.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void AddMeleeCollider(GameObject go, UnitNew u, float latExp, float frontExp)
    {
        // TODO : find a better place to instantiate the LineRenderer
        u.lr = go.AddComponent<LineRenderer>();
        u.lr.enabled = false;


        var meleeCollider = new GameObject("MeleeCollider");
        meleeCollider.tag = "Melee";
        meleeCollider.transform.parent = go.transform;
        meleeCollider.transform.localPosition = Vector3.zero;
        meleeCollider.AddComponent<MeleeCollider>().unit = u;
        meleeCollider.layer = LayerMask.NameToLayer(allyUnitLayer);
        var bColl = meleeCollider.AddComponent<BoxCollider>();
        bColl.size = new Vector3(2 * latExp, 2, 2 * frontExp);
        bColl.isTrigger = true;
        var rb = meleeCollider.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }









}
