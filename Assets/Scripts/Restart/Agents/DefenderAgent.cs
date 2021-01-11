using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;


public class DefenderAgent : Agent
{
    public ArmyNew army;


    private void Update()
    {
        if (!Application.isPlaying)
            GetComponent<BehaviorParameters>().BrainParameters.VectorObservationSize =
                5 * (army.infantryStats.Count + army.cavalryStats.Count) +
                6 * (army.archersStats.Count) +
                3;
    }


    public override void OnEpisodeBegin()
    {
    }



    private void AddMeleeInformation(VectorSensor sensor, UnitNew u)
    {
        sensor.AddObservation(u.ID);
        sensor.AddObservation(new Vector2(u.transform.position.x, u.transform.position.z));
        sensor.AddObservation(new Vector2(u.transform.forward.x, u.transform.forward.z));
    }

    private void AddRangedInformation(VectorSensor sensor, ArcherNew u)
    {
        sensor.AddObservation(u.ID);
        sensor.AddObservation(new Vector2(u.transform.position.x, u.transform.position.z));
        sensor.AddObservation(new Vector2(u.transform.forward.x, u.transform.forward.z));
        sensor.AddObservation(u.range);
    }


    public override void CollectObservations(VectorSensor sensor)
    {


        sensor.AddObservation(army.infantryUnits.Count);
        foreach (var i in army.infantryUnits)
            AddMeleeInformation(sensor, i);


        sensor.AddObservation(army.archerUnits.Count);
        foreach (var a in army.archerUnits)
            AddRangedInformation(sensor, a);


        sensor.AddObservation(army.cavalryUnits.Count);
        foreach (var c in army.cavalryUnits)
            AddMeleeInformation(sensor, c);


    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {

        Debug.Log("Heuristica");

    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {


        SetReward(1.0f);

    }



}
