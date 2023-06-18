# GroupSync
Delta's Experimental Grouped Networking for Udon

# Installation
1. Download and install [USPPPatcher](https://github.com/DeltaNeverUsed/USPPPatcher) into your project.
2. Download and install [USPPNet](https://github.com/DeltaNeverUsed/USPPNet) into your project.
3. Clone this repository into your project somewhere

# Setup
1. Drag the "GroupNetworkingStuff" prefab into your scene somewhere
2. Now we want to setup some NetworkZones, you do that by creating an object with a collider set to trigger and adding the "GroupZoneTrigger" to it.
    - Then you'd want to set the zone name, and then you're done!
3. Now instead of using "VRCObjectSync" you'd add a "GroupObjectSync" Script!
4. After you've added any new GroupObjectSync's you'll need to either manually give them a unique network id, or go to the "GroupObjectSyncManager" under "GroupNetworkingStuff" and hit "Assign new IDs"

# Custom Scripts
To make custom stuff sync in groups you can inherit any script from "GroupCustomSync",
GroupCustomSync provides five functions to help sync stuff.
But before you use any of those, you need to run the StartNet() function, to initialize the networking

### Global
1. SetVariableInAllGroups(string name, object value, bool setLocally = true, bool autoSerialize = true)


2. CallFunctionInAllGroups(string name, bool callLocally = true, bool autoSerialize = true)


### Local group
3. SetVariableInLocalGroup(string name, object value, bool setLocally = true, bool autoSerialize = true)


4. CallFunctionInLocalGroup(string name, bool callLocally = true, bool autoSerialize = true)


5. SubLeaveGroupCallback()
   - To use this, you need a public function in your script called exactly "LeaveCallback"
   - This will get run on the local user whenever they change groups.


## TODO: Write better readme