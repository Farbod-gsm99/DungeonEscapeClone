using UnityEngine;

public class SimpleNPC : MonoBehaviour
{
    public Transform target;
    private Rigidbody rb;

    public float walkSpeed = 1;

    private Vector3 dirToGo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        dirToGo = target.position - transform.position;
        dirToGo.y = 0;
        rb.rotation = Quaternion.LookRotation(dirToGo);

        rb.MovePosition(transform.position + transform.forward * walkSpeed * Time.deltaTime);
    }

    public void SetRandomWalkSpeed()
    {
        walkSpeed = Random.Range(1f, 7f);
    }
}
