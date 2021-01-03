using PathCreation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class CUnitNew : MonoBehaviour
{
    [Range(0.0f, 5.0f)]
    public float noise = 4f;
    [Range(0.05f, 1.0f)]
    public float attackingFactor = 0.9f;

    public PathCreator pathCreator;
    public EndOfPathInstruction endOfPathInstruction;

    public float distanceTravelled;


    VertexPath path;
    FormationResult fr;
    public UnitNew unit;

    private Vector3 destDirection;
    private bool setFinalDirection;


    public void Initialize(UnitNew u, PathCreator pathCreator, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop)
    {
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
        }
        else
        {
            unit.combactState = UnitCombactState.DEFENDING;
            unit.commandTarget = null;
        }

        if (unit.isInFight)
            unit.state = UnitState.ESCAPING;
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



    public void UnitUpdate()
    {
        if (!unit) return;
        if (!pathCreator)
        {
            pathCreator = GetComponentInChildren<PathCreator>();
            pathCreator.transform.position = Vector3.zero;
            pathCreator.transform.rotation = Quaternion.identity;
            UpdateFormation(unit.position, unit.direction);
        }


        if (unit.isInFight && unit.state != UnitState.ESCAPING && unit.fightingTarget)
        {
            // TODO : generalize for all kind of formations
            // TODO : Add a check so that depending on the unit Update CombactFormation or NotInformtion are called
            UpdateCombactFormation();
        }
        else
        {

            path = pathCreator.path;
            if (unit.state == UnitState.MOVING || unit.state == UnitState.ESCAPING)
            {
                if (unit.DEBUG_MODE)
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
                {
                    distanceTravelled += unit.pathSpeed * Time.deltaTime * (1 - averageDist / 3);
                }



                if (path.GetClosestTimeOnPath(unit.position) > 0.97f)
                {
                    unit.position = path.GetPointAtDistance(distanceTravelled, endOfPathInstruction) + path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
                    if (setFinalDirection)
                        unit.direction = destDirection;
                    else
                        unit.direction = path.GetDirectionAtDistance(distanceTravelled, endOfPathInstruction);
                    unit.state = UnitState.IDLE;
                    foreach (var s in unit.soldiers)
                        s.targetLookAt = s.targetPos + unit.direction;
                }
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
                    s.direction = s.targetLookAt;
                }
            }

        }

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

    private Vector3[] GetFormationAtPos(Vector3 center, Vector3 direction, int numOfSoldiers, int cols, float soldierDistLateral, float soldierDistVertical)
    {
        throw new System.NotImplementedException();
    }

    private void UpdateCombactFormation()
    {


        if (unit.combactState == UnitCombactState.DEFENDING)
        {
            formationPos = unit.position;
            unitDir = unit.fightingTarget.position - unit.position;
        }
        else
        {
            var numOfRows = GetNumRows(unit.numOfSoldiers, unit.numCols);
            var ColLength = GetHalfLenght(unit.soldierDistVertical, numOfRows);
            numOfRows = GetNumRows(unit.fightingTarget.numOfSoldiers, unit.fightingTarget.numCols);
            var EnemyColLength = GetHalfLenght(unit.fightingTarget.soldierDistVertical, numOfRows);

            unitDir = (unit.fightingTarget.position - unit.position).normalized;
            unitDir *= (ColLength + EnemyColLength) * attackingFactor;
            formationPos = unit.fightingTarget.position - unitDir;
        }





        CalculateAssignments(formationPos, unitDir);
        for (int i = 0; i < unit.numOfSoldiers; i++)
        {
            var s = unit.soldiers.ElementAt(i);

            // Look at the closest enemy
            var closer = s.soldiersFightingAgainstDistance.OrderBy(kv => kv.Value).First().Key;
            s.targetLookAt = GetVector3Down(closer.position) + 0.5f * Vector3.up;

            // Set formation position
            s.targetPos = GetVector3Down(targets[assignment[i]]) + Vector3.up * 0.5f;

            var dir = closer.position - s.position;
            RaycastHit hit;
            if (Physics.Raycast(s.frontPos, dir, out hit, 100, LayerMask.GetMask(unit.fightingTarget.soldierLayerName)))
            {
                var dist = hit.distance * 0.8f;
                if (dist < 0.5f) dist = 0;

                // Lerping between the formation pos and the position for attacking the enemy soldier
                s.targetPos += dist * (dir).normalized + new Vector3(Random.Range(-noise, noise), 0, Random.Range(-noise, noise));

                // Fighting
                if (hit.distance < s.meeleRange)
                {
                    var enemy = hit.collider.GetComponent<SoldierUtils>().s;
                    float damage = Mathf.Max(s.meeleAttack - enemy.meeleDefence, 0);
                    enemy.health -= (damage + Random.Range(0, 1)) * Time.deltaTime;
                }

                if (unit.DEBUG_MODE)
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
            s.targetPos = s.targetLookAt = GetVector3Down(targets[assignment[i]]) + Vector3.up * 0.5f;
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
                s.targetLookAt = GetVector3Down(hit.point) + 0.5f * Vector3.up;
                var dist = hit.distance * 0.8f;
                if (dist < 0.5f) dist = 0;
                Debug.Log(dist);
                s.targetPos = dist * (dir).normalized + s.position + new Vector3(Random.Range(-noise, noise), 0, Random.Range(-noise, noise));
                Debug.DrawRay(s.position, s.targetPos - s.position + Vector3.up * 2, Color.magenta);
            }
            s.Move();
        }
    }






}
