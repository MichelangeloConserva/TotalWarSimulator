using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class SoldierController : MonoBehaviour
{
    [System.Serializable]
    public class Stats
    {
        public float dragToStop;
        public float topSpeedRunning;
        public float topSpeedWalking;
        public float accelleration;
        public float decelleration;
        public float adjustingAccelleration;
    }


    public Stats stats;
    public Vector3 targetPos;
    public int team;

    private Rigidbody rb;
    private UnitController unitController;
    private Animator anim;
    private Vector3 lastVelocity;
    private float initialDrag;
    private bool isFighting;


    private void OnCollisionEnter(Collision collision)
    {
        var sc = collision.gameObject.GetComponent<SoldierController>();

        if (sc)
            if (sc.team != team)
            {
                rb.AddForce(transform.forward * Random.Range(-1f, 1f), ForceMode.Impulse);
            }
            else
            {
                Debug.Log("friend");
            }

    }

    private void OnTriggerEnter(Collider other)
    {
        var sc = other.gameObject.GetComponent<SoldierController>();

        if (sc)
            if (sc.team != team)
            {
                anim.SetBool("IsFighting", true);
            }
    }

    private void OnTriggerStay(Collider other)
    {
        var sc = other.gameObject.GetComponent<SoldierController>();

        if (sc)
            if (sc.team != team)
            {
                anim.SetBool("IsFighting", true);
                targetPos = Vector3.Lerp(other.transform.position, transform.position, 0.5f);
            }


        //var sc = other.gameObject.GetComponent<SoldierController>();

        //if (sc)
        //    if (sc.team != team)
        //    {
        //        foreach (GameObject s in unitController.GetSoldiers())
        //            s.GetComponent<SoldierController>().targetPos = Vector3.Lerp(s.GetComponent<SoldierController>().targetPos, s.transform.position, 0.01f);
        //    }
    }


    private void OnTriggerExit(Collider other)
    {
        var sc = other.gameObject.GetComponent<SoldierController>();
        if (sc)
            anim.SetBool("IsFighting", false);
    }



    // DRag 18 for box collider


    // Start is called before the first frame update
    void Start()
    {
        targetPos = transform.position;
        rb = GetComponent<Rigidbody>();
        initialDrag = rb.drag;
        unitController = GetComponentInParent<UnitController>();
        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {








        //if (targetPos.y == -1)
        //    return;

        //Debug.DrawLine(transform.position, targetPos, Color.red);


        //  MOVEMENT
        float rbSpeed = rb.velocity.magnitude;
        if (Vector3.Distance(transform.position, targetPos) > 1f)
        {
            anim.SetBool("IsMoving", true);


            var topSpeed = (unitController.unitStatus == UnitStatus.WALKING ? stats.topSpeedWalking : stats.topSpeedRunning);


            // TODO : Optimize the way you get the mean distance
            float meanDistance = 0;
            var allSoldiers = unitController.GetSoldiers();
            foreach (GameObject s in allSoldiers)
                meanDistance += Vector3.Distance(s.transform.position, s.GetComponent<SoldierController>().targetPos) / allSoldiers.Count;
            float dist = Vector3.Distance(transform.position, targetPos);
            float coeff = meanDistance / topSpeed;


            // WORKS but would be better not
            //transform.LookAt(targetPos);
            //rb.velocity = transform.forward * (dist / coeff);


            // YOU ARE NOT TAKING INTO ACCOUNT something

            transform.LookAt(targetPos);

            // Force to move
            var forwardSpeed = Vector3.Project(rb.velocity, transform.forward).magnitude;
            //Debug.Log(Vector3.Angle(rb.velocity, transform.forward));

            if (Vector3.Angle(rb.velocity, transform.forward) > 90)
                forwardSpeed *= -1;

            var accelleration = (dist / coeff - forwardSpeed) / Time.fixedDeltaTime  * Random.Range(0.9f,1.1f);
            rb.AddForce(accelleration * transform.forward, ForceMode.Acceleration);

            // Kill lateral velocity
            //var lateralVelocity = Vector3.Project(rb.velocity, transform.right).normalized;
            //rb.AddForce(-50f * lateralVelocity, ForceMode.Acceleration);
            var lateralVelocity = Vector3.Project(rb.velocity, transform.right);
            lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, 5f);
            rb.AddForce(lateralVelocity, ForceMode.Acceleration);

            //Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.Project(rb.velocity, transform.forward), Color.green);


            //Debug.DrawRay(transform.position + Vector3.up, rb.velocity, Color.green);
            //Debug.DrawRay(transform.position + Vector3.up, Vector3.Project(rb.velocity, transform.forward), Color.green);
            //Debug.DrawRay(transform.position + Vector3.up, Vector3.Project(rb.velocity, transform.right), Color.green);


            //transform.LookAt(targetPos);

            //// Force to move
            //var forwardSpeed = Vector3.Project(rb.velocity, transform.forward).magnitude;
            //Debug.Log(Vector3.Angle(rb.velocity, transform.forward));

            //if (Vector3.Angle(rb.velocity, transform.forward) > 90)
            //    forwardSpeed *= -1;

            //Debug.Log(dist / coeff);

            //var accelleration = (dist / coeff - forwardSpeed) * Time.fixedDeltaTime;
            //rb.AddForce(accelleration * transform.forward, ForceMode.Acceleration);

            //// Kill lateral velocity
            ////var lateralVelocity = Vector3.Project(rb.velocity, transform.right);
            ////lateralVelocity = lateralVelocity.normalized * Mathf.Max(lateralVelocity.magnitude, 5f);
            ////rb.AddForce(lateralVelocity, ForceMode.Acceleration);

            //Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.Project(rb.velocity, transform.forward), Color.green);
        }
        else if (rbSpeed > 1f)
        {
            var accelleration = -rbSpeed / Time.fixedDeltaTime;
            rb.AddForce(accelleration * transform.forward, ForceMode.Acceleration);

            anim.SetBool("IsMoving", false);
            //rb.drag = stats.dragToStop;
        }
        else
            rb.drag = initialDrag;





        rb.velocity = lastVelocity = Vector3.ClampMagnitude(rb.velocity, 15);


    }
}
