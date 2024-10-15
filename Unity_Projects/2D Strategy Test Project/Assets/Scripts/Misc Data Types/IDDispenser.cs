using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is not an interface. It's a tiny little guy that dispenses int IDs in sequence, starting at 0.
/// </summary>
public class IDDispenser
{
    private int _nextID = 0;
    public int DispenseID() {  return _nextID++; }

    public IDDispenser() { }
    public IDDispenser(int startingValue) {  _nextID = startingValue; }
}
