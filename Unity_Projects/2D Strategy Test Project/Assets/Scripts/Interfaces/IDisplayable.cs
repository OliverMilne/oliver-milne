using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void DisplayableInfoUpdated();
public interface IDisplayable
{
    public event DisplayableInfoUpdated OnDisplayableInfoUpdated;
}
