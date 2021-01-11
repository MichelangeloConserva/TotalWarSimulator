using DbscanImplementation;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using NetTopologySuite.Operation.Union;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
    public List<MeleeStats> infantryStats;
    public List<MeleeStats> cavalryStats;
    public List<RangedUnitStats> archersStats;

    public float epsClust = 8;
    public float expansion = 1f;
    public bool DEBUG_MODE;

    public List<UnitNew> units;
    public List<List<UnitNew>> partions;


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
        {
            InstantiateArmy(true);
            partions = new List<List<UnitNew>>();
            partions.Add(units);
        }
        else
        {
            //CheckPartitions();
        }

    }


    List<UnitNew> tempList = new List<UnitNew>();
    Vector3 clusterPos;
    int totalSoldiers;
    int meleeSoldiers;
    int rangedSoldies;
    float clusterWidth;
    UnitNew u;
    Polygon geom;
    private void CheckPartitions()
    {
        tempList.Clear();

        GUI.color = Color.yellow;


        var simpleDbscan = new DbscanAlgorithm<Tuple<UnitNew,Vector3>>((v1, v2) => Vector3.SqrMagnitude(v1.Item2-v2.Item2));
        var result = simpleDbscan.ComputeClusterDbscan(units.Select(u => new Tuple<UnitNew, Vector3>(u, u.position)).ToArray(), epsilon: epsClust, minimumPoints: 1);




        foreach (var p in result.Clusters.Values)
        {
            clusterPos = Vector3.zero;
            totalSoldiers = 0;


            geom = (Polygon)new ConvexHull(p.SelectMany(u => u.Feature.Item1.rectangle).ToArray(), GeometryFactory.Default).GetConvexHull();
            var coords = geom.Coordinates;
            for (int j = 0; j < coords.Length - 1; j++)
            {
                Gizmos.DrawLine(new Vector3((float)coords[j].X, 5, (float)coords[j].Y),
                                new Vector3((float)coords[j + 1].X, 5, (float)coords[j + 1].Y));
            }




            int i = 0;
            foreach (var tuple in p)
            {
                u = tuple.Feature.Item1;
                clusterPos += tuple.Feature.Item2;
                totalSoldiers += u.numOfSoldiers;




                //clusterWidth += u.width;

                if (i++ != 0)
                {





                }


            }

            clusterPos = clusterPos / p.Count + Vector3.up * 5;

            Handles.Label(clusterPos, 
                "# Soldier: " + totalSoldiers + "\n" +
                "Perimeter/Sold coedd: " + (geom.Length / totalSoldiers).ToString("F2")
                );



            Gizmos.DrawSphere(clusterPos, 0.5f);
        }










    }








    internal void RemoveUnit(UnitNew u)
    {
        units.Remove(u);
        if (u.GetType() == typeof(ArcherNew))
            archerUnits.Remove((ArcherNew)u);
        else
        {
            if (infantryUnits.Contains(u))
                infantryUnits.Remove(u);
            else
                cavalryUnits.Remove(u);
        }

    }

    public List<UnitNew> infantryUnits, cavalryUnits;
    public List<ArcherNew> archerUnits;


    public void OnDestroy()
    {
        if (File.Exists("army" + (int)role + ".txt"))
            File.Delete("army" + (int)role + ".txt");
    }


    private void WriteDownArmySpec()
    {
        using (StreamWriter writetext = File.AppendText("army" + (int)role + ".txt"))
        {
            writetext.WriteLine(infantryStats.Count);
            writetext.WriteLine(archersStats.Count);
            writetext.WriteLine(cavalryStats.Count);
            writetext.Close();
        }
    }



    public void InstantiateArmy(bool debug = false)
    {
        WriteDownArmySpec();

        units = new List<UnitNew>(infantryStats.Count + archersStats.Count + cavalryStats.Count);
        infantryUnits = new List<UnitNew>(infantryStats.Count);
        archerUnits = new List<ArcherNew>(archersStats.Count);
        cavalryUnits = new List<UnitNew>(cavalryStats.Count);

        float infantryLineDepth = 2 * GetHalfLenght(infantryStats.First().meleeHolder.soldierDistVertical, infantryStats.First().meleeHolder.startingCols);

        float infantryLineLength = 0;
        foreach (var i in infantryStats)
            infantryLineLength += 2 * GetHalfLenght(i.meleeHolder.soldierDistLateral, i.meleeHolder.startingNumOfSoldiers / i.meleeHolder.startingCols);

        Vector3 start = transform.position - transform.right * infantryLineLength;
        Vector3 end = transform.position + transform.right * infantryLineLength;

        float c = infantryStats.Count - 1;
        for (int i = 0; i < infantryStats.Count; i++)
        {
            Gizmos.color = Color.green;
            Vector3 curPos = start * (i / c) + end * (1 - i / c);
            var u = infantryStats.ElementAt(i);
            if (debug)
                DrawMeleeAtPos(curPos, u);
            else
                AddMeleeAtPos(curPos, "Infantry", u, i, infantryUnits);

        }


        Vector3 leftCavPos = start - transform.right * 5 * infantryLineLength / infantryStats.Count - transform.forward * infantryLineDepth;
        Vector3 RightCavPos = end + transform.right * 5 * infantryLineLength / infantryStats.Count - transform.forward * infantryLineDepth;
        var leftCav = cavalryStats.First();
        var rightCav = cavalryStats.Last();

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
        foreach (var i in archersStats)
        {
            var a = i.meleeStats;
            archersLineLength += 2 * GetHalfLenght(a.meleeHolder.soldierDistLateral, a.meleeHolder.startingNumOfSoldiers / a.meleeHolder.startingCols);
        }

        start = transform.position - transform.right * archersLineLength + transform.forward * 2 * infantryLineDepth;
        end = transform.position + transform.right * archersLineLength + transform.forward * 2 * infantryLineDepth;
        c = archersStats.Count - 1;
        for (int i = 0; i < archersStats.Count; i++)
        {
            Vector3 curPos = start * (i / c) + end * (1 - i / c);
            var u = archersStats.ElementAt(i);

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

    private void AddMeleeAtPos(Vector3 pos, string name, MeleeStats u, int i, List<UnitNew> list)
    {
        var curUnit = new GameObject(name+" (" + i + ")");
        curUnit.transform.position = pos;
        curUnit.transform.rotation = Quaternion.Euler(transform.forward);
        curUnit.transform.parent = transform;

        var unitMB = curUnit.AddComponent<UnitNew>();
        unitMB.Instantiate(pos, transform.forward, u.meleeHolder, soldiersHolder, u.soldierPrefab, this);
        unitMB.ID = i;
        list.Add(unitMB);
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

        var unitMB = AddArcherComponent(curUnit, pos, u.meleeStats.meleeHolder, u.meleeStats.soldierPrefab, u.rangedStats.rangedHolder, u.rangedStats.arrow);
        unitMB.ID = i;
        AddRangedCollider(curUnit, unitMB, u.rangedStats.rangedHolder.range);

        float frontExp = CalculateFrontalExpansion(u.meleeStats.meleeHolder.soldierDistVertical, u.meleeStats.meleeHolder.startingNumOfSoldiers, u.meleeStats.meleeHolder.startingCols, expansion);
        float latExp = CalculateLateralExpansion(u.meleeStats.meleeHolder.soldierDistLateral, u.meleeStats.meleeHolder.startingCols, expansion);
        AddMeleeCollider(curUnit, unitMB, latExp, frontExp);


        var sc = Instantiate(selectionCirclePrefab, curUnit.transform);
        unitMB.rangeShader = sc.GetComponent<Projector>();
        unitMB.rangeShader.orthographicSize = u.rangedStats.rangedHolder.range + 5;
        unitMB.rangeShader.enabled = false;

        archerUnits.Add(unitMB);

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
