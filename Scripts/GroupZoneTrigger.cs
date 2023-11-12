
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupZoneTrigger : UdonSharpBehaviour
{
    public USPPNetEveryPlayerManager playerNetworkManager;
    public string zoneName = "ChangeMe";

    [Space]
    public bool exitZoneOnTriggerExit;

    private void Start()
    {
        if (playerNetworkManager != null) return;
        playerNetworkManager = GameObject.Find("EachPlayerUSPPNet").GetComponent<USPPNetEveryPlayerManager>();

        if (playerNetworkManager != null) return;
        Debug.LogError($"playerNetworkManager is not set on: {gameObject.name}");
        gameObject.SetActive(false);
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer)
            return;
        
        playerNetworkManager.local_object.request_new_group(zoneName);
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!exitZoneOnTriggerExit || player != Networking.LocalPlayer)
            return;
        
        playerNetworkManager.local_object.request_remove_from_group(Networking.LocalPlayer.playerId);
    }
}
