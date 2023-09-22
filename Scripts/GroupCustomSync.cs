using System;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public abstract class GroupCustomSync : UdonSharpBehaviour
{
    public int networkId = -1;
    public bool forceGlobalSync;
    [NonSerialized] public GroupObjectSyncManager gosm;
    [NonSerialized] public USPPNetEveryPlayerManager psm;

    internal bool StartedNet;

    public bool StartNet()
    {
        if (StartedNet)
            return true;
        
        if (networkId == -1)
        {
            Debug.LogError("networkId is -1 on " + gameObject.name +
                           "Please select to object to generate a new networkId, or create select one manually");
            return false;
        }        
        if (gosm == null)
        {
            gosm = GameObject.Find("GroupObjectSyncManager").GetComponent<GroupObjectSyncManager>();
            if (gosm == null)
            {
                Debug.LogError("Couldn't find GroupObjectSyncManager");
                return false;
            }
        }
        if (psm == null)
        {
            psm = GameObject.Find("EachPlayerUSPPNet").GetComponent<USPPNetEveryPlayerManager>();
            if (psm == null)
            {
                Debug.LogError("Couldn't find USPPNetEveryPlayer");
                return false;
            }
        }
        
        gosm.AddCustomObject(this);
        
        StartedNet = true;

        return true;
    }

    public void OnDestroy()
    {
        if (StartedNet)
            gosm.RemoveCustomObject(this);
    }

    private bool _dontExists = true;
    private bool CheckLocalObject()
    {
        if (!StartedNet)
            return true;
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
        if (CheckLocalObject())
            return;
        SetVariable(-2, name, value, setLocally, autoSerialize);
    }
    public void CallFunctionInAllGroups(string name, bool callLocally = true, bool autoSerialize = true)
    {
        if (CheckLocalObject())
            return;
        CallFunction(-2, name, callLocally, autoSerialize);
    }

    public void SetVariableInLocalGroup(string name, object value, bool setLocally = true, bool autoSerialize = true)
    {
        if (CheckLocalObject())
            return;
        SetVariable(forceGlobalSync ? -2 : psm.groupManager.local_group, name, value, setLocally, autoSerialize);
    }
    public void CallFunctionInLocalGroup(string name, bool callLocally = true, bool autoSerialize = true)
    {
        if (CheckLocalObject())
            return;
        CallFunction(forceGlobalSync ? -2 : psm.groupManager.local_group, name, callLocally, autoSerialize);
    }

    private void CallFunction(int group, string name, bool callLocally = true, bool autoSerialize = true)
    {
        psm.local_object.RemoteFunctionCall(group, name, networkId, callLocally);
        if (autoSerialize)
            psm.local_object.RequestSerialization();
    }

    private void SetVariable(int group, string name, object value, bool setLocally = true, bool autoSerialize = true)
    {
        psm.local_object.SetRemoteVar(group, name, networkId, value, setLocally);
        if (autoSerialize)
            psm.local_object.RequestSerialization();
    }

    /// <summary>
    /// The call back gets called whenever the group changed is request, and before the group is changed!
    /// </summary>
    protected void SubLeaveGroupCallback()
    {
        psm.groupManager.SubLeaveGroupCallback(this);
    }

    /// <summary>
    /// Locks the group so that new people can't join it.
    /// </summary>
    protected void CloseCurrentGroup()
    {
        psm.local_object.close_group_joinings(psm.groupManager.local_group);
    }
    
}
