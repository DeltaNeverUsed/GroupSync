﻿
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
    public int maxGroups = 12;
    public int maxPlayersPerGroup = 10;

    private int _groupsArraySize;
    
    public string[] joinable = Array.Empty<string>();
    [UdonSynced] public short[] groups = Array.Empty<short>();

    public DataList leaveGroupCallbacks = new DataList();

    public int local_group = -1;

    private void USPPNET_tell_client_group(int player, int group)
    {
        if (Networking.LocalPlayer.playerId != player)
            return;
        local_group = group;
        Debug.Log($"local_group updated: {local_group}");
    }

    private void Start()
    {
        _groupsArraySize = maxGroups * maxPlayersPerGroup;
        
        groups = new short[_groupsArraySize];
        joinable = new string[maxGroups];
        for (int i = 0; i < groups.Length; i++)
            groups[i] = -1;
        for (int i = 0; i < maxGroups; i++)
            joinable[i] = "";
    }

    private int _cleanupCheck;
    private void FixedUpdate()
    {
        if (!Networking.IsMaster)
            return;
        
        _cleanupCheck++;
        if (_cleanupCheck % 240 == 0)
            CheckGroupsEmpty();
    }

    public int GroupAndPlayer2Index(int group, int player)
    {
        return group * maxPlayersPerGroup + player;
    }

    public bool IsPlayerInGroup(int playerId, int group)
    {
        if (groups.Length != _groupsArraySize)
            return false;
        if (group >= maxGroups)
            return false;

        for (int i = 0; i < maxPlayersPerGroup; i++)
            if (groups[GroupAndPlayer2Index(group, i)] == playerId)
                return true;

        return false;
    }
    
    public int GetPlayerGroup(int playerId)
    {
        for (int x = 0; x < maxGroups; x++)
            for (int y = 0; y < maxPlayersPerGroup; y++)
                if (groups[GroupAndPlayer2Index(x, y)] == playerId)
                    return x;
        return -1;
    }

    public bool IsPlayerInLocalGroup(int playerId)
    {
        if (local_group != -1)
            return IsPlayerInGroup(playerId, local_group);
        return false;
    }

    private void CheckGroupsEmpty()
    {
        for (int x = 0; x < maxGroups; x++)
        {
            var clear = true;
            for (int y = 0; y < maxPlayersPerGroup; y++)
            {
                if (groups[GroupAndPlayer2Index(x, y)] == -1) continue;
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

        RequestSerialization();
    }

    public void AddPlayerToGroup(int playerId, int group)
    {
        if (group < 0 || group >= maxGroups)
            return;
        if (GetPlayerGroup(playerId) != -1)
            return;

        var added = false;
        for (int i = 0; i < maxPlayersPerGroup; i++)
        {
            if(groups[GroupAndPlayer2Index(group, i)] != -1)
                continue;

            groups[GroupAndPlayer2Index(group, i)] = (short)playerId;
            added = true;
            
            break;
        }

        if (added)
        {
            if (!VRCPlayerApi.GetPlayerById(playerId).IsOwner(gameObject))
                USPPNET_tell_client_group(playerId, group);
            else
                local_group = group;
        }
        
        RequestSerialization();
    }

    public int GetJoinableGroup(string zone)
    {
        if (joinable.Length < maxGroups)
            return -1;
        
        var firstChoice = -1; // First choice is a group with the same zone name as the one requested.
        var secondChoice = -1; // Second choice is an empty zone.
        
        for (var i = 0; i < maxGroups; i++)
        {
            if (joinable[i] == zone)
                firstChoice = i;
            if (joinable[i] == "" && secondChoice == -1)
                secondChoice = i;
        }

        if (firstChoice != -1 && groups[GroupAndPlayer2Index(firstChoice, maxPlayersPerGroup-1)] == -1) return firstChoice;
        
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
    
    /// <summary>
    /// Disables joining for specified Group
    /// DON'T CALL UNLESS YOU ARE HOST
    /// </summary>
    /// <param name="group"></param>
    public void DisableJoinGroup(int group)
    {
        if (group < 0 || group >= joinable.Length)
            return;
        joinable[group] = "_UnJoinable";
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!Networking.IsMaster)
            return;

        var updated = false;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == -1) continue;
            var plr = VRCPlayerApi.GetPlayerById(groups[i]);
            if (plr != null && plr.IsValid()) continue; // Check if player is valid
            
            groups[i] = -1;
            updated = true;
        }
        
        if (updated)
            RequestSerialization();
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
