using UnityEngine;
using System.Collections;

public class GlobalInputManager : MonoBehaviour
{
    public static GlobalInputManager Instance;

    [Header("Global Input Settings")]
    public float globalInputBlockDuration = 2f;

    private bool globalInputsBlocked = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool AreInputsBlocked()
    {
        return globalInputsBlocked;
    }

    public void BlockAllPlayerInputs()
    {
        StartCoroutine(BlockInputsCoroutine());
    }

    private IEnumerator BlockInputsCoroutine()
    {
        globalInputsBlocked = true;

        // Stop all tank movement immediately
        TankController[] allTanks = FindObjectsOfType<TankController>();
        foreach (TankController tank in allTanks)
        {
            tank.ForceStopMovement();
        }

        Debug.Log($"All player inputs blocked for {globalInputBlockDuration} seconds!");

        yield return new WaitForSeconds(globalInputBlockDuration);

        globalInputsBlocked = false;
        Debug.Log("All player inputs restored!");
    }
}
