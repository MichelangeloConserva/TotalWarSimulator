using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeleeStats;
using static Utils;

public class SoldierNew : MonoBehaviour
{

    public UnitNew unit;
    public Rigidbody rb;

    public Dictionary<Soldier, float> soldiersFightingAgainstDistance = new Dictionary<Soldier, float>();

    public Vector3 enemySoldierPosition;
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
    public float mass;

    private Transform front;


    public Vector3 position
    {
        get { return _position; }
        set { transform.position = value; }
    }
    public Vector3 direction
    {
        get { return _direction; }
        set { transform.rotation = Quaternion.LookRotation(value); }
    }
    private Vector3 _position, _direction, _velocity;
    public Vector3 frontPos
    {
        get
        {
            return front.position;
        }
    }


    public void Initialize(UnitNew u, MeleeStatsHolder stats, Vector3 targetPos, Vector3 targetLookAt)
    {
        this.targetPos = targetPos;
        this.targetLookAt = targetLookAt;
        unit = u;
        meeleRange = stats.meeleRange;
        health = stats.health;
        meeleAttack = stats.meeleAttack;
        meeleDefence = stats.meeleDefence;
        topSpeed = stats.topSpeed;
        movementForce = stats.movementForce;
        rb = GetComponent<Rigidbody>();
        front = transform.GetChild(1); // get the transform of the front handler
        mass = rb.mass;
    }


    float dt = 0.02f;
    public void Move()
    {

        if (_velocity.magnitude < topSpeed)
        {

            Vector3 force = mass * (targetPos - _position - _velocity * dt) / dt;  // TODO : Damping is to taken into account

            //rb.AddForce(Vector3.ClampMagnitude(force, movementForce),
            //            isCharging ? ForceMode.Impulse : ForceMode.Force);
            data.bUpdate = true;
            data.force = Vector3.ClampMagnitude(force, movementForce);
        }
    }


    private struct UpdateData
    {
        public bool bUpdate;
        public Vector3 force;
    }
    private UpdateData data;

    private void Start()
    {
        radius = GetComponent<SphereCollider>().radius;
    }

    private void Update()
    {
        if (!letItPass)
        {
            if (data.bUpdate)
            {
                data.bUpdate = false;

                rb.AddForce(Vector3.ClampMagnitude(data.force, movementForce),
                            isCharging ? ForceMode.Impulse : ForceMode.Force);

                if (unit.isInFight)
                {
                    var rotation = Quaternion.LookRotation(targetLookAt - position);
                    transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 3 * Time.deltaTime);
                }
                else
                    transform.LookAt(targetLookAt);
            }
        }
        else
            letItPass = false;

        
        _position = transform.position;
        _direction = transform.forward;
        _velocity = rb.velocity;
    }


    private bool letItPass;
    //private void OnCollisionStay(Collision collision)
    //{
    //    if (collision.gameObject.layer == gameObject.layer && collision.gameObject.GetComponent<SoldierNew>().unit != unit && !unit.isInFight)
    //    {
    //        rb.AddForce( GetVector3Down((collision.GetContact(0).point - transform.position).normalized) * movementForce + GetRandomVectorXZ(movementForce/5));
    //        letItPass = true;
    //        Debug.DrawRay(collision.GetContact(0).point, Vector3.up * 10);
    //    }
    //}

    public float radius;
    public int allyCollision = 0;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != gameObject.layer) return;

        var otherUnit = collision.gameObject.GetComponent<SoldierNew>().unit;
        if (otherUnit != unit && !unit.isInFight && !otherUnit.isInFight)
        {
            Vector3 dir = collision.transform.position - transform.position;
            if (Vector3.Angle(dir, transform.forward) < 45)
            {
                if (Vector3.Distance(collision.transform.position, targetPos) < Vector3.Distance(transform.position, targetPos))
                {
                    var oldPos = transform.position;
                    transform.position = collision.transform.position;
                    collision.transform.position = oldPos;
                }
            }
            allyCollision++;
            transform.LookAt(unit.transform.forward);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer != gameObject.layer) return;

        var otherUnit = collision.gameObject.GetComponent<SoldierNew>().unit;
        if (otherUnit != unit && !unit.isInFight && !otherUnit.isInFight)
        {
            rb.AddForce(Random.Range(2.5f,5f) * movementForce * Vector3.RotateTowards(targetPos - transform.position, GetRandomVectorXZ(1), 60 * Mathf.Deg2Rad, 100).normalized);
            transform.LookAt(unit.transform.forward);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == gameObject.layer && collision.gameObject.GetComponent<SoldierNew>().unit != unit && !unit.isInFight)
            allyCollision--;
        GetComponent<SphereCollider>().radius = radius;
    }


    //WaitForEndOfFrame wfeof = new WaitForEndOfFrame();
    //private IEnumerator JustLetItPass()
    //{
    //    yield return wfeof



    //}


}
