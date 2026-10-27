using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Para funcionar com o HUD (Text ou TextMeshPro)
using TMPro;          // Se estiver usando TextMeshPro, senão remova esta linha
using Group3;         // Para acessar o ThrowController

namespace Group2
{
    public class GameManager : MonoBehaviour
    {
        [Header("Referências do Grupo 3")]
        [Tooltip("Arraste o GameObject que tem o ThrowController aqui.")]
        public ThrowController throwController;

        [Header("Estoque de Facas")]
        [Tooltip("Centro do estoque (onde a mão precisa fechar para pegar a faca).")]
        public Vector3 estoqueCentro = new Vector3(3f, 2f, 0f);
        [Tooltip("Tamanho da área de pega (X, Y, Z).")]
        public Vector3 estoqueExtents = new Vector3(1.5f, 1.5f, 1f);

        [Header("Configurações do Jogo")]
        public int facasIniciais = 3;
        private int _facasRestantes;
        private int _pontuacao = 0;
        private int _baloesEstourados = 0;

        [Header("Balões")]
        [Tooltip("Arraste os balões da cena aqui.")]
        public GameObject[] baloes;
        private int _totalBaloes;

        [Header("HUD (Interface)")]
        public TextMeshProUGUI textoFacas;
        public TextMeshProUGUI textoPontuacao;
        public GameObject painelFimDeJogo; // Um painel que aparece no final

        void Start()
        {
            Debug.Log("GameManager iniciou com sucesso!");
            _facasRestantes = facasIniciais;
            _totalBaloes = baloes.Length;
            if (painelFimDeJogo != null) painelFimDeJogo.SetActive(false);

            // --- Conectando aos eventos do Grupo 3 ---
            if (throwController != null)
            {
                throwController.OnFacaColidiu += HandleFacaColidiu;
                throwController.OnFacaSaiuDaCena += HandleFacaSaiuDaCena;
                throwController.OnLancamento += HandleLancamento;
            }
            else
            {
                Debug.LogError("[GameManager] ThrowController não atribuído no Inspector!");
            }

            AtualizarHUD();
        }

        void OnDestroy()
        {
            // Limpar eventos para não dar erro ao fechar o jogo
            if (throwController != null)
            {
                throwController.OnFacaColidiu -= HandleFacaColidiu;
                throwController.OnFacaSaiuDaCena -= HandleFacaSaiuDaCena;
                throwController.OnLancamento -= HandleLancamento;
            }
        }

        // ==========================================
        // CONTRATO: GRUPO 2 -> GRUPO 3
        // Chamado pelo ThrowController quando a mão fecha
        // ==========================================
        public bool TentarPegarFaca(Vector3 maoMundo)
        {
            // 1. Tem faca no estoque?
            if (_facasRestantes <= 0)
            {
                Debug.Log("[GameManager] Sem facas restantes.");
                return false;
            }

            // 2. A mão está dentro da área do estoque?
            Vector3 localPos = maoMundo - estoqueCentro;
            if (Mathf.Abs(localPos.x) > estoqueExtents.x ||
                Mathf.Abs(localPos.y) > estoqueExtents.y ||
                Mathf.Abs(localPos.z) > estoqueExtents.z)
            {
                Debug.Log($"[GameManager] Mão fora do estoque: {maoMundo} | centro={estoqueCentro} extents={estoqueExtents}");
                return false; // Mão longe do estoque
            }

            // 3. Autoriza o Grupo 3 a prender a faca
            _facasRestantes--;
            AtualizarHUD();
            return true;
        }

        // ==========================================
        // CONTRATOS: GRUPO 3 -> GRUPO 2
        // ==========================================
        private void HandleFacaColidiu()
        {
            // A faca cruzou o plano Z e acertou um balão
            _baloesEstourados++;
            _pontuacao += 10; // 10 pontos por balão
            AtualizarHUD();

            // Desativa um balão (o Grupo 3 já destruiu a faca)
            foreach (var b in baloes)
            {
                if (b != null && b.activeSelf)
                {
                    b.SetActive(false); // "Estoura" o balão
                    break;
                }
            }

            VerificarFimDeJogo();
        }

        private void HandleFacaSaiuDaCena()
        {
            // A faca errou ou caiu. Só checa se o jogo acabou.
            VerificarFimDeJogo();
        }

        private void HandleLancamento(LaunchData data)
        {
            Debug.Log($"[GameManager] Faca lançada com velocidade: {data.Velocidade[0]:F1}, {data.Velocidade[1]:F1}, {data.Velocidade[2]:F1}");
        }

        // ==========================================
        // LÓGICA GERAL
        // ==========================================
        private void AtualizarHUD()
        {
            if (textoFacas != null)
                textoFacas.text = $"Facas: {_facasRestantes}";

            if (textoPontuacao != null)
                textoPontuacao.text = $"Pontos: {_pontuacao}";
        }

        private void VerificarFimDeJogo()
        {
            // O jogo acaba se:
            // 1. Todos os balões foram estourados OU
            // 2. Acabaram as facas e não tem nenhuma voando no momento
            bool semBaloes = (_baloesEstourados >= _totalBaloes);
            bool semFacas = (_facasRestantes <= 0);

            if (semBaloes || semFacas)
            {
                // Espera um segundinho antes de mostrar o painel para a faca terminar o trajeto
                Invoke(nameof(MostrarFimDeJogo), 2f);
            }
        }

        private void MostrarFimDeJogo()
        {
            if (painelFimDeJogo != null)
            {
                painelFimDeJogo.SetActive(true);
                TextMeshProUGUI txt = painelFimDeJogo.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = $"FIM DE JOGO!\nAcertos: {_baloesEstourados}/{_totalBaloes}";
            }
            Debug.Log("[GameManager] FIM DE JOGO!");
        }
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(estoqueCentro, estoqueExtents * 2);
        }
    }
}