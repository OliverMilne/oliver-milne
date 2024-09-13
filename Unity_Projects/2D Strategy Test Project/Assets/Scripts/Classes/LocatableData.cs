using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocatableData
{
    public int assignedTAEID;
    public int? unitInfoId = null; // this is used to populate the UnitInfo on load
    public bool isUnit = false;
    public bool isScenery = false;
    public bool isSelectable = false;
}
