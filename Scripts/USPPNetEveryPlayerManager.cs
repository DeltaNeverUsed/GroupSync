
#define USPPNet_string
#define USPPNet_int

using USPPNet;
using System;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class USPPNetEveryPlayerManager : UdonSharpBehaviour
{
    public GroupManager groupManager;
    
    [HideInInspector] public USPPNetEveryPlayer local_object;
    [HideInInspector] public USPPNetEveryPlayer[] Objects;

    private void Start()
    {
        Objects = new USPPNetEveryPlayer[transform.childCount];
        var i = 0;
        foreach (Transform child in transform)
        {
            Objects[i] = child.GetComponent<USPPNetEveryPlayer>();
            i++;
        }
        
    }

    private void USPPNET_SetPlayerObject(int target_player, int id)
    {
        if (Networking.LocalPlayer.playerId != target_player)
            return;
        local_object = Objects[id];
        local_object.owned = true;
        local_object.RequestSerialization();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (!Networking.IsMaster)
            return;

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
