using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionArea : MonoBehaviour
{
    public float lifetime = 0.3f; // How long the circle stays
    public int damage = 1;
    public int ownerPlayerNumber;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TankController tank = other.GetComponent<TankController>();
        if (tank != null && tank.playerNumber != ownerPlayerNumber)
        {
            tank.TakeDamage(); // This should handle 1 damage or spin
        }
    }
}