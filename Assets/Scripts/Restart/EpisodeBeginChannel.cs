using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using System.Text;
using System;

public class StringLogSideChannel : SideChannel
{




    public StringLogSideChannel()
    {
        ChannelId = new Guid("621f0a70-4f87-11ea-a6bf-784f4387d1f7");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        var receivedString = msg.ReadString();
        Debug.Log("From Python : " + receivedString);
    }

    public void SendArmyInfo(ArmyNew agentArmy, ArmyNew enemyArmy)
    {

        using(var msgOut = new OutgoingMessage())
        {
            msgOut.WriteInt32(agentArmy.infantryUnits.Count);
            msgOut.WriteInt32(agentArmy.archerUnits.Count);
            msgOut.WriteInt32(agentArmy.cavalryStats.Count);


            msgOut.WriteInt32(enemyArmy.infantryUnits.Count);
            msgOut.WriteInt32(enemyArmy.archerUnits.Count);
            msgOut.WriteInt32(enemyArmy.cavalryStats.Count);

        }
    }
    public void SendDebugStatementToPython(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            var stringToSend = type.ToString() + ": " + logString + "\n" + stackTrace;
            using (var msgOut = new OutgoingMessage())
            {
                msgOut.WriteString(stringToSend);
                QueueMessageToSend(msgOut);
            }
        }
    }


}


public class EpisodeBeginChannel : MonoBehaviour
{
    StringLogSideChannel stringChannel;
    public void Awake()
    {
        // We create the Side Channel
        stringChannel = new StringLogSideChannel();

        // When a Debug.Log message is created, we send it to the stringChannel
        Application.logMessageReceived += stringChannel.SendDebugStatementToPython;

        // The channel must be registered with the SideChannelManager class
        SideChannelManager.RegisterSideChannel(stringChannel);
    }
    public void OnDestroy()
    {
        // De-register the Debug.Log callback
        Application.logMessageReceived -= stringChannel.SendDebugStatementToPython;
        if (Academy.IsInitialized)
        {
            SideChannelManager.UnregisterSideChannel(stringChannel);
        }
    }


    public void SendArmyInfo(ArmyNew agentArmy, ArmyNew enemyArmy)
    {
        stringChannel.SendArmyInfo(agentArmy, enemyArmy);
    }
    public void Update()
    {
        // Optional : If the space bar is pressed, raise an error !
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogError("This is a fake error. Space bar was pressed in Unity.");
        }
    }
}
