using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CombactManagerNew : MonoBehaviour
{

    public ArmyNew attacker, defender;

    public List<UnitNew> unitsAttacker, unitsDefender;
    public static List<UnitNew> allUnits;


    void Start()
    {
        attacker.InstantiateArmy();
        defender.InstantiateArmy();

        unitsAttacker = attacker.units;
        unitsDefender = defender.units;
        allUnits = unitsAttacker.Concat(unitsDefender).ToList();

        StartCoroutine(UpdateCombactManager());
    }


    private HashSet<SoldierNew> prova = new HashSet<SoldierNew>();
    private HashSet<SoldierNew> deads = new HashSet<SoldierNew>();
    private HashSet<UnitNew> deadUnits = new HashSet<UnitNew>();
    private void CheckForDeads()
    {

        deadUnits.Clear();
        foreach (UnitNew u in allUnits)
        {
            
            if (u.soldiers.Count == 0)
            {
                deadUnits.Add(u);

                if (attacker.units.Contains(u))
                    attacker.RemoveUnit(u);
                else
                    defender.RemoveUnit(u);


                continue;
            }


            deads.Clear();
            foreach(var s in u.soldiers)
                if (s.health < 0)
                    deads.Add(s);

            foreach(var d in deads)
            {
                u.soldiers.Remove(d);
                Destroy(d.gameObject);
            }
            if (deads.Count > 0)
                u.UpdateMeleeCollider();



            if (u.fightingAgainst.Count > 0)
                AddSoldiersTargets(u);

        }

        foreach(var du in deadUnits)
        {
            if (unitsAttacker.Contains(du))
                unitsAttacker.Remove(du);
            else
                unitsDefender.Remove(du);
            allUnits.Remove(du);

            StartCoroutine(DestroyUnitCO(du));

        }
    }

    private IEnumerator DestroyUnitCO(UnitNew du)
    {
        du.position = -Vector3.up * 100;
        yield return new WaitForSeconds(10);
        Destroy(du);
    }



    private void AddSoldiersTargets(UnitNew u)
    {
        prova.Clear();
        foreach (var enemyUnit in u.fightingAgainst)
            foreach (var es in enemyUnit.soldiers)
                prova.Add(es);

        if (prova.Count == 0) return;

        foreach (var s in u.soldiers)
            s.enemySoldierPosition = prova.OrderBy(es => Vector3.SqrMagnitude(es.position - s.position)).First().position;
    }


    private struct UpdateCUnitJOB : IJob
    {
        public int i;

        public void Execute()
        {
            allUnits[i].cunit.UnitUpdate();
        }
    }

    private void UpdateCUnit()
    {
        //NativeArray<JobHandle> handles = new NativeArray<JobHandle>(allUnits.Count, Allocator.TempJob);

        //for (int i=0; i<allUnits.Count; i++)
        //{
        //    allUnits[i].cunit.CalculateVariablesBeforeScheduling();
        //    UpdateCUnitJOB jobData = new UpdateCUnitJOB { i = i};
        //    handles[i] = jobData.Schedule();
        //}

        //JobHandle jh = JobHandle.CombineDependencies(handles);
        //jh.Complete();
        //handles.Dispose();


        // For some weird reason this is much faster
        foreach (var u in allUnits)
            u.cunit.UnitUpdate();
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

            CheckForDeads();
            UpdateCUnit();
        }
    }
}