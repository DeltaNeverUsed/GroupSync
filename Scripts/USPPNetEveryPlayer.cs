
#define USPPNet_string
#define USPPNet_int

using USPPNet;
using System;

using UdonSharp;
using UnityEngine;
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

    private void temp_color()
    {
        var r = GetComponent<Renderer>();
        if (r == null)
            return;

        if (Networking.IsOwner(gameObject))
        {
            r.material.color = Networking.IsMaster ? Color.red : Color.green;
        }
        else
        {
            r.material.color = Color.black;
        }

    }

    private void Start()
    {
        _usppNetEveryPlayerManager = transform.parent.GetComponent<USPPNetEveryPlayerManager>();
    }

    public int t = 0;

    private void FixedUpdate()
    {
        t++;
        if(t%60 == 0)
            temp_color();
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