
#define USPPNet_string
#define USPPNet_int

using USPPNet;
using System;

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class USPPNetEveryPlayerManager : UdonSharpBehaviour
    {
        public GroupManager groupManager;
    
        [HideInInspector] public USPPNetEveryPlayer local_object;
        [HideInInspector] public USPPNetEveryPlayer[] Objects;

        public void Bootstrap()
        {
            Objects = new USPPNetEveryPlayer[transform.childCount];
            var i = 0;
            foreach (Transform child in transform)
            {
                Objects[i] = child.GetComponent<USPPNetEveryPlayer>();
                i++;
            }
            
            SendCustomEventDelayedSeconds(nameof(ErrorLocalObject), 10);
        }

        public void ErrorLocalObject()
        {
            if (Utilities.IsValid(local_object))
                return;
            
            Debug.LogError("This is really bad! report this to me, please! send me your log! discord: deltaneverused");
        }

        private void USPPNET_SetPlayerObject(int target_player, int id)
        {
            if (Networking.LocalPlayer.playerId != target_player)
                return;
            
            local_object = Objects[id];
            Networking.SetOwner(Networking.LocalPlayer, local_object.gameObject);
            
            local_object.owned = true;
            local_object.RequestSerialization();
        }

        public DataList playersToAssign = new DataList();

        public void AssignPlayerObject(VRCPlayerApi player)
        {
            for (int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].owned)
                    continue;
            
                Objects[i].owned = true;
                Networking.SetOwner(player, Objects[i].gameObject);
                if (player == Networking.LocalPlayer) // if joined player is instance master don't network
                    local_object = Objects[i];
                else
                {
                    USPPNET_SetPlayerObject(player.playerId, i);
                    RequestSerialization();
                }

                break;
            }
        }

        public void CheckAssignWorked()
        {
            var index = playersToAssign.Count - 1;
            var hasToken = playersToAssign.TryGetValue(index, TokenType.Int, out var valueToken);
            playersToAssign.RemoveAt(index);
            
            if (!hasToken)
                return;

            var playerId = valueToken.Int;
            var player = VRCPlayerApi.GetPlayerById(playerId);
            
            foreach (var lObject in Objects)
            {
                if (!lObject.owned)
                    continue;
                if (Networking.IsOwner(player, lObject.gameObject))
                {
                    Debug.Log( $"local_object got assigned correctly for player: ({player}, {playerId})!");
                    return;
                }
            }
            
            Debug.LogWarning($"Couldn't assign player local_object once, trying again! for: ({player}, {playerId})");
            AssignPlayerObject(player);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsMaster || !Utilities.IsValid(player))
                return;
            
            playersToAssign.Add(player.playerId);
            AssignPlayerObject(player);
            
            SendCustomEventDelayedSeconds(nameof(CheckAssignWorked), 5);
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
