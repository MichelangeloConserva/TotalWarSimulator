using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{

    public float damage = 5, destroyAfter = 10; 
    private Rigidbody rb;
    
    void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = transform.GetChild(2).position;
    }

    
    void FixedUpdate()
    {
        if (rb && rb.velocity != Vector3.zero)
            rb.rotation = Quaternion.LookRotation(rb.velocity) * Quaternion.Euler(90,0,0);
        
    }



    private void OnCollisionEnter(Collision collision)
    {
        //return;
        rb.velocity *= 0;

        Destroy(GetComponent<CapsuleCollider>());
        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<TrailRenderer>());

        if (collision.rigidbody == null)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;  // hit the terrain
        }
        else  // hit a soldier
        {
            transform.parent = collision.gameObject.transform.GetChild(0).transform;
            collision.gameObject.GetComponent<SoldierNew>().health -= damage;
        }

        Destroy(gameObject, destroyAfter);
    }

    public void Launch(Vector3 target, float launchAngle = 45)
    {
        Initialize();

        // think of it as top-down view of vectors: 
        //   we don't care about the y-component(height) of the initial and target position.
        Vector3 projectileXZPos = new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 targetXZPos = new Vector3(target.x, 0.0f, target.z);

        // rotate the object to face the target
        transform.LookAt(targetXZPos);

        // shorthands for the formula
        float R = Vector3.Distance(projectileXZPos, targetXZPos);
        float G = Physics.gravity.y;
        float tanAlpha = Mathf.Tan(launchAngle * Mathf.Deg2Rad);
        float H = target.y - transform.position.y;

        // calculate the local space components of the velocity 
        // required to land the projectile on the target object 
        float Vz = Mathf.Sqrt(G * R * R / (2.0f * (H - R * tanAlpha)));
        float Vy = tanAlpha * Vz;

        // create the velocity vector in local space and get it in global space
        Vector3 localVelocity = new Vector3(0f, Vy, Vz);
        Vector3 globalVelocity = transform.TransformDirection(localVelocity);

        // launch the object by setting its initial velocity and flipping its state
        rb.velocity = globalVelocity;
    }

}
