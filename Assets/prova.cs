using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static Utils;
using Debug = UnityEngine.Debug;

public class prova : MonoBehaviour
{

    public Transform TargetObjectTF;
    public GameObject arrow;

    public float LaunchAngle = 45;

    public GameObject instantiatedArrow;

    private Rigidbody rigidbody;
    private Vector3 initialPosition;
    private Quaternion initialRotation;


    private void Start()
    {
        instantiatedArrow = Instantiate(arrow, transform.position, Quaternion.Euler(90,0,0));
        rigidbody = instantiatedArrow.GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        initialPosition = instantiatedArrow.transform.position;
        initialRotation = instantiatedArrow.transform.rotation;
    }


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            rigidbody.constraints = RigidbodyConstraints.None;
            Launch();
        }
        if (Input.GetMouseButtonDown(0))
            ResetToInitialState();

        //if (rigidbody.velocity.magnitude != 0)
        //    instantiatedArrow.transform.rotation = Quaternion.LookRotation(rigidbody.velocity);// * initialRotation;

    }

    void ResetToInitialState()
    {
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        instantiatedArrow.transform.SetPositionAndRotation(initialPosition, initialRotation);
    }

    void Launch()
    {
        // think of it as top-down view of vectors: 
        //   we don't care about the y-component(height) of the initial and target position.
        Vector3 projectileXZPos = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 targetXZPos = new Vector3(TargetObjectTF.position.x, 0.0f, TargetObjectTF.position.z);

        // rotate the object to face the target
        instantiatedArrow.transform.LookAt(targetXZPos);

        // shorthands for the formula
        float R = Vector3.Distance(projectileXZPos, targetXZPos);
        float G = Physics.gravity.y;
        float tanAlpha = Mathf.Tan(LaunchAngle * Mathf.Deg2Rad);
        float H = TargetObjectTF.position.y - transform.position.y;

        // calculate the local space components of the velocity 
        // required to land the projectile on the target object 
        float Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));
        float Vy = tanAlpha * Vz;

        // create the velocity vector in local space and get it in global space
        Vector3 localVelocity = new Vector3(0f, Vy, Vz);
        Vector3 globalVelocity = instantiatedArrow.transform.TransformDirection(localVelocity);

        // launch the object by setting its initial velocity and flipping its state
        rigidbody.velocity = globalVelocity;
    }


}
