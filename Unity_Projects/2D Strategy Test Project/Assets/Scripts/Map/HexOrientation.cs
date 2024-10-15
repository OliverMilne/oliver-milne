using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains methods for working with HexDirection enums.
/// </summary>
public class HexOrientation
{
    public static HexDir Anticlockwise1(HexDir direction) 
    {
        switch (direction)
        {
            case HexDir.W: return HexDir.SW;
            case HexDir.NW: return HexDir.W;
            case HexDir.NE: return HexDir.NW;
            case HexDir.E: return HexDir.NE;
            case HexDir.SE: return HexDir.E;
            case HexDir.SW: return HexDir.SE;
            default: throw new System.Exception("Passed invalid value to HexOrientation.Antilockwise1!");
        }
    }
    public static HexDir Anticlockwise2(HexDir direction) 
    {
        switch (direction)
        {
            case HexDir.W: return HexDir.SE;
            case HexDir.NW: return HexDir.SW;
            case HexDir.NE: return HexDir.W;
            case HexDir.E: return HexDir.NW;
            case HexDir.SE: return HexDir.NE;
            case HexDir.SW: return HexDir.E;
            default: throw new System.Exception("Passed invalid value to HexOrientation.Anticlockwise2!");
        }
    }
    public static HexDir Clockwise1(HexDir direction)
    {
        switch (direction)
        {
            case HexDir.W: return HexDir.NW;
            case HexDir.NW: return HexDir.NE;
            case HexDir.NE: return HexDir.E;
            case HexDir.E: return HexDir.SE;
            case HexDir.SE: return HexDir.SW;
            case HexDir.SW: return HexDir.W;
            default: throw new System.Exception("Passed invalid value to HexOrientation.Clockwise!");
        }
    }
    public static HexDir Clockwise2(HexDir direction) 
    {
        switch (direction)
        {
            case HexDir.W: return HexDir.NE;
            case HexDir.NW: return HexDir.E;
            case HexDir.NE: return HexDir.SE;
            case HexDir.E: return HexDir.SW;
            case HexDir.SE: return HexDir.W;
            case HexDir.SW: return HexDir.NW;
            default: throw new System.Exception("Passed invalid value to HexOrientation.Clockwise2!");
        }
    }
    public static HexDir HexDirPointAToPointB(Vector3 pointA, Vector3 pointB) 
    { 
        Vector3 directionVector = pointA - pointB;
        float angle = Vector3.SignedAngle(Vector3.up, directionVector, Vector3.back);
        if (0 < angle && angle <= 60) return HexDir.SW;
        if (60 < angle && angle <= 120) return HexDir.W;
        if (120 < angle && angle <= 180) return HexDir.NW;
        if (-180 < angle && angle <= -120) return HexDir.NE;
        if (-120 < angle && angle <= -60) return HexDir.E;
        if (-60 < angle && angle <= 0) return HexDir.SE;
        throw new System.Exception("Invalid angle: " + angle +"!");
    }
    public static Quaternion HexDirToRotation(HexDir direction)
    {
        Quaternion rot;
        switch (direction)
        {
            case HexDir.W: rot = Quaternion.identity; break;
            case HexDir.NW: rot = Quaternion.Euler(0, 0, 300); break;
            case HexDir.NE: rot = Quaternion.Euler(0, 0, 240); break;
            case HexDir.E: rot = Quaternion.Euler(0, 0, 180); break;
            case HexDir.SE: rot = Quaternion.Euler(0, 0, 120); break;
            case HexDir.SW: rot = Quaternion.Euler(0, 0, 60); break;
            default: throw new System.Exception("Passed invalid value to HexOrientation.HexDirToRotation!");
        }
        return rot;
    }
    public static HexDir Opposite(HexDir direction)
    {
        switch (direction) 
        {
            case HexDir.W: return HexDir.E;
            case HexDir.NW: return HexDir.SE;
            case HexDir.NE: return HexDir.SW;
            case HexDir.E: return HexDir.W;
            case HexDir.SE: return HexDir.NW;
            case HexDir.SW: return HexDir.NE;
            default: throw new System.Exception("Passed invalid value to HexOrientation.Opposite!");
        }
    }
    public static HexDir RandomDir()
    {
        int selector = Random.Range(0, 6);
        switch (selector) 
        {
            case 0: return HexDir.W;
            case 1: return HexDir.NW;
            case 2: return HexDir.NE;
            case 3: return HexDir.E;
            case 4: return HexDir.SE;
            case 5: return HexDir.SW;
            default: throw new System.Exception();
        }
    }
}
