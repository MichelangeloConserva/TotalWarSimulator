using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeleeStats;

public class SoldierNew : MonoBehaviour
{

    public UnitNew unit;
    public Rigidbody rb;

    public Dictionary<Soldier, float> soldiersFightingAgainstDistance = new Dictionary<Soldier, float>();

    public Vector3 targetPos;
    public Vector3 targetLookAt;

    public bool isCharging;
    public float distFromFront;
    public float meeleRange;
    public float health;
    public float meeleAttack;
    public float meeleDefence;
    public float topSpeed;
    public float movementForce;

    private Transform front;


    public Vector3 position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }
    public Vector3 direction
    {
        get { return transform.forward; }
        set { transform.rotation = Quaternion.LookRotation(value); }
    }
    public Vector3 frontPos
    {
        get
        {
            return front.position;
        }
    }


    public void Initialize(UnitNew u, MeleeStatsHolder stats)
    {
        unit = u;
        meeleRange = stats.meeleRange;
        health = stats.health;
        meeleAttack = stats.meeleAttack;
        meeleDefence = stats.meeleDefence;
        topSpeed = stats.topSpeed;
        movementForce = stats.movementForce;
        rb = GetComponent<Rigidbody>();
        front = transform.GetChild(1); // get the transform of the front handler
    }

    public void Move()
    {
        Vector3 p = rb.position;

        if (rb.velocity.magnitude < topSpeed)
        {
            float dt = 0.02f;

            Vector3 v = rb.velocity;

            Vector3 force = rb.mass * (targetPos - p - v * dt) / dt;  // TODO : Damping is to taken into account

            rb.AddForce(Vector3.ClampMagnitude(force, movementForce),
                        isCharging ? ForceMode.Impulse : ForceMode.Force);


            if (unit.isInFight)
            {
                var rotation = Quaternion.LookRotation(targetLookAt - position);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime);
            }
            else
                transform.LookAt(targetLookAt);
        }
    }




}
