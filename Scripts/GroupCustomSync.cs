using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupCustomSync : UdonSharpBehaviour
{
    [Header("Network stuff")]
    public int networkId = -1;
    public GroupObjectSyncManager gosm;
    public USPPNetEveryPlayerManager psm;
    
    [Space(10)]

    private bool _startedNet;

    protected void StartNet()
    {
        if (_startedNet)
            return;
        _startedNet = true;
        
        Debug.Log("Okayy, that did done did the thing i was hoping");
        if (networkId == -1)
            Debug.LogError("networkId is -1 on " + gameObject.name + "Please select to object to generate a new networkId, or create select one manually");
        if (gosm == null)
        {
            gosm = GameObject.Find("GroupObjectSyncManager").GetComponent<GroupObjectSyncManager>();
            if (gosm == null)
                Debug.LogError("Couldn't find GroupObjectSyncManager");
        }
        if (psm == null)
        {
            psm = GameObject.Find("EachPlayerUSPPNet").GetComponent<USPPNetEveryPlayerManager>();
            if (psm == null)
                Debug.LogError("Couldn't find USPPNetEveryPlayer");
        }
        
        gosm.AddCustomObject(this);
        
    }

    public void SetVariableInLocalGroup(string name, object value, bool setLocally = true, bool autoSerialize = true)
    {
        psm.local_object.SetRemoteVar(name, networkId, value, setLocally);
        if (autoSerialize)
            psm.local_object.RequestSerialization();
    }
    public void CallFunctionInLocalGroup(string name, bool callLocally = true, bool autoSerialize = true)
    {
        psm.local_object.RemoteFunctionCall(name, networkId, callLocally);
        if (autoSerialize)
            psm.local_object.RequestSerialization();
    }
    
}
