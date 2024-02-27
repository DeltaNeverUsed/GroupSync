#define USPPNet_string
#define USPPNet_int

using Cyan.PlayerObjectPool;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class USPPNetEveryPlayerManager : CyanPlayerObjectPoolEventListener
    {
        public GroupManager groupManager;
        [HideInInspector] public USPPNetEveryPlayer local_object;
        public bool neverCloseGroups;

        public override void _OnLocalPlayerAssigned() {
            
        }

        public override void _OnPlayerAssigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject) {
            if (!player.isLocal)
                return;
            local_object = poolObject.GetComponent<USPPNetEveryPlayer>();
        }

        public override void _OnPlayerUnassigned(VRCPlayerApi player, int poolIndex, UdonBehaviour poolObject) {
            
        }
    }
}
