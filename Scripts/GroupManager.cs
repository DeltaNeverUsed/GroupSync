
#define USPPNet_int

using USPPNet;

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GroupManager : UdonSharpBehaviour
{
    public GroupObjectSyncManager syncManager;
    
    public int maxGroups = 12;
    public int maxPlayersPerGroup = 10;
    
    public string[] joinable = Array.Empty<string>();
    [UdonSynced] public int[] groups = Array.Empty<int>();

    public DataList leaveGroupCallbacks = new DataList();

    public int local_group = -1;

    private void USPPNET_tell_client_group(int player, int group)
    {
        //var guy = VRCPlayerApi.GetPlayerById(player);
        //Debug.Log($"Player: ({guy.displayName}, {guy.playerId}, Local: {Networking.LocalPlayer.playerId}), Group: {group}");
        if (Networking.LocalPlayer.playerId != player)
            return;
        syncManager.ResetRealFakeIds();
        local_group = group;
        Debug.Log($"local_group updated: {local_group}");
    }

    private void Start()
    {
        if (!Networking.IsOwner(gameObject))
            return;
        
        groups = new int[maxGroups * maxPlayersPerGroup];
        joinable = new string[maxGroups];
        for (int i = 0; i < groups.Length; i++)
            groups[i] = -1;
        for (int i = 0; i < maxGroups; i++)
            joinable[i] = "";
        
    }

    private int _cleanupCheck;
    private void FixedUpdate()
    {

        if (!Networking.IsOwner(gameObject))
            return;
        
        _cleanupCheck++;
        if (_cleanupCheck % 60 == 0)
            CheckGroupsEmpty();
    }

    public int Pos2Index(int x, int y)
    {
        return x * maxPlayersPerGroup + y;
    }

    public bool IsPlayerInGroup(int playerId, int group)
    {
        if (groups.Length != maxGroups * maxPlayersPerGroup)
            return false;
        if (group >= maxGroups || groups.Length < maxPlayersPerGroup * maxGroups)
            return false;

        for (int i = 0; i < maxPlayersPerGroup; i++)
            if (groups[Pos2Index(group, i)] == playerId)
                return true;

        return false;
    }

    public bool IsPlayerInLocalGroup(int playerId)
    {
        return IsPlayerInGroup(playerId, local_group);
    }

    private void CheckGroupsEmpty()
    {
        for (int x = 0; x < maxGroups; x++)
        {
            var clear = true;
            for (int y = 0; y < maxPlayersPerGroup; y++)
            {
                if (groups[Pos2Index(x, y)] == -1) continue;
                clear = false;
                break;
            }

            if (clear)
                joinable[x] = "";
        }
    }
    public void RemovePlayerFromGroups(int playerId)
    {
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == playerId)
                groups[i] = -1;
        }

        for (var index = 0; index < leaveGroupCallbacks.Count; index++)
        {
            var caller = leaveGroupCallbacks[index];
            ((UdonSharpBehaviour)caller.Reference).SendCustomEvent("LeaveCallback");
        }

        RequestSerialization();
    }

    public void AddPlayerToGroup(int playerId, int group)
    {
        if (group < 0)
            return;

        var added = false;
        for (int i = 0; i < maxPlayersPerGroup; i++)
        {
            if(groups[Pos2Index(group, i)] != -1)
                continue;

            groups[Pos2Index(group, i)] = playerId;
            added = true;
            
            break;
        }

        if (added)
            if (!VRCPlayerApi.GetPlayerById(playerId).IsOwner(gameObject))
                USPPNET_tell_client_group(playerId, group);
            else
                local_group = group;
        RequestSerialization();
    }

    public int GetJoinableGroup(string zone)
    {
        var secondChoice = -1;
        for (int i = 0; i < maxGroups; i++)
        {
            if (joinable[i] == zone)
                return i;
            if (joinable[i] == "" && secondChoice == -1)
                secondChoice = i;
        }

        joinable[secondChoice] = zone;
        return secondChoice;
       
    }

    public void SubLeaveGroupCallback(UdonSharpBehaviour caller)
    {
        if (leaveGroupCallbacks.Contains(caller))
        {
            Debug.LogError("Call back already registered");
            return;
        }
        
        leaveGroupCallbacks.Add(caller);
    }
    
    // Only call from host
    public void DisableJoinGroup(int group)
    {
        if (group == -1)
            return;
        joinable[group] = "_UnJoinable";
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
