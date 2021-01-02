using NetTopologySuite.Geometries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Utils;

public struct Matrix2DIndices<K, T>
{
    public static T[] GetCol(T[,] matrix, int columnNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(0))
                .Select(x => matrix[x, columnNumber])
                .ToArray();
    }
    public static T[] GetRow(T[,] matrix, int rowNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
    }

    public static Dictionary<K, T> GetRow(T[,] matrix, int rowNumber, Dictionary<K, int> otherIndices)
    {
        return otherIndices.ToDictionary(kv => kv.Key, kv => matrix[rowNumber, kv.Value]);
    }
    public static Dictionary<K, T> GetCol(T[,] matrix, int colNumber, Dictionary<K, int> otherIndices)
    {
        return otherIndices.ToDictionary(kv => kv.Key, kv => matrix[kv.Value, colNumber]);
    }



    private Dictionary<K, int> indices1, indices2;
    private T[,] values;


    public void SetValue(K k1, K k2, T value)
    {
        values[indices1[k1], indices2[k2]] = value;
    }
    public T GetValue(K k1, K k2)
    {
        if (indices1.ContainsKey(k1))
            return values[indices1[k1], indices2[k2]];
        return values[indices2[k1], indices1[k2]];
    }
    public Dictionary<K,T> GetDictValue(K k)
    {
        return indices1.ContainsKey(k) ? GetRow(values, indices1[k], indices2) : GetCol(values, indices2[k], indices1);
    }
    public T[] GetArrayValue(K k)
    {
        return indices1.ContainsKey(k) ? GetRow(values, indices1[k]) : GetCol(values, indices2[k]);
    }
    public void Remove(K k)
    {
        if (indices1.ContainsKey(k))
        {
            int oldInd = indices1[k];

            T[,] newValues = new T[indices1.Count, indices2.Count];
            foreach (int row in indices1.Values)
                foreach (int col in indices2.Values)
                    newValues[row > oldInd ? row - 1 : row, col] = values[row, col];
            values = newValues;

            foreach (var kk in indices1.Keys.ToList())
                if (indices1[kk] > oldInd)
                    indices1[kk] -= 1;
            indices1.Remove(k);
        }
        else
        {
            int oldInd = indices2[k];

            T[,] newValues = new T[indices1.Count, indices2.Count];
            foreach (int row in indices1.Values)
                foreach (int col in indices2.Values)
                    newValues[row, col > oldInd ? col - 1 : col] = values[row, col];
            values = newValues;

            foreach (var kk in indices2.Keys.ToList())
                if (indices2[kk] > oldInd)
                    indices2[kk] -= 1;

            indices2.Remove(k);
        }

        
    }

    public Matrix2DIndices(IEnumerable<K> k1, IEnumerable<K> k2)
    {
        int k = 0;
        indices1 = k1.ToDictionary(key => key, key => k++);
        k = 0;
        indices2 = k2.ToDictionary(key => key, key => k++);

        values = new T[indices1.Count, indices2.Count];
        for (int i = 0; i < indices1.Count; i++)
            for (int j = 0; j < indices2.Count; j++)
                values[i, j] = default;
    }

}



//[ExecuteInEditMode]
public class CombactManager : MonoBehaviour
{
    public Army army1, army2;

    public List<Unit> units1, units2, allUnits;
    public Matrix2DIndices<Unit,bool> bUnitFighting;
    //public Matrix2DIndices<Unit,float> unitDistances;


    int numOfRows1, numOfRows2;
    float colLength1, rowLength1, colLength2, rowLength2, dist;


    void Start()
    {
        army1.Initialize();
        army2.Initialize();

        units1 = army1.units.ToList();
        units2 = army2.units.ToList();
        allUnits = units1.Concat(units2).ToList();


        foreach (var u1 in units1)
        {
            u1.isInFight = false;
        }

        foreach (var u2 in units2)
        {
            u2.isInFight = false;
        }

        bUnitFighting = new Matrix2DIndices<Unit, bool>(units1, units2);
        foreach (var u1 in units1)
        {
            foreach (var u2 in units2)
            {
                bUnitFighting.SetValue(u1, u2, false);
            }
        }

        //unitDistances = new Matrix2DIndices<Unit, float>(units1, units2);


        StartCoroutine(UpdateCombactManager());
    }

    private void ResetValues()
    {

        foreach (var u in allUnits)
        {
            foreach (var s in u.soldiers)
                s.soldiersFightingAgainstDistance.Clear();
        }

        foreach (var u1 in units1)
        {
            foreach (var u2 in units2)
            {
                bUnitFighting.SetValue(u1, u2, false);
            }
        }
    }


    private bool CheckIfUnitsFighting(Unit u1, Unit u2)
    {
        return !u1.meleeGeom.Disjoint(u2.meleeGeom);


        //// TRYING NOT TO USE THE CONVEX HULL
        //numOfRows1 = GetNumRows(u1.numOfSoldiers, u1.cols);
        //colLength1 = 2 * GetHalfLenght(u1.stats.soldierDistVertical, numOfRows1);
        //rowLength1 = 2 * GetHalfLenght(u1.stats.soldierDistLateral, u1.cols);
        //numOfRows2 = GetNumRows(u2.numOfSoldiers, u2.cols);
        //colLength2 = 2 * GetHalfLenght(u2.stats.soldierDistVertical, numOfRows2);
        //rowLength2 = 2 * GetHalfLenght(u2.stats.soldierDistLateral, u2.cols);

        //dist = Vector3.Distance(u1.position, u2.position);
        //if (dist <  Mathf.Max(colLength1, colLength2, rowLength1, rowLength2)) // if this two units are close
        //{
        //    return true;

        //}
        //return false;


        //numOfRows1 = GetNumRows(u1.numOfSoldiers, u1.cols);
        //colLength1 = 2 * GetHalfLenght(u1.stats.soldierDistVertical, numOfRows1);
        //rowLength1 = 2 * GetHalfLenght(u1.stats.soldierDistLateral, u1.cols);
        //numOfRows2 = GetNumRows(u2.numOfSoldiers, u2.cols);
        //colLength2 = 2 * GetHalfLenght(u2.stats.soldierDistVertical, numOfRows2);
        //rowLength2 = 2 * GetHalfLenght(u2.stats.soldierDistLateral, u2.cols);

        //dist = Vector3.Distance(u1.position, u2.position);
        //if (dist < 2 * Mathf.Max(colLength1, colLength2, rowLength1, rowLength2)) // if this two units are close
        //{
        //    // we then need to update their hulls if not already done
        //    if (!u1.hullAlreadyUpdated)
        //    {
        //        UpdateUnitHull(u1);
        //    }
        //    if (!u2.hullAlreadyUpdated)
        //    {
        //        UpdateUnitHull(u2);
        //    }

        //}
        //else
        //{
        //    return false;
        //}


        //if (u1.hull == null)
        //{
        //    UpdateUnitHull(u1);
        //}
        //if (u2.hull == null)
        //{
        //    UpdateUnitHull(u2);
        //}

        //return !u1.hull.Disjoint(u2.hull);
    }
    private void PostUpdate()
    {

        // Update fighting condition
        foreach (var u in allUnits)
        {
            if (!bUnitFighting.GetArrayValue(u).Contains(true))
                u.isInFight = false;
        }


        // Updating fighting targets
        foreach (var u in allUnits)
        {
            if (u.isInFight)
            {

                if (!u.fightingTarget)
                {
                    // TODO : refactor
                    if (u.commandTarget)
                    {
                        if (bUnitFighting.GetValue(u, u.commandTarget))   // if the command target unit is in range you should fight against that one
                            u.fightingTarget = u.commandTarget;
                        else                                              // Otherwise just select one
                            u.fightingTarget = bUnitFighting.GetDictValue(u).Where(kv => kv.Value).First().Key;
                    }
                    else                                              // Otherwise just select one
                        u.fightingTarget = bUnitFighting.GetDictValue(u).Where(kv => kv.Value).First().Key;

                }

            }
            else
                if (u.fightingTarget)
                u.fightingTarget = null;

            if (u.DEBUG_MODE)
                if (u.fightingTarget)
                    Debug.DrawLine(u.position + Vector3.up * 3, u.fightingTarget.position + Vector3.up * 3, Color.cyan);
        }
    }
    private static Dictionary<int, CUnit> cUnitDict;
    private void PreUpdate()
    {
        //int i = 0;
        //cUnitDict = allUnits.ToDictionary(u => i++, u => u.cunit);

        //NativeArray<JobHandle> handles = new NativeArray<JobHandle>(allUnits.Count, Allocator.TempJob);
        //int k = 0;
        var toRemove = new List<Unit>();
        foreach (var u in allUnits)
        {
            if (u.numOfSoldiers == 0)
            {
                DestroyGO(u.gameObject);

                if (units1.Contains(u)) units1.Remove(u);
                else units2.Remove(u);
                toRemove.Add(u);
                bUnitFighting.Remove(u);

                foreach(var o in allUnits)
                {

                    if (o.fightingTarget == u || o.commandTarget == u)
                    {
                        if (o.fightingTarget == u)
                            o.fightingTarget = null;
                        if (o.commandTarget == u)
                            o.commandTarget = null;

                        o.targetPos = o.position;
                        //o.targetDirection = u.lastPos;
                        o.combactState = UnitCombactState.DEFENDING;
                        o.state = UnitState.IDLE;
                    }

                }
            }
        }
        foreach (var toRem in toRemove)
            allUnits.Remove(toRem);


        foreach (var u in allUnits)
            u.cunit.UnitUpdate();

        //JobHandle jh = JobHandle.CombineDependencies(handles);
        //jh.Complete();

    }



    private WaitForEndOfFrame wfeof;
    IEnumerator UpdateCombactManager()
    {
        wfeof = new WaitForEndOfFrame();


        int k = 0;
        while (true)
        {

            if (k < 5)
            {
                k++;
                yield return wfeof;
            }
            else
                k = 0;


            if (!Application.isPlaying)
            {
                Start();
            }


            PreUpdate();

            // RESET
            ResetValues();




            foreach (var u1 in units1)
                foreach (var u2 in units2)
                {
                    //float dist = Vector3.Distance(u1.position, u2.position);
                    //unitDistances.SetValue(u1, u2, dist);
                    //if (dist > 20) continue;

                    if (CheckIfUnitsFighting(u1, u2))
                    {
                        // Update if fighting
                        bUnitFighting.SetValue(u1, u2, true);
                        u1.isInFight = u2.isInFight = true;

                        // Update soldiers fighting against
                        foreach (var s2 in u2.soldiers)
                        {
                            foreach (var s1 in u1.soldiers)
                            {

                                bool s1Done = s1.soldiersFightingAgainstDistance.ContainsKey(s2);
                                bool s2Done = s2.soldiersFightingAgainstDistance.ContainsKey(s1);

                                if (s1Done && s2Done)
                                    continue;
                                else if (s1Done && !s2Done)
                                    s2.soldiersFightingAgainstDistance[s1] = s1.soldiersFightingAgainstDistance[s2];
                                else if (!s1Done && s2Done)
                                    s1.soldiersFightingAgainstDistance[s2] = s2.soldiersFightingAgainstDistance[s1];
                                else
                                    s1.soldiersFightingAgainstDistance[s2] = s2.soldiersFightingAgainstDistance[s1] = Vector3.Distance(s1.go.transform.position, s2.go.transform.position);

                            }
                        }
                    }

                }

            PostUpdate();
        }

        

    }

    


}
