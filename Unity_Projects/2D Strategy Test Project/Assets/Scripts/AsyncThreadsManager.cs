using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
/// <summary>
/// This is kind of clunky and manual, but for now, any time you start a new thread,
/// you should declare it here, and any time you stop one, you should declare it stopped.
/// </summary>
public class AsyncThreadsManager 
{
    private static Dictionary<int, string> AsyncThreadsStatus = new Dictionary<int, string>();
    private static SemaphoreSlim AsyncThreadsStatusSemaphore = new SemaphoreSlim(1, 1);

    public static bool AreAsyncThreadsStillRunning()
    {
        AsyncThreadsStatusSemaphore.Wait();
        if (AsyncThreadsStatus.ContainsValue("Running"))
        { AsyncThreadsStatusSemaphore.Release(); return true; }
        else
        { AsyncThreadsStatusSemaphore.Release(); return false; }
    }
    public static void DeclareThreadRunning()
    {
        AsyncThreadsStatusSemaphore.Wait();
        AsyncThreadsStatus[System.Environment.CurrentManagedThreadId]= "Running";
        AsyncThreadsStatusSemaphore.Release();
    }
    public static void DeclareThreadStopped()
    {
        AsyncThreadsStatusSemaphore.Wait();
        AsyncThreadsStatus[System.Environment.CurrentManagedThreadId] = "Stopped";
        AsyncThreadsStatusSemaphore.Release();
    }
}

public class AsyncResourcesSemaphores
{
    public static Dictionary<int, SemaphoreSlim> taeUtilityCheckBoolDictSemaphores 
        = new Dictionary<int, SemaphoreSlim>();
}