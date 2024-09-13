using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These classes are used to give things stats to & from which modifiers can easily and cleanly 
/// be applied & removed.
/// </summary>
public interface IBuff { }
public interface IBuffedVariable
{
    public void TickDownBuffs();
}
// Root Class
public abstract class BuffedVariable<T> : IBuffedVariable
{
    public T baseValue;
    public float minimumMultiplier = 0;

    public Dictionary<string, AdditiveBuff<T>> additiveBuffs = new Dictionary<string, AdditiveBuff<T>>();
    public Dictionary<string, PercentagePointBuff> percentageBuffs 
        = new Dictionary<string, PercentagePointBuff>();

    protected float multiplier 
    { 
        get 
        {
            float ppsSummed = 0;
            foreach (PercentagePointBuff ppb in percentageBuffs.Values) ppsSummed += ppb.percentagePointsValue;
            return Mathf.Clamp(1 + (0.01f * ppsSummed), minimumMultiplier, float.MaxValue);
        } 
    }

    public void ApplyBuff(string buffName, AdditiveBuff<T> buffDetails)
    {
        if (additiveBuffs.ContainsKey(buffName)) additiveBuffs.Remove(buffName);
        additiveBuffs.Add(buffName, buffDetails);
    }
    public void ApplyBuff(string buffName, PercentagePointBuff buffDetails)
    {
        if (percentageBuffs.ContainsKey(buffName)) percentageBuffs.Remove(buffName);
        percentageBuffs.Add(buffName, buffDetails);
    }
    /// <summary>
    /// This should be used to remove buffs - means you don't have to worry about which dictionary you're
    /// removing it from.
    /// </summary>
    /// <param name="buffName"></param>
    public void RemoveBuff(string buffName)
    {
        additiveBuffs.Remove(buffName);
        percentageBuffs.Remove(buffName);
    }
    /// <summary>
    /// method to tick down buff durations and clear them out if they've hit zero,
    /// to be called at the start of every turn
    /// </summary>
    public void TickDownBuffs()
    {
        List<string> keysToRemove = new List<string>();
        foreach (var entry in additiveBuffs)
        {
            if (entry.Value.turnsRemaining == null) continue;
            entry.Value.turnsRemaining -= 1;
            if (entry.Value.turnsRemaining <= 0 ) keysToRemove.Add(entry.Key);
        }
        foreach (string s in keysToRemove) additiveBuffs.Remove(s);
        keysToRemove.Clear();
        foreach (var entry in percentageBuffs)
        {
            if (entry.Value.turnsRemaining == null) continue;
            entry.Value.turnsRemaining -= 1;
            if (entry.Value.turnsRemaining <= 0) keysToRemove.Add(entry.Key);
        }
        foreach (string s in keysToRemove) percentageBuffs.Remove(s);
    }
}
// Buffable Data Types
public class BuffedFloat : BuffedVariable<float>
{
    public float value
    {
        get
        {
            float addedBonus = 0;
            foreach (var ab in additiveBuffs.Values) addedBonus += ab.value;

            return (baseValue * multiplier) + addedBonus;
        }
    }
}
public class BuffedInt : BuffedVariable<int>
{
    public int value 
    {
        get
        {
            float addedBonus = 0;
            foreach (var ab in additiveBuffs.Values) addedBonus += ab.value;

            return (int)Mathf.Ceil(((float)baseValue * multiplier) + addedBonus);
        }
    }
}
// Buff Classes
public class AdditiveBuff<T> : IBuff
{
    public string name { get; set; }
    public string description { get; set; }
    public int? turnsRemaining { get; set; }
    public T value { get; set; }
}
public class PercentagePointBuff : IBuff
{
    public string name { get; }
    public string description { get; }
    public int? turnsRemaining { get; set; }
    /// <summary>
    /// These are all added together, divided by 100, and added to 1 to give the total multiplier
    /// for the buffed value.
    /// </summary>
    public float percentagePointsValue;
}
