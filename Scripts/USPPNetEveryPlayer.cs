#define USPPNet_string
#define USPPNet_int
#define USPPNet_bool
#define USPPNet_float
#define USPPNet_Vector2
#define USPPNet_Vector3
#define USPPNet_Vector4
#define USPPNet_Quaternion

using USPPNet;
using System;

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class USPPNetEveryPlayer : UdonSharpBehaviour
{
    public GroupManager groupManager;
    public GroupObjectSyncManager syncManager;
    public USPPNetEveryPlayerManager playerManager;
    
    private USPPNetEveryPlayerManager _usppNetEveryPlayerManager;
    [UdonSynced]
    public bool owned = false;

    private string _currZone = "";
    
    private void USPPNET_requst_unsync(int objectId, int caller)
    {
        if (!syncManager.syncedRealObjects.TryGetValue(objectId, out var dataOut))
            return;
        var obj = (GroupObjectSync)dataOut.Reference;
        
        if (obj.FakeSyncId == -1)
            return;
        if (!Networking.IsOwner(syncManager.syncedObjects[obj.FakeSyncId].gameObject))
            return;
        
        obj.UnSync();
        Debug.Log($"Unsynced: {objectId}");

        // Make sure we call this on the correct object
        playerManager.local_object.finish_sync(objectId, caller);
    }
    private void USPPNET_finish_sync(int objectId, int player)
    {
        if (Networking.LocalPlayer.playerId != player)
            return;
        
        if (!syncManager.syncedRealObjects.TryGetValue(objectId, out var dataOut))
            return;
        
        ((GroupObjectSync)dataOut.Reference).FinishSync();
        Debug.Log($"Finished Sync: {objectId}");
    }
    
    private void USPPNET_request_new_group(string zone, int playerId)
    {
        if (!Networking.IsMaster)
            return;
        _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
        var group = _usppNetEveryPlayerManager.groupManager.GetJoinableGroup(zone);
        _usppNetEveryPlayerManager.groupManager.AddPlayerToGroup(playerId, group);
        
        Debug.Log($"zone: {zone}, player: {playerId}");
    }

    private void GenericSet(string varName, int netId, object var)
    {
        if (netId != groupManager.local_group)
            return;
        if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
            return;
        var obj = (GroupCustomSync)data.Reference;

        obj.SetProgramVariable(name, var);
    }
    
    private void USPPNET_CustomSet_int(string varName, int netId, int var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_string(string varName, int netId, string var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_bool(string varName, int netId, bool var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_float(string varName, int netId, float var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_Vector2(string varName, int netId, Vector2 var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_Vector3(string varName, int netId, Vector3 var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_Vector4(string varName, int netId, Vector4 var)
    {
        GenericSet(varName, netId, var);
    }
    private void USPPNET_CustomSet_Quaternion(string varName, int netId, Quaternion var)
    {
        GenericSet(varName, netId, var);
    }

    private void USPPNET_CustomRPC(string eventName, int netId)
    {
        if (netId != groupManager.local_group)
            return;
        if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
            return;
        var obj = (GroupCustomSync)data.Reference;

        obj.SendCustomEvent(eventName);
    }


    public void SetRemoteVar(string varName, int netId, object var, bool setLocally = true)
    {
        var argType = var.GetType();
        
        if (argType == typeof(int))
            USPPNET_CustomSet_int(varName, netId, (int)var);
        if (argType == typeof(string))
            USPPNET_CustomSet_string(varName, netId, (string)var);
        if (argType == typeof(bool))
            USPPNET_CustomSet_bool(varName, netId, (bool)var);
        if (argType == typeof(float))
            USPPNET_CustomSet_float(varName, netId, (float)var);
        if (argType == typeof(Vector2))
            USPPNET_CustomSet_Vector2(varName, netId, (Vector2)var);
        if (argType == typeof(Vector3))
            USPPNET_CustomSet_Vector3(varName, netId, (Vector3)var);
        if (argType == typeof(Vector4))
            USPPNET_CustomSet_Vector4(varName, netId, (Vector4)var);
        if (argType == typeof(Quaternion))
            USPPNET_CustomSet_Quaternion(varName, netId, (Quaternion)var);
        
        if (setLocally)
            GenericSet(varName, netId, var);
    }

    public void RemoteFunctionCall(string eventName, int netId, bool callLocally = true)
    {
        USPPNET_CustomRPC(eventName, netId);
        
        if (!callLocally) return;
        if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
            return;
        
        var obj = (GroupCustomSync)data.Reference;

        obj.SendCustomEvent(eventName);
    }

    // Master 
    private void USPPNET_close_group_joins(int group)
    {
        Debug.Log($"Is Master: {Networking.IsMaster}");
        if (!Networking.IsMaster)
            return;

        groupManager.DisableJoinGroup(group);
        Debug.Log($"Group: {group} joins closed.");
    }

    // Client
    public void close_group_joinings(int group)
    {
        if (!Networking.IsMaster)
        {
            USPPNET_close_group_joins(group);
            RequestSerialization();
        }
        else
            groupManager.DisableJoinGroup(group);
    }

    public void finish_sync(int objectId, int player)
    {
        USPPNET_finish_sync(objectId, player);
        RequestSerialization();
    }
    
    public void request_new_group(string zone, int playerId)
    {
        if (_currZone == zone)
            return;
        _currZone = zone;
        syncManager.LocalDropAll();
        if (Networking.IsMaster)
        {
            _usppNetEveryPlayerManager.groupManager.RemovePlayerFromGroups(playerId);
            var group = _usppNetEveryPlayerManager.groupManager.GetJoinableGroup(zone);
            _usppNetEveryPlayerManager.groupManager.AddPlayerToGroup(playerId, group);
            return;
        }
        
        USPPNET_request_new_group(zone, playerId);
        RequestSerialization();
    }
    
    public void request_unsync(int objectId, int caller)
    {
        USPPNET_requst_unsync(objectId, caller);
        RequestSerialization();
        Debug.Log("Request unsync sent");
    }

    private void Start()
    {
        _usppNetEveryPlayerManager = transform.parent.GetComponent<USPPNetEveryPlayerManager>();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!Networking.IsMaster)
            return;
        
        if (playerManager.local_object == this)
            return;
        
        if (Networking.IsOwner(gameObject))
            owned = false;
    }

    public override void OnDeserialization()
    {
        // USPPNet OnDeserialization
    }
    
    public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
    {
        // USPPNet OnPostSerialization
    }

    // USPPNet Init
}