using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

[RequireComponent(typeof(VRCObjectSync))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class FakeObjectSync : UdonSharpBehaviour
{
    [UdonSynced] [HideInInspector] public bool used;
    [UdonSynced] [HideInInspector] public bool pickedUp;

    [HideInInspector] public int id;

    public void UnSync()
    {
        used = false;
        pickedUp = false;
        gameObject.SetActive(false);
    }
}
