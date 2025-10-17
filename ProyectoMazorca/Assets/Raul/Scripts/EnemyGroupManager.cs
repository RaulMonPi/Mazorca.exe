using UnityEngine;
using System.Collections.Generic;

public class EnemyGroupManager : MonoBehaviour
{
    public List<MovmientoNPC> enemies = new List<MovmientoNPC>();

    public void AlertGroup(Vector3 playerPosition, MovmientoNPC caller)
    {
        foreach (var npc in enemies)
        {
            if (npc != null && npc != caller)
            {
                npc.ReceiveAlert(playerPosition); // Activa heardSound y le pasa la ubicación
            }
        }
    }
}