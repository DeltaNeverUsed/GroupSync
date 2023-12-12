#define USPPNet_short

using USPPNet;

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GroupManager : UdonSharpBehaviour
    {
        public short maxGroups = 12;
        public short maxPlayersPerGroup = 10;

        private int _groupsArraySize;
    
        [HideInInspector] public string[] joinable = Array.Empty<string>();
        [HideInInspector] [UdonSynced] public short[] groups = Array.Empty<short>();

        [HideInInspector] public DataList leaveGroupCallbacks = new DataList();

        [HideInInspector] public short local_group = -1;

        private void USPPNET_tell_client_group(short player, short group)
        {
            if (Networking.LocalPlayer.playerId != player)
                return;
            local_group = group;
            Debug.Log($"local_group updated: {local_group}");
        }

        public void Bootstrap()
        {
            _groupsArraySize = maxGroups * maxPlayersPerGroup;
        
            groups = new short[_groupsArraySize];
            joinable = new string[maxGroups];
            for (var i = 0; i < groups.Length; i++)
                groups[i] = -1;
            for (var i = 0; i < maxGroups; i++)
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

        public int GroupAndPlayer2Index(short group, short player)
        {
            return group * maxPlayersPerGroup + player;
        }

        public bool IsPlayerInGroup(short playerId, short group)
        {
            if (groups.Length != _groupsArraySize)
                return false;
            if (group >= maxGroups || group < 0)
                return false;

            for (short i = 0; i < maxPlayersPerGroup; i++)
                if (groups[GroupAndPlayer2Index(group, i)] == playerId)
                    return true;

            return false;
        }
    
        public int GetPlayerGroup(short playerId)
        {
            for (short x = 0; x < maxGroups; x++)
            for (short y = 0; y < maxPlayersPerGroup; y++)
                if (groups[GroupAndPlayer2Index(x, y)] == playerId)
                    return x;
            return -1;
        }

        public bool IsPlayerInLocalGroup(short playerId)
        {
            if (local_group != -1)
                return IsPlayerInGroup(playerId, local_group);
            return false;
        }
        
        private static T[] Add<T>(T[] array, T item)
        {
            var len = array.Length + 1;
            var tempArray = new T[len];
            array.CopyTo(tempArray, 0);
            tempArray[len-1] = item;
            return tempArray;
        }

        public short[] GetPlayersInGroup(short group)
        {
            var players = new short[0];
            
            if (groups.Length != _groupsArraySize)
                return players;
            if (group >= maxGroups || group < 0)
                return players;
            
            for (short i = 0; i < maxPlayersPerGroup; i++)
            {
                var player = groups[GroupAndPlayer2Index(group, i)];
                if (player != -1)
                    players = Add(players, player);
            }

            return players;
        }

        private void CheckGroupsEmpty()
        {
            for (short x = 0; x < maxGroups; x++)
            {
                var clear = true;
                for (short y = 0; y < maxPlayersPerGroup; y++)
                {
                    if (groups[GroupAndPlayer2Index(x, y)] == -1) continue;
                    clear = false;
                    break;
                }

                if (clear)
                    joinable[x] = "";
            }
        }
        public void RemovePlayerFromGroups(short playerId)
        {
            for (var i = 0; i < groups.Length; i++)
            {
                if (groups[i] == playerId)
                    groups[i] = -1;
            }

            RequestSerialization();
        }

        public void AddPlayerToGroup(short playerId, short group)
        {
            if (group < 0 || group >= maxGroups)
                return;
            if (GetPlayerGroup(playerId) != -1)
                return;

            var added = false;
            for (short i = 0; i < maxPlayersPerGroup; i++)
            {
                if(groups[GroupAndPlayer2Index(group, i)] != -1)
                    continue;

                groups[GroupAndPlayer2Index(group, i)] = playerId;
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

        public short GetJoinableGroup(string zone)
        {
            if (joinable.Length < maxGroups)
                return -1;
        
            short firstChoice = -1; // First choice is a group with the same zone name as the one requested.
            short secondChoice = -1; // Second choice is an empty zone.
        
            for (short i = 0; i < maxGroups; i++)
            {
                if (joinable[i] == zone)
                    firstChoice = i;
                if (joinable[i] == "" && secondChoice == -1)
                    secondChoice = i;
            }

            if (firstChoice != -1 && groups[GroupAndPlayer2Index(firstChoice, (short)(maxPlayersPerGroup - 1))] == -1)
                return firstChoice;
            if (secondChoice == -1)
                return -1;
        
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
        public void DisableJoinGroup(short group)
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
            for (var i = 0; i < groups.Length; i++)
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
}
