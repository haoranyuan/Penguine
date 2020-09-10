using System.Collections;
using Unity.MLAgents;
using UnityEngine;
using Unity.MLAgents.Sensors;


public class PenguinAgent : Agent
{
    [Tooltip("How fast the agent moves forward")]
    public float moveSpeed = 5f;

    [Tooltip("How fast the agent turns")]
    public float turnSpeed = 180f;

    [Tooltip("Maximum number of steps")]
    public int maxStep = 3000;

    [Tooltip("Prefab of the heart that appears when the baby is fed")]
    public GameObject heartPrefab;

    [Tooltip("Prefab of the regurgitated fish that appears when the baby is fed")]
    public GameObject regurgitatedFishPrefab;

    private PenguinArea penguinArea;
    new private Rigidbody rigidbody;
    private GameObject baby;
    private bool isFull; // If true, penguin has a full stomach
    private float feedRadius = 0f;

    
    public override void OnEpisodeBegin()
    {
        Debug.Log("On Episode Begin");
        penguinArea = GetComponentInParent<PenguinArea>();
        baby = penguinArea.penguinBaby;
        rigidbody = GetComponent<Rigidbody>();
        isFull = false;
        penguinArea.ResetArea();
        
        feedRadius = Academy.Instance.EnvironmentParameters.GetWithDefault("feed_radius", 0f);
    }

    

    /// <summary>
    /// Perform actions based on a vector of numbers
    /// </summary>
    /// <param name="vectorAction">The list of actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        //Convert teh first action to forward movement 
        float forwardAmount = vectorAction[0];

        //Convert the second action to turning left or right
        float turnAmount = 0f;
        if (vectorAction[1] == 1f)
        {
            turnAmount = -1f;
        }
        else if (vectorAction[1] == 2f)
        {
            turnAmount = 1f;
        }

        // Apply movement
        rigidbody.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(transform.up * turnAmount * turnSpeed * Time.fixedDeltaTime);

        // Apply a tiny negative reward every step to encourage action
        if (maxStep > 0) AddReward(-1f / maxStep);

    }

    /// <summary>
    /// Read inputs from the keyboard and convert them to a list of actions.
    /// </summary>
    /// <returns>A vectorAction array of floats that will be passed into <see cref="AgentAction(float[])"/></returns>
    public override void Heuristic(float[] actionsOut)
    {
        float forwardAction = 0f;
        float turnAction = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            // move forward
            forwardAction = 1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            // turn left
            turnAction = 1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            // turn right
            turnAction = 2f;
        }

        actionsOut[0] = forwardAction;
        actionsOut[1] = turnAction;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(isFull);
        sensor.AddObservation(Vector3.Distance(baby.transform.position, transform.position));
        // Add baby's direction
        sensor.AddObservation((baby.transform.position - transform.position).normalized );
        // Add penguin's direction
        sensor.AddObservation(transform.forward);

        // 1 + 1 + 3 + 3 = 8
    }

    public void FixedUpdate()
    {
        // Requset decision every 5 steps. RequestDecision() automatically calls RequestAction
        // But for the steps in between, we need to call it explicitly to take action using the 
        // results of the previous decision
        if (StepCount % 4 == 0)
        {
            RequestDecision();
        }
        else 
        {
            RequestAction();
        }

        // Test if the agent is close enough to feed the baby
        if (Vector3.Distance(transform.position, baby.transform.position) < feedRadius)
        {
            // Close enough, feed the baby
            RegurgitateFish();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("fish"))
        {
            // Try to eat the fish
            EatFish(collision.gameObject);
        }
        else if (collision.transform.CompareTag("baby"))
        {
            // Try to feed the baby
            RegurgitateFish();
        }
    }


    /// <summary>
    /// Check if the agent is full, if not, eat the fish and get a reward
    /// </summary>
    /// <param name="fishObject">The fish to eat</param>
    private void EatFish(GameObject fishObject)
    {
        if (isFull) return; // Can't eat another fish while full
        isFull = true;

        penguinArea.RemoveSpecificFish(fishObject);

        AddReward(1f);
    }

    /// <summary>
    /// 
    /// </summary>
    private void RegurgitateFish()
    {
        if (!isFull) return;
        isFull = false;

        // Spawn regurgitated fish
        GameObject regurigitatedFsih = Instantiate<GameObject>(regurgitatedFishPrefab);
        regurigitatedFsih.transform.parent = transform.parent;
        regurigitatedFsih.transform.position = baby.transform.position;
        Destroy(regurigitatedFsih, 4f);

        // Spawn heart
        GameObject heart = Instantiate<GameObject>(heartPrefab);
        heart.transform.parent = transform.parent;
        heart.transform.position = baby.transform.position + Vector3.up * 1.5f;
        Destroy(heart, 4f);

        AddReward(1f);

        if (penguinArea.FishRemaining <= 0)
        {
            EndEpisode();
        }
    }


    private void Awake()
    {
        OnEpisodeBegin();
    }

}
