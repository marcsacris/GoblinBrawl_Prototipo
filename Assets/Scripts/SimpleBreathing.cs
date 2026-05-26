using UnityEngine;

// Autor: Marc Sacristán
// Proyecto: Goblin Brawl: Crónicas del Pantano

public class SimpleBreathing : MonoBehaviour
{
    [Header("Ajustes de Respiración")]
    [Tooltip("La rapidez con la que el goblin respira.")]
    public float breathSpeed = 3f; 
    
    [Tooltip("Cuánto se estira el sprite al respirar.")]
    public float breathAmount = 0.05f; 

    private Vector3 originalScale;

    void Start()
    {
        // Guardamos la escala original para no deformar al sprite a lo largo del tiempo
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Calculamos la oscilación suave usando el tiempo y la velocidad
        float breathingEffect = Mathf.Sin(Time.time * breathSpeed) * breathAmount;

        // Se lo aplicamos solo al eje Y para que parezca que el pecho se hincha hacia arriba.
        // El eje X y Z se quedan con su valor original.
        transform.localScale = new Vector3(
            originalScale.x, 
            originalScale.y + breathingEffect, 
            originalScale.z
        );
    }
}