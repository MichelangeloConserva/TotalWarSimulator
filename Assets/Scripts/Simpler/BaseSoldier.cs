using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unit;


public abstract class BaseSoldier
{
    public GameObject go;
    public Rigidbody rb;

    public bool isCharging;
    public float distFromFront;
    public float meeleRange;
    public float health;
    public float meeleAttack;
    public float meeleDefence;
    public UnitStats stats;
    public Vector3 targetPos;
    public Vector3 targetLookAt;
    public Dictionary<BaseSoldier, float> soldiersFightingAgainstDistance = new Dictionary<BaseSoldier, float>();
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
    //private readonly Unit unit;

    public BaseSoldier(GameObject g, BaseUnitStats baseStats)
    {
        meeleRange = baseStats.meeleRange;
        health = baseStats.health;
        meeleAttack = baseStats.meeleAttack;
        meeleDefence = baseStats.meeleDefence;
        //this.unit = unit;
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
        }
        //if (unit.isInFight)
        //{
        //    var rotation = Quaternion.LookRotation(targetLookAt - position);
        //    go.transform.rotation = Quaternion.Lerp(go.transform.rotation, rotation, Time.deltaTime);
        //}
        //else
        //    go.transform.LookAt(targetLookAt);
    }

}
