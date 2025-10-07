using UnityEngine;
using System.Collections;

public class PlayerVida : MonoBehaviour
{
    public int vida = 3;
    public float tiempoInvulnerable = 1.0f; // segundos de invulnerabilidad tras recibir daño
    public float duracionHitStop = 0.5f;

    private float tiempoUltimoDanio = -Mathf.Infinity;
    public Animator animator;
    private bool isHitStop = false;

    public void RecibirDanio(int cantidad)
    {
        if (Time.time - tiempoUltimoDanio < tiempoInvulnerable)
            return; // Aún es invulnerable

        vida -= cantidad;
        tiempoUltimoDanio = Time.time;
        Debug.Log("Jugador ha recibido daño. Vida restante: " + vida);

        if (vida <= 0)
        {
            // Lógica de muerte
        }

        animator.SetTrigger("Hit");

        StartCoroutine(HitStopCoroutine());

    }

    private IEnumerator HitStopCoroutine()
    {


        isHitStop = true;
        float originalTimeScale = Time.timeScale;

        Time.timeScale = 0f;

        //Esperamos en tiempo real
        yield return new WaitForSecondsRealtime(duracionHitStop);

        Time.timeScale = originalTimeScale;
        isHitStop = false;
    }
}