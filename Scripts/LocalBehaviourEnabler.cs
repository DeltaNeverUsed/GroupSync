
using System;
using DeltasInteractions.Utils;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Component = UnityEngine.Component;

[RequireComponent(typeof(GroupObjectSync))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LocalBehaviourEnabler : UdonSharpBehaviour
{
    public UdonSharpBehaviour[] excluded = { };

    private UdonSharpBehaviour[] _compList = { };

    private void Start()
    {
        excluded = excluded.Add(GetComponent<GroupObjectSync>());
        excluded = excluded.Add(this);
        var objectComps = GetComponents<UdonSharpBehaviour>();
        foreach (var comp in objectComps)
        {
            if (excluded.Contains(comp))
                return;
            _compList = _compList.Add(comp);
        }
    }

    public void EnableComps()
    {
        foreach (var comp in _compList)
            comp.enabled = true;
    }
    public void DisableComps()
    {
        foreach (var comp in _compList)
            comp.enabled = false;
    }
}
