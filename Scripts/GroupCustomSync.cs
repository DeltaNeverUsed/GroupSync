using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupCustomSync : UdonSharpBehaviour
{
    [Header("Network stuff")]
    public int networkId = -1;
    public GroupObjectSyncManager gosm;
    public USPPNetEveryPlayerManager psm;

    private bool _startedNet;

    protected void StartNet()
    {
        if (_startedNet)
            return;
        _startedNet = true;
        
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

    private bool _dontExists = true;
    private bool CheckLocalObject()
    {
        if (_dontExists)
        {
            if (psm.local_object == null)
                return true;
        }
        else
            return false;
        _dontExists = false;
        return false;
    }
    
    public void SetVariableInAllGroups(string name, object value, bool setLocally = true, bool autoSerialize = true)
    {
        SetVariable(-2, name, value, setLocally, autoSerialize);
    }
    public void CallFunctionInAllGroups(string name, bool callLocally = true, bool autoSerialize = true)
    {
        CallFunction(-2, name, callLocally, autoSerialize);
    }

    public void SetVariableInLocalGroup(string name, object value, bool setLocally = true, bool autoSerialize = true)
    {
        SetVariable(psm.groupManager.local_group, name, value, setLocally, autoSerialize);
    }
    public void CallFunctionInLocalGroup(string name, bool callLocally = true, bool autoSerialize = true)
    {
        CallFunction(psm.groupManager.local_group, name, callLocally, autoSerialize);
    }

    private void CallFunction(int group, string name, bool callLocally = true, bool autoSerialize = true)
    {
        if (CheckLocalObject())
            return;
        
        psm.local_object.RemoteFunctionCall(group, name, networkId, callLocally);
        if (autoSerialize)
            psm.local_object.RequestSerialization();
    }

    private void SetVariable(int group, string name, object value, bool setLocally = true, bool autoSerialize = true)
    {
        if (CheckLocalObject())
            return;
        
        psm.local_object.SetRemoteVar(group, name, networkId, value, setLocally);
        if (autoSerialize)
            psm.local_object.RequestSerialization();
    }

    public void SubLeaveGroupCallback()
    {
        psm.groupManager.SubLeaveGroupCallback(this);
    }
    
}
