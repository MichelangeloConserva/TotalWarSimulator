using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils;

public class SpringTest : MonoBehaviour
{

    public GameObject target;
    public List<Vector3> targetsTrajectory;

    private List<GameObject> soldiers;
    private Rigidbody rb;
    private Vector3 targetPos;




    private void OnCollisionEnter(Collision collision)
    {
        
    }


    private IEnumerator nextTg()
    {
        yield return new WaitForSeconds(1f);

    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        var targets = new List<Vector3>();
        for (int i = 0; i < target.transform.childCount; i++)
            targets.Add(target.transform.GetChild(i).position);

        targetPos = targets.OrderBy(o => Vector3.Distance(o, transform.position)).First();

        var dir = targetPos - transform.position;

        targetsTrajectory = new List<Vector3>();
        targetsTrajectory.Add(transform.position + Vector3.ClampMagnitude(dir, dir.magnitude - 1.5f));

        soldiers = new List<GameObject>();
        for (int i = 0; i < transform.parent.childCount; i++)
            soldiers.Add(transform.parent.GetChild(i).gameObject);
    }

    // Update is called once per frame
    void Update()
    {



        //float rbSpeed = rb.velocity.magnitude;
        //if (Vector3.Distance(transform.position, targetPos) > 0.3f)
        //{

        //    transform.LookAt(targetPos);

        //    // Force to move
        //    var accelleration = 25  * Random.Range(0.9f, 1.1f);
        //    rb.AddForce(accelleration * transform.forward, ForceMode.Acceleration);

        //    // Kill lateral velocity
        //    var lateralVelocity = Vector3.Project(rb.velocity, transform.right);
        //    lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, 5f);
        //    rb.AddForce(lateralVelocity, ForceMode.Acceleration);

        //    //Debug.DrawRay(transform.position + Vector3.up * 2, Vector3.Project(rb.velocity, transform.forward), Color.green);
        //    //Debug.DrawRay(transform.position + Vector3.up, rb.velocity, Color.green);
        //    //Debug.DrawRay(transform.position + Vector3.up, Vector3.Project(rb.velocity, transform.forward), Color.green);
        //    //Debug.DrawRay(transform.position + Vector3.up, Vector3.Project(rb.velocity, transform.right), Color.green);


        //}
        //else if (rbSpeed > 1f)
        //{
        //    if (targetsTrajectory.Count > 0)
        //    {
        //        targetPos = targetsTrajectory.First();
        //        targetsTrajectory.RemoveAt(0);
        //    }

        //    var accelleration = -rbSpeed / Time.fixedDeltaTime;
        //    rb.AddForce(accelleration * transform.forward, ForceMode.Acceleration);
        //}
    }
}
