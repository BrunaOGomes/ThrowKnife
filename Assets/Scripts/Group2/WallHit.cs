using UnityEngine;
using Group3;

public class WallHit : MonoBehaviour
{
    // Quando algo bater na parede
    private void OnCollisionEnter(Collision collision)
    {
        // Verifica se o que bateu foi a faca (pela Tag)
        if (collision.gameObject.CompareTag("Faca"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                ThrowController controlador = FindObjectOfType<ThrowController>();
                if (controlador != null)
                    controlador.NotificarFacaCravada(collision.gameObject);

                // 1. Para a física da faca completamente
                rb.isKinematic = true;
                rb.detectCollisions = false; // Desativa colisões para não bugar

                // A faca fica parada no ponto de impacto sem mudar a hierarquia da cena.

                Debug.Log("Faca cravada na parede!");
            }
        }
    }
}
