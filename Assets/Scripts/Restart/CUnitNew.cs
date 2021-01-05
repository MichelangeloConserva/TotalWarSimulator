using PathCreation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class CUnitNew : MonoBehaviour
{
    public float noise;
    public float attackingFactor;
    public float escapeTime = 3; // TODO : encode this value


    public PathCreator pathCreator;
    public EndOfPathInstruction endOfPathInstruction;

    public float distanceTravelled;


    VertexPath path;
    FormationResult fr;
    public UnitNew unit;

    private Vector3 destDirection;
    private bool setFinalDirection;


    public void Initialize(UnitNew u, PathCreator pathCreator, float noise, float attackingFactor, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop)
    {
        this.noise = noise;
        this.attackingFactor = attackingFactor;
        unit = u;
        this.pathCreator = pathCreator;
        this.endOfPathInstruction = endOfPathInstruction;
    }

    public void MoveAt(List<Vector3> traj)
    {

        var colls = Physics.OverlapSphere(traj.Last(), 3, LayerMask.GetMask(unit.army.enemySoldierLayer));
        if (colls.Length > 0)
        {
            unit.combactState = UnitCombactState.ATTACKING;
            unit.commandTarget = colls[0].GetComponent<SoldierNew>().unit;
            traj[traj.Count-1] = unit.commandTarget.position;
        }
        else
        {
            unit.combactState = UnitCombactState.DEFENDING;
            unit.commandTarget = null;
        }

        if(unit.commandTarget && unit.GetType() == typeof(ArcherNew) && ((ArcherNew)unit).unitsInRange.Contains(unit.commandTarget))
        {
            return;
        }


        if (unit.isInFight)
            StartCoroutine(EscapingCO());
        else
            unit.state = UnitState.MOVING;


        distanceTravelled = 0;
        traj.Insert(0, unit.position);
        pathCreator.bezierPath = new BezierPath(traj, false, PathSpace.xyz);
    }
    public void MoveAt(Vector3 mouseClick) { MoveAt(new List<Vector3>() { unit.position * 0.5f + mouseClick * 0.5f, mouseClick }); }

    public void MoveAt(Vector3 dest, Vector3 destDirection)
    {
        this.destDirection = destDirection;
        setFinalDirection = true;
        MoveAt(dest);
    }

    private IEnumerator EscapingCO()
    {
        unit.state = UnitState.ESCAPING;
        yield return new WaitForSeconds(escapeTime);
        if (unit.state == UnitState.ESCAPING)
            unit.state = UnitState.MOVING;
    }


    //public void CalculateVariablesBeforeScheduling()
    //{
    //    path = pathCreator.path;
    //}


    public void UnitUpdate()
    {

        if (unit.isInFight && unit.state != UnitState.ESCAPING && unit.fightingTarget)
        {
            UpdateCombactFormation();
        }
        else
        {

            path = pathCreator.path;
            if (unit.state == UnitState.MOVING || unit.state == UnitState.ESCAPING)
            {
                if (unit.army.DEBUG_MODE)
                {
                    for (int i = 0; i < path.NumPoints; i++)
                    {
                        int nextI = i + 1;
                        if (nextI >= path.NumPoints)
                            if (false)
                                nextI %= path.NumPoints;
                            else
                                break;
                        Debug.DrawLine(path.GetPoint(i) + Vector3.up * 3, path.GetPoint(nextI) + Vector3.up * 3, Color.yellow);
                    }
                }

                float averageDist = unit.soldiers.Aggregate(0f, (acc, s) => acc + (s.targetPos - s.position).magnitude) / unit.numOfSoldiers;
                if (averageDist < 3)
                    distanceTravelled += unit.pathSpeed * Time.deltaTime * (1 - averageDist / 3);


                if (path.GetClosestTimeOnPath(unit.position) > 0.97f)
                    Stop();
                else
                {
                    unit.position = path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
                    unit.direction = path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
                    UpdateFormation(unit.position, unit.direction);
                }
            }

            else if (unit.state == UnitState.IDLE)
            {

                UpdateFormation(unit.position, unit.direction);

                foreach (var s in unit.soldiers)
                {
                    s.targetLookAt = s.position + unit.direction;

                    if (Vector3.Distance(s.position, s.targetPos) > 0.5f)
                        s.Move();

                    // we must do this manually because when getting close to the target pos then the soldiers spins on themselves
                    //s.gameObject.transform.LookAt(s.targetLookAt);
                }
            }

        }

    }

    public void Stop()
    {
        pathCreator.bezierPath = new BezierPath(Vector3.zero);
        unit.position = path.GetPointAtDistance(distanceTravelled, endOfPathInstruction) + path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
        if (setFinalDirection)
            unit.direction = destDirection;
        else
            unit.direction = path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
        unit.state = UnitState.IDLE;
        foreach (var s in unit.soldiers)
            s.targetLookAt = s.targetPos + unit.direction;
    }



    private Vector3 formationPos, unitDir;
    private Vector3[] targets, currents = new Vector3[0];
    private int[] assignment;
    private void CalculateAssignments(Vector3 center, Vector3 direction)
    {
        targets = GetFormationAtPos(center, direction, unit.numOfSoldiers, unit.numCols, unit.soldierDistLateral, unit.soldierDistVertical);
        if (currents.Length != unit.numOfSoldiers)
            currents = new Vector3[unit.numOfSoldiers];
        for (int i = 0; i < unit.numOfSoldiers; i++)
            currents[i] = unit.soldiers.ElementAt(i).position;

        assignment = LSCAssignment(currents, targets);
    }


    private void UpdateCombactFormation()
    {

        if (unit.combactState == UnitCombactState.DEFENDING)
        {
            formationPos = unit.position;
            unitDir = unit.direction;
        }
        else
        {
            formationPos = unit.position;
            unitDir = unit.fightingTarget.position - unit.position;
        }


        CalculateAssignments(formationPos, unitDir);
        for (int i = 0; i < unit.numOfSoldiers; i++)
        {
            var s = unit.soldiers.ElementAt(i);

            // Look at the closest enemy
            s.targetLookAt = GetVector3Down(s.enemySoldierPosition) ;//+ 0.5f * Vector3.up;

            // Set formation position
            s.targetPos = GetVector3Down(targets[assignment[i]]) ;//+ 0.5f * Vector3.up;

            var dir = s.enemySoldierPosition - s.position;
            RaycastHit hit;
            if (Physics.Raycast(s.frontPos, dir, out hit, 100, LayerMask.GetMask(unit.fightingTarget.soldierLayerName, unit.soldierLayerName)))
            {
                float dist;
                if(hit.collider.gameObject.layer == s.gameObject.layer) // hit ally
                {
                    dist = hit.distance * 0.8f; 
                    if (dist < 0.5f) dist = 0;
                    
                    // Lerping between the formation pos and the position for attacking the enemy soldier
                    s.targetPos += dist * (dir).normalized + new Vector3(Random.Range(-noise, noise), 0, Random.Range(-noise, noise));

                }
                else // hit enemy
                {
                    // Fighting
                    if (hit.distance < s.meeleRange)
                    {
                        var enemy = hit.collider.GetComponent<SoldierNew>();
                        float damage = Mathf.Max(s.meeleAttack - enemy.meeleDefence, 0);
                        enemy.health -= (damage + Random.Range(0, 1)) * Time.deltaTime;
                    }

                    // We get closer to the enemy only if we are not too close
                    if (hit.distance > s.meeleRange / 4)
                        s.targetPos = GetVector3Down(hit.point) + new Vector3(Random.Range(-noise, noise), 0, Random.Range(-noise, noise));//+ 0.5f * Vector3.up;
                }

                if (unit.army.DEBUG_MODE)
                    Debug.DrawRay(s.position, s.targetPos - s.position + Vector3.up * 2, Color.magenta);
            }

            s.Move();
        }
    }







    private void UpdateFormation(Vector3 center, Vector3 direction)
    {
        CalculateAssignments(center, direction);
        for (int i = 0; i < unit.numOfSoldiers; i++)
        {
            var s = unit.soldiers.ElementAt(i);
            s.targetPos = s.targetLookAt = GetVector3Down(targets[assignment[i]]) ;//+ 0.5f * Vector3.up;
            s.Move();
        }
    }


    /// <summary>
    /// It is okay for disorganized fights in which each soldiers just tries to attack the closest enemy
    /// </summary>
    private void FightNotInformation()
    {
        foreach (var s in unit.soldiers)
        {
            var closer = s.soldiersFightingAgainstDistance.OrderBy(kv => kv.Value).First().Key;
            var dir = closer.position - s.position;
            RaycastHit hit;
            if (Physics.Raycast(s.frontPos, dir, out hit))
            {
                s.targetLookAt = GetVector3Down(hit.point) ;//+ 0.5f * Vector3.up;
                var dist = hit.distance * 0.8f;
                if (dist < 0.5f) dist = 0;
                s.targetPos = dist * (dir).normalized + s.position + new Vector3(Random.Range(-noise, noise), 0, Random.Range(-noise, noise));

                if (unit.army.DEBUG_MODE)
                    Debug.DrawRay(s.position, s.targetPos - s.position + Vector3.up * 2, Color.magenta);
            }
            s.Move();
        }
    }






}
