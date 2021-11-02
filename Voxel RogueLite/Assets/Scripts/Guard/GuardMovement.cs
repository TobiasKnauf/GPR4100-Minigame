using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardMovement : MonoBehaviour
{
    private GuardBehaviour gBehaviour;
    private GuardVision gVision;
    private GuardHearing gHearing;
    public NavMeshAgent Agent { get { return agent; } }
    public float MaxDistanceToPlayer { get { return maxDistanceToPlayer; } }
    private NavMeshAgent agent;
    private List<Transform> patrolPoints = new List<Transform>(); //list of all patrol points of the guard
    [SerializeField]
    [Tooltip("The distance at which the guard stops moving towards the player")]
    private float maxDistanceToPlayer; //the distance at which the guard stops moving towards the player


    private Vector3 desiredLocation;
    private void Awake()
    {
        gBehaviour = GetComponent<GuardBehaviour>();
        agent = GetComponent<NavMeshAgent>();
        gVision = GetComponentInChildren<GuardVision>();
        gHearing = GetComponentInChildren<GuardHearing>();

        InitializePatrolPoints();
    }

    private void InitializePatrolPoints()
    {
        foreach (Transform child in transform)
        {
            //if it's a patrol point
            if (child.CompareTag("PatrolPoint"))
            {
                patrolPoints.Add(child.transform); //add it to the list
            }
        }
        //go through every Transform on patrolPoints
        foreach (Transform point in patrolPoints)
        {
            point.SetParent(null); //set parent of point to null
        }
    }

    private void Update()
    {
        CalculateAction();
        Debug.DrawLine(transform.position, agent.destination, Color.red); //Debug: Draw line to current destination
    }

    private void CalculateAction()
    {
        //Guard is chasing
        if (gBehaviour.CurrentBehaviour != GuardBehaviour.EBehaviour.patrolling)
            MoveTowardsLocation(desiredLocation); //move to location  

        //Guard is patrolling
        if (gBehaviour.CurrentBehaviour == GuardBehaviour.EBehaviour.patrolling)
            Patrol();
    }

    /// <summary>
    /// This function sends all guards back to their patroling path once one guard reached the location of the alarm
    /// </summary>
    private void GuardClearedAlarm()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        foreach (var guard in gm.Guards)
        {
            if (guard.gameObject != null)
            {
                GuardMovement move = guard.gameObject.GetComponent<GuardMovement>(); //get Movement script of guard
                guard.CurrentBehaviour = GuardBehaviour.EBehaviour.patrolling; //set current behaviour to patroling
                move.agent.ResetPath(); //reset path so guard can patrol
                guard.Alarmed = false; //guard is no longer alarmed
            }
        }
    }

    /// <summary>
    /// Calculates if the guard can reach its destination
    /// </summary>
    /// <param name="_targetLocation"> The position of the target Location</param>
    /// <returns></returns>
    private bool PathIsValid(Vector3 _targetLocation)
    {
        NavMeshPath path = new NavMeshPath();

        //if guard can reach its destination
        if (agent.CalculatePath(_targetLocation, path) && path.status == NavMeshPathStatus.PathComplete)
            return true;
        
        else
            return false;
    }

    /// <summary>
    /// Calculates the desired Agent Location based on the Guards behaviour
    /// </summary>
    private void DesiredLocCalc()
    {
        if (gBehaviour.CurrentBehaviour == GuardBehaviour.EBehaviour.chasing)
            desiredLocation = gVision.LastKnownPlayerPos;

        else if (gBehaviour.CurrentBehaviour == GuardBehaviour.EBehaviour.searching)
            desiredLocation = gHearing.NoiseLocation;
    }

    /// <summary>
    /// Makes the guard move towards a position based on its behaviour
    /// </summary>
    /// <param name="_location">The position of the desired Location</param>
    private void MoveTowardsLocation(Vector3 _location)
    {
        DesiredLocCalc();
        agent.SetDestination(_location); //set ai destination to location

        if (PathIsValid(_location))
        {
            if (gBehaviour.CurrentBehaviour == GuardBehaviour.EBehaviour.chasing)
                MoveToPlayerCalc(_location);

            else if (gBehaviour.CurrentBehaviour == GuardBehaviour.EBehaviour.searching)
                MoveToNoiseCalc(_location);
        }

        else
            gBehaviour.CurrentBehaviour = GuardBehaviour.EBehaviour.patrolling;
    }

    /// <summary>
    /// Makes the guard move towards the player's location
    /// </summary>
    /// <param name="_location">The position of the desired Location</param>
    private void MoveToPlayerCalc(Vector3 _location)
    {
        //if distance to the players location is greater than 3
        if (Vector3.Distance(transform.position, _location) >= maxDistanceToPlayer)
        {
            agent.isStopped = false; //start to move
        }

        //if guard sees player and is on maxDistance
        if (Vector3.Distance(transform.position, _location) <= maxDistanceToPlayer && gVision.SeesPlayer)
        {
            transform.LookAt(_location); //rotate to the player
            agent.isStopped = true; //stop movement
        }

        //if guard does not see player and is on maxDistance
        if (Vector3.Distance(transform.position, _location) <= maxDistanceToPlayer && !gVision.SeesPlayer)
        {
            agent.isStopped = false;
        }

        if (Vector3.Distance(transform.position, gVision.LastKnownPlayerPos) <= 1)
        {
            gBehaviour.CurrentBehaviour = GuardBehaviour.EBehaviour.patrolling;

            if (gBehaviour.Alarmed)
                GuardClearedAlarm(); //clear alarm for all guards
        }
    }    

    /// <summary>
    /// Makes the guard move towards the origin of the noise
    /// </summary>
    /// <param name="_location">The position of the desired location</param>
    private void MoveToNoiseCalc(Vector3 _location)
    {
        agent.isStopped = false;

        if (Vector3.Distance(transform.position, _location) <= 1f)
        {
            gBehaviour.CurrentBehaviour = GuardBehaviour.EBehaviour.patrolling;

            if (gBehaviour.Alarmed)
                GuardClearedAlarm(); //clear alarm for all guards
        }
    }

    /// <summary>
    /// Sends the guard to his next patrol point
    /// </summary>
    private void Patrol()
    {
        //if agent has no path (so that the next patrol point will only be chosen when the guard reached the current destination)
        if (!agent.hasPath)
        {
            //search random patrolpoint and set destination to it's position
            int p = Random.Range(0, patrolPoints.Count);
            agent.SetDestination(patrolPoints[p].position);
            agent.isStopped = false; //start movement
        }
    }
}
