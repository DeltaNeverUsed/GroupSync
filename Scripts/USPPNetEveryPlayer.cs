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

    private void USPPNET_request_sync(int objectId, int caller)
    {
        if (!Networking.IsMaster)
            return;
        playerManager.local_object.request_sync_master(objectId, caller);
    }
    
    private void USPPNET_requst_transfer_owner(int objectId, int caller)
    {
        requst_transfer_owner_all(objectId, caller);
    }

    private void requst_transfer_owner_all(int objectId, int caller)
    {
        if (!syncManager.syncedRealObjects.TryGetValue(objectId, out var dataOut))
            return;
        var obj = (GroupObjectSync)dataOut.Reference;
        
        if (obj.fakeSyncId == -1)
            return;
        var fakeSyncObject = syncManager.syncedObjects[obj.fakeSyncId].gameObject;
        if (!Networking.IsOwner(fakeSyncObject))
            return;
        
        Debug.Log($"transferred ownership: {objectId}");

        // Set the new owner faster 
        var playerCaller = VRCPlayerApi.GetPlayerById(caller);
        if (playerCaller.IsValid())
            Networking.SetOwner(playerCaller, fakeSyncObject);
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

    private void GenericSet(int group, string varName, int netId, object var)
    {
        if (group != groupManager.local_group)
            return;
        if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
            return;
        var obj = (GroupCustomSync)data.Reference;

        obj.SetProgramVariable(varName, var);
    }
    
    private void USPPNET_CustomSet_int(int group, string varName, int netId, int var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_string(int group, string varName, int netId, string var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_bool(int group, string varName, int netId, bool var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_float(int group, string varName, int netId, float var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_Vector2(int group, string varName, int netId, Vector2 var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_Vector3(int group, string varName, int netId, Vector3 var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_Vector4(int group, string varName, int netId, Vector4 var)
    {
        GenericSet(group, varName, netId, var);
    }
    private void USPPNET_CustomSet_Quaternion(int group, string varName, int netId, Quaternion var)
    {
        GenericSet(group, varName, netId, var);
    }

    private void USPPNET_CustomRPC(int group, string eventName, int netId)
    {
        if (group != -1 && group != groupManager.local_group)
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
            USPPNET_CustomSet_int(groupManager.local_group, varName, netId, (int)var);
        if (argType == typeof(string))
            USPPNET_CustomSet_string(groupManager.local_group, varName, netId, (string)var);
        if (argType == typeof(bool))
            USPPNET_CustomSet_bool(groupManager.local_group, varName, netId, (bool)var);
        if (argType == typeof(float))
            USPPNET_CustomSet_float(groupManager.local_group, varName, netId, (float)var);
        if (argType == typeof(Vector2))
            USPPNET_CustomSet_Vector2(groupManager.local_group, varName, netId, (Vector2)var);
        if (argType == typeof(Vector3))
            USPPNET_CustomSet_Vector3(groupManager.local_group, varName, netId, (Vector3)var);
        if (argType == typeof(Vector4))
            USPPNET_CustomSet_Vector4(groupManager.local_group, varName, netId, (Vector4)var);
        if (argType == typeof(Quaternion))
            USPPNET_CustomSet_Quaternion(groupManager.local_group, varName, netId, (Quaternion)var);
        
        if (setLocally)
            GenericSet(groupManager.local_group, varName, netId, var);
    }

    public void RemoteFunctionCall(string eventName, int netId, bool callLocally = true)
    {
        USPPNET_CustomRPC(groupManager.local_group, eventName, netId);
        
        if (!callLocally) return;
        if (!syncManager.syncedCustomObjects.TryGetValue(netId, out var data))
            return;
        
        var obj = (GroupCustomSync)data.Reference;

        obj.SendCustomEvent(eventName);
    }

    private void USPPNET_set_fake_sync(int caller, int fakeSyncId, int objectId)
    {
        set_fake_sync_target(caller, fakeSyncId, objectId);
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

    private void set_fake_sync_target(int caller, int fakeSyncId, int objectId)
    {
        if (Networking.LocalPlayer.playerId != caller)
            return;
        
        var fakeSync = syncManager.syncedObjects[fakeSyncId];
        if (!Networking.IsOwner(fakeSync.gameObject))
        {
            Debug.Log("Not owner of object?");
            Networking.SetOwner(Networking.LocalPlayer, fakeSync.gameObject);
        }

        fakeSync.group = groupManager.local_group;
        fakeSync.target = objectId;
    }

    private void request_sync_master(int objectId, int caller)
    {
        if (!syncManager.syncedRealObjects.TryGetValue(objectId, out var dataOut))
            return;
        var obj = (GroupObjectSync)dataOut.Reference;
        
        if (obj.fakeSyncId != -1 && obj.fakeSync.target == obj.networkId && !Networking.IsOwner(VRCPlayerApi.GetPlayerById(caller), obj.fakeSync.gameObject))
        {
            requst_transfer_owner(obj.networkId, caller);
            return;
        }
        
        var newSyncId = syncManager.GetFakeSync();
        if (newSyncId == -1)
            return;
        
        var fakeSync = syncManager.syncedObjects[newSyncId];

        var playerCaller = VRCPlayerApi.GetPlayerById(caller);
        if (playerCaller.IsValid())
            Networking.SetOwner(playerCaller, fakeSync.gameObject);

        if (caller == Networking.LocalPlayer.playerId)
            set_fake_sync_target(caller, newSyncId, objectId);
        else
        {
            USPPNET_set_fake_sync(caller, newSyncId, objectId);
            RequestSerialization();
        }        
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

    public void request_new_group(string zone, int playerId)
    {
        if (_currZone == zone)
            return;
        _currZone = zone;
        syncManager.LocalDropAll();
        
        // This needs to be called locally, and this doesn't actually get called on group leave,
        // Just on requesting a new one, which might as well be the same thing.
        for (var index = 0; index < _usppNetEveryPlayerManager.groupManager.leaveGroupCallbacks.Count; index++)
        {
            var caller = _usppNetEveryPlayerManager.groupManager.leaveGroupCallbacks[index];
            if ((UdonSharpBehaviour)caller.Reference != null)
                ((UdonSharpBehaviour)caller.Reference).SendCustomEvent("LeaveCallback");
        }
        
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
    
    public void requst_transfer_owner(int objectId, int caller)
    {
        USPPNET_requst_transfer_owner(objectId, caller);
        requst_transfer_owner_all(objectId, caller);
        RequestSerialization();
        Debug.Log("Request unsync sent");
    }
    
    public void request_start_sync(int objectId, int caller)
    {
        if (!syncManager.syncedRealObjects.TryGetValue(objectId, out var dataOut))
            return;
        
        if (Networking.IsMaster)
            request_sync_master(objectId, caller);
        else
        {
            USPPNET_request_sync(objectId, caller);
            RequestSerialization();
        }        
        Debug.Log("Requested sync started");
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