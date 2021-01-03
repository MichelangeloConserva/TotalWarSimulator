using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public CombactManager combactManager;
    public ArmyNew enemy;
    public Transform soldiersHolder;

    [Header("ArmyComposition")]
    public List<MeleeStats> infantry;
    public List<MeleeStats> cavalry;
    public List<RangedUnitStats> archers;


    private Vector3[] res;


    public string enemySoldierLayer
    {
        get { return "unitSoldier" + ((int)role + 1); }
    }



    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
            InstantiateArmy(true);
    }



    public GameObject[] infantryUnits, archerUnits, cavalryUnits;

    public void InstantiateArmy(bool debug = false)
    {
        infantryUnits = new GameObject[infantry.Count];
        archerUnits = new GameObject[archers.Count];
        cavalryUnits = new GameObject[cavalry.Count];
        float expansion = 1f;

        float infantryLineDepth = 2 * GetHalfLenght(infantry.First().meleeHolder.soldierDistVertical, infantry.First().meleeHolder.startingCols);

        float infantryLineLength = 0;
        foreach (var i in infantry)
            infantryLineLength += 2 * GetHalfLenght(i.meleeHolder.soldierDistLateral, i.meleeHolder.startingNumOfSoldiers / i.meleeHolder.startingCols);

        Vector3 start = transform.position - transform.right * infantryLineLength;
        Vector3 end = transform.position + transform.right * infantryLineLength;

        float c = infantry.Count - 1;
        for (int i = 0; i < infantry.Count; i++)
        {
            Vector3 curPos = start * (i / c) + end * (1 - i / c);
            var u = infantry.ElementAt(i).meleeHolder;
            if (debug)
            {
                res = GetFormationAtPos(curPos, transform.forward, u.startingNumOfSoldiers, u.startingCols, u.soldierDistLateral, u.soldierDistVertical);
                Gizmos.color = Color.green;
                foreach (var p in res)
                    Gizmos.DrawSphere(p + Vector3.up * 2, 0.4f);

                Gizmos.color = Color.red;

                float curLength = GetHalfLenght(u.soldierDistLateral, u.startingCols);
                float frontExp = GetHalfLenght(u.soldierDistVertical, u.startingNumOfSoldiers / u.startingCols) + expansion;
                float latExp = curLength + expansion;

                Gizmos.color = Color.red;
                Vector3 frontLeft = curPos + transform.forward * frontExp - transform.right * latExp;
                Vector3 frontRight = curPos + transform.forward * frontExp + transform.right * latExp;
                Vector3 backLeft = curPos - transform.forward * frontExp - transform.right * latExp;
                Vector3 backRight = curPos - transform.forward * frontExp + transform.right * latExp;
                Gizmos.DrawLine(backLeft + Vector3.up, backRight + Vector3.up);
                Gizmos.DrawLine(frontRight + Vector3.up, frontLeft + Vector3.up);
                Gizmos.DrawLine(backLeft + Vector3.up, frontLeft + Vector3.up);
                Gizmos.DrawLine(backRight + Vector3.up, frontRight + Vector3.up);

            }
            else
            {
                var curUnit = new GameObject("Infantry (" + i + ")");
                curUnit.transform.position = curPos;
                curUnit.transform.rotation = Quaternion.Euler(transform.forward);
                curUnit.transform.parent = transform;

                float curLength = GetHalfLenght(u.soldierDistLateral, u.startingCols);
                float frontExp = GetHalfLenght(u.soldierDistVertical, u.startingNumOfSoldiers / u.startingCols) + expansion;
                float latExp = curLength + expansion;

                var bColl = curUnit.AddComponent<BoxCollider>();
                bColl.size = new Vector3(2*latExp, 2, 2*frontExp);
                bColl.isTrigger = true;



                var unitMB = curUnit.AddComponent<UnitNew>();
                unitMB.Instantiate(curPos, transform.forward, infantry.ElementAt(i).meleeHolder, soldiersHolder, infantry.ElementAt(i).soldierPrefab, this);
                
                infantryUnits[i] = curUnit;
            }

        }


        Vector3 leftCavPos = start - transform.right * 3 * infantryLineLength / infantry.Count - transform.forward * infantryLineDepth;
        Vector3 RightCavPos = end + transform.right * 3 * infantryLineLength / infantry.Count - transform.forward * infantryLineDepth;

        var leftCav = cavalry.First().meleeHolder;
        var rightCav = cavalry.Last().meleeHolder;

        if (debug)
        {
            res = GetFormationAtPos(leftCavPos, transform.forward, leftCav.startingNumOfSoldiers, leftCav.startingCols, leftCav.soldierDistLateral, leftCav.soldierDistVertical);
            Gizmos.color = Color.blue;
            foreach (var p in res)
                Gizmos.DrawSphere(p + Vector3.up * 2, 0.4f);
            
            res = GetFormationAtPos(RightCavPos, transform.forward, rightCav.startingNumOfSoldiers, rightCav.startingCols, rightCav.soldierDistLateral, rightCav.soldierDistVertical);
            Gizmos.color = Color.blue;
            foreach (var p in res)
                Gizmos.DrawSphere(p + Vector3.up * 2, 0.4f);



            Gizmos.color = Color.red;

            float curLength = GetHalfLenght(leftCav.soldierDistLateral, leftCav.startingCols);
            float frontExp = GetHalfLenght(leftCav.soldierDistVertical, leftCav.startingNumOfSoldiers / leftCav.startingCols) + expansion;
            float latExp = curLength + expansion;

            Gizmos.color = Color.red;
            Vector3 frontLeft = leftCavPos + transform.forward * frontExp - transform.right * latExp;
            Vector3 frontRight = leftCavPos + transform.forward * frontExp + transform.right * latExp;
            Vector3 backLeft = leftCavPos - transform.forward * frontExp - transform.right * latExp;
            Vector3 backRight = leftCavPos - transform.forward * frontExp + transform.right * latExp;
            Gizmos.DrawLine(backLeft + Vector3.up, backRight + Vector3.up);
            Gizmos.DrawLine(frontRight + Vector3.up, frontLeft + Vector3.up);
            Gizmos.DrawLine(backLeft + Vector3.up, frontLeft + Vector3.up);
            Gizmos.DrawLine(backRight + Vector3.up, frontRight + Vector3.up);

            curLength = GetHalfLenght(rightCav.soldierDistLateral, rightCav.startingCols);
            frontExp = GetHalfLenght(rightCav.soldierDistVertical, rightCav.startingNumOfSoldiers / rightCav.startingCols) + expansion;
            latExp = curLength + expansion;

            Gizmos.color = Color.red;
            frontLeft = RightCavPos + transform.forward * frontExp - transform.right * latExp;
            frontRight = RightCavPos + transform.forward * frontExp + transform.right * latExp;
            backLeft = RightCavPos - transform.forward * frontExp - transform.right * latExp;
            backRight = RightCavPos - transform.forward * frontExp + transform.right * latExp;
            Gizmos.DrawLine(backLeft + Vector3.up, backRight + Vector3.up);
            Gizmos.DrawLine(frontRight + Vector3.up, frontLeft + Vector3.up);
            Gizmos.DrawLine(backLeft + Vector3.up, frontLeft + Vector3.up);
            Gizmos.DrawLine(backRight + Vector3.up, frontRight + Vector3.up);

        }
        else
        {
            float curLength = GetHalfLenght(leftCav.soldierDistLateral, leftCav.startingCols);
            float frontExp = GetHalfLenght(leftCav.soldierDistVertical, leftCav.startingNumOfSoldiers / leftCav.startingCols) + expansion;
            float latExp = curLength + expansion;
            var curUnit = new GameObject("Cavalry (0)");
            curUnit.transform.position = leftCavPos;
            curUnit.transform.rotation = Quaternion.Euler(transform.forward);
            curUnit.transform.parent = transform;
            var bColl = curUnit.AddComponent<BoxCollider>();
            bColl.size = new Vector3(2 * latExp, 2, 2 * frontExp);
            bColl.isTrigger = true;
            var unitMB = curUnit.AddComponent<UnitNew>();
            unitMB.Instantiate(leftCavPos, transform.forward, cavalry.First().meleeHolder, soldiersHolder, cavalry.First().soldierPrefab, this);
            cavalryUnits[0] = curUnit;


            curLength = GetHalfLenght(rightCav.soldierDistLateral, rightCav.startingCols);
            frontExp = GetHalfLenght(rightCav.soldierDistVertical, rightCav.startingNumOfSoldiers / rightCav.startingCols) + expansion;
            latExp = curLength + expansion;
            curUnit = new GameObject("Cavalry (1)");
            curUnit.transform.position = RightCavPos;
            curUnit.transform.rotation = Quaternion.Euler(transform.forward);
            curUnit.transform.parent = transform;
            bColl = curUnit.AddComponent<BoxCollider>();
            bColl.size = new Vector3(2 * latExp, 2, 2 * frontExp);
            bColl.isTrigger = true;
            unitMB = curUnit.AddComponent<UnitNew>();
            unitMB.Instantiate(RightCavPos, transform.forward, cavalry.Last().meleeHolder, soldiersHolder, cavalry.Last().soldierPrefab, this);
            cavalryUnits[0] = curUnit;
        }

        float archerLenght = 2 * GetHalfLenght(archers.First().meleeStats.meleeHolder.soldierDistVertical, archers.First().meleeStats.meleeHolder.startingCols);
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
            var u = archers.ElementAt(i).meleeStats;

            if (debug)
            {
                res = GetFormationAtPos(curPos, transform.forward, u.meleeHolder.startingNumOfSoldiers, u.meleeHolder.startingCols, u.meleeHolder.soldierDistLateral, u.meleeHolder.soldierDistVertical);
                Gizmos.color = Color.cyan;
                foreach (var p in res)
                    Gizmos.DrawSphere(p + Vector3.up * 2, 0.4f); 

                float curLength = GetHalfLenght(u.meleeHolder.soldierDistLateral, u.meleeHolder.startingCols);
                float frontExp = GetHalfLenght(u.meleeHolder.soldierDistVertical, u.meleeHolder.startingNumOfSoldiers / u.meleeHolder.startingCols) + expansion;
                float latExp = curLength + expansion;

                Gizmos.color = Color.red;
                Vector3 frontLeft = curPos + transform.forward * frontExp - transform.right * latExp;
                Vector3 frontRight = curPos + transform.forward * frontExp + transform.right * latExp;
                Vector3 backLeft = curPos - transform.forward * frontExp - transform.right * latExp;
                Vector3 backRight = curPos - transform.forward * frontExp + transform.right * latExp;
                Gizmos.DrawLine(backLeft + Vector3.up, backRight + Vector3.up);
                Gizmos.DrawLine(frontRight + Vector3.up, frontLeft + Vector3.up);
                Gizmos.DrawLine(backLeft + Vector3.up, frontLeft + Vector3.up);
                Gizmos.DrawLine(backRight + Vector3.up, frontRight + Vector3.up);
            }
            else
            {
                var curUnit = new GameObject("Archer (" + i + ")");
                curUnit.transform.position = curPos;
                curUnit.transform.rotation = Quaternion.LookRotation(transform.forward);
                curUnit.transform.parent = transform;

                float curLength = GetHalfLenght(u.meleeHolder.soldierDistLateral, u.meleeHolder.startingCols);
                float frontExp = GetHalfLenght(u.meleeHolder.soldierDistVertical, u.meleeHolder.startingNumOfSoldiers / u.meleeHolder.startingCols) + expansion;
                float latExp = curLength + expansion;
                var bColl = curUnit.AddComponent<BoxCollider>();
                bColl.size = new Vector3(2*latExp, 2, 2*frontExp);
                bColl.isTrigger = true;

                var unitMB = curUnit.AddComponent<ArcherNew>();
                unitMB.Instantiate(curPos, transform.forward, u.meleeHolder, soldiersHolder, u.soldierPrefab, this);

                archerUnits[i] = curUnit;
            }

        }


    }







    private void Start()
    {
        InstantiateArmy();
    }









}
