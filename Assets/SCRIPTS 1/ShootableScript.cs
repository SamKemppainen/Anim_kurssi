using UnityEngine;

public class ShootableProp : MonoBehaviour, IShootable
{
    public Collider TakeHit()
    {
        return GetComponent<Collider>();
    }
}