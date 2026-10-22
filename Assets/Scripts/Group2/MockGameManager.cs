using System.Collections;
using UnityEngine;
using Group3;

namespace MockGroup2
{
    /// <summary>
    /// Simula o Grupo 2 para você testar sozinho.
    /// No dia da integração, troque por tentar o GameManager real.
    /// </summary>
    public class MockGameManager : MonoBehaviour
    {
        [Header("Config (mock)")]
        public int facasIniciais = 3;
        public Vector3 estoqueCentro = new Vector3(0f, 1f, 0f);
        public Vector3 estoqueExtents = new Vector3(2f, 2f, 1f);

        [Header("Eventos do ThrowController")]
        public ThrowController throwController;

        private int _facas;

        void Start()
        {
            _facas = facasIniciais;

            if (throwController != null)
            {
                throwController.OnFacaColidiu += HandleColidiu;
                throwController.OnFacaSaiuDaCena += HandleSaiuDaCena;
                throwController.OnLancamento += HandleLancamento;
            }
        }

        void OnDestroy()
        {
            if (throwController != null)
            {
                throwController.OnFacaColidiu -= HandleColidiu;
                throwController.OnFacaSaiuDaCena -= HandleSaiuDaCena;
                throwController.OnLancamento -= HandleLancamento;
            }
        }

        // ---- Contrato Grupo 2 -> Grupo 3 ----
        public bool TentarPegarFaca(Vector3 maoMundo)
        {
            if (_facas <= 0)
            {
                Debug.Log("[MOCK] Sem facas restantes.");
                return false;
            }

            Vector3 local = maoMundo - estoqueCentro;
            if (Mathf.Abs(local.x) > estoqueExtents.x ||
                Mathf.Abs(local.y) > estoqueExtents.y ||
                Mathf.Abs(local.z) > estoqueExtents.z)
            {
                Debug.Log($"[MOCK] Mão fora do estoque ({maoMundo}).");
                return false;
            }

            _facas--;
            Debug.Log($"[MOCK] Faca pega. Restam {_facas}/{facasIniciais}.");
            return true;
        }

        // ---- Contratos Grupo 3 -> Grupo 2 ----
        private void HandleColidiu()
        {
            Debug.Log("[MOCK] OnFacaColidiu -> balão estourado! +1 ponto.");
        }

        private void HandleSaiuDaCena()
        {
            Debug.Log("[MOCK] OnFacaSaiuDaCena -> errou.");
        }

        private void HandleLancamento(LaunchData data)
        {
            Debug.Log($"[MOCK] OnLancamento recebido:\n{data.ToJson()}");
            StartCoroutine(ResetFacasSeAcabou());
        }

        private IEnumerator ResetFacasSeAcabou()
        {
            yield return new WaitForSeconds(1f);
            if (_facas <= 0)
            {
                _facas = facasIniciais;
                Debug.Log("[MOCK] Facas resetadas.");
            }
        }
    }
}