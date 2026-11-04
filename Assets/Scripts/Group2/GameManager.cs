using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Group3;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace Group2
{
    public class GameManager : MonoBehaviour
    {
        [Header("Referências do Grupo 3")]
        public ThrowController throwController;

        [Header("Estoque de Facas")]
        public Vector3 estoqueCentro = new Vector3(3f, 2f, 0f);
        public Vector3 estoqueExtents = new Vector3(1.5f, 1.5f, 1f);

        [Header("Configurações do Jogo")]
        public int facasIniciais = 3;
        private int _facasRestantes;
        private int _pontuacao = 0;
        private int _baloesEstourados = 0;

        [Header("Balões")]
        public GameObject[] baloes;
        private int _totalBaloes;

        [Header("HUD (Interface)")]
        public TextMeshProUGUI textoFacas;
        public TextMeshProUGUI textoPontuacao;

        [Header("Tela Final")]
        public GameObject painelFimDeJogo; // O painel que estava desativado
        public TextMeshProUGUI textoResultadoFinal; // O texto que vai mostrar os balões

        private bool _jogoFinalizado;

        void Start()
        {
            // O estoque não pode oferecer mais facas do que as disponíveis no controlador.
            if (throwController != null && throwController.facasNaMesa != null)
                facasIniciais = Mathf.Min(facasIniciais, throwController.facasNaMesa.Length);

            _facasRestantes = facasIniciais;
            _totalBaloes = baloes != null ? baloes.Length : 0;

            GarantirInterface();

            if (painelFimDeJogo != null)
                painelFimDeJogo.SetActive(false);

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
            if (throwController != null)
            {
                throwController.OnFacaColidiu -= HandleFacaColidiu;
                throwController.OnFacaSaiuDaCena -= HandleFacaSaiuDaCena;
                throwController.OnLancamento -= HandleLancamento;
            }
        }

        public bool TentarPegarFaca(Vector3 maoMundo)
        {
            // 1. Tem faca no estoque?
            if (_facasRestantes <= 0)
            {
                Debug.Log("[GameManager] Sem facas restantes.");
                return false;
            }

            Vector3 localPos = maoMundo - estoqueCentro;
            if (Mathf.Abs(localPos.x) > estoqueExtents.x ||
                Mathf.Abs(localPos.y) > estoqueExtents.y ||
                Mathf.Abs(localPos.z) > estoqueExtents.z)
            {
                Debug.Log($"[GameManager] Mão fora do estoque: {maoMundo} | centro={estoqueCentro} extents={estoqueExtents}");
                return false; // Mão longe do estoque
            }

            _facasRestantes--;
            AtualizarHUD();
            return true;
        }

        private void HandleFacaColidiu(Transform balaoAtingido)
        {
            _baloesEstourados++;
            _pontuacao += 10;
            AtualizarHUD();

            // Desativa APENAS o balão que foi atingido
            if (balaoAtingido != null)
            {
                balaoAtingido.gameObject.SetActive(false);
                Debug.Log($"[GameManager] Balão atingido: {balaoAtingido.name}");
            }

            if (_baloesEstourados >= _totalBaloes || _facasRestantes <= 0)
                FinalizarJogo();
        }

        private void HandleFacaSaiuDaCena()
        {
            // Se não tem mais facas para jogar, o jogo acaba
            if (_facasRestantes <= 0 && throwController != null)
            {
                // Espera um pouco para a última faca terminar de voar antes de mostrar a tela
                Invoke(nameof(FinalizarJogo), 1.5f);
            }
        }

        private void HandleLancamento(LaunchData data)
        {
            Debug.Log($"[GameManager] Faca lançada com velocidade: {data.Velocidade[0]:F1}, {data.Velocidade[1]:F1}, {data.Velocidade[2]:F1}");
        }

        private void AtualizarHUD()
        {
            if (textoFacas != null)
                textoFacas.text = $"Facas: {_facasRestantes}";
            if (textoPontuacao != null)
                textoPontuacao.text = $"Pontos: {_pontuacao}";
        }

        private void FinalizarJogo()
        {
            if (_jogoFinalizado) return;
            _jogoFinalizado = true;

            if (painelFimDeJogo != null)
            {
                painelFimDeJogo.SetActive(true);

                if (textoResultadoFinal != null)
                {
                    textoResultadoFinal.text = $"FIM DE JOGO!\n\nBalões Estourados: {_baloesEstourados} de {_totalBaloes}\nPontuação Final: {_pontuacao}";
                }
            }
            if (throwController != null)
                throwController.enabled = false;
            Debug.Log("[GameManager] Jogo Finalizado!");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(estoqueCentro, estoqueExtents * 2);
        }

        public void ReiniciarJogo()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void GarantirInterface()
        {
            if (painelFimDeJogo != null) return;

            GameObject canvasObject = new GameObject("Canvas (HUD)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            textoFacas = CriarTexto(canvasObject.transform, "TextoFacas", new Vector2(20, -20), new Vector2(300, 60), TextAlignmentOptions.TopLeft, 30);
            textoPontuacao = CriarTexto(canvasObject.transform, "TextoPontuacao", new Vector2(-20, -20), new Vector2(300, 60), TextAlignmentOptions.TopRight, 30);
            textoFacas.rectTransform.anchorMin = new Vector2(0, 1);
            textoFacas.rectTransform.anchorMax = new Vector2(0, 1);
            textoFacas.rectTransform.pivot = new Vector2(0, 1);
            textoPontuacao.rectTransform.anchorMin = new Vector2(1, 1);
            textoPontuacao.rectTransform.anchorMax = new Vector2(1, 1);
            textoPontuacao.rectTransform.pivot = new Vector2(1, 1);

            painelFimDeJogo = new GameObject("PainelFimDeJogo", typeof(RectTransform), typeof(Image));
            painelFimDeJogo.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = painelFimDeJogo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700, 460);
            painelFimDeJogo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

            textoResultadoFinal = CriarTexto(painelFimDeJogo.transform, "ResultadoFinal", new Vector2(0, 55), new Vector2(640, 270), TextAlignmentOptions.Center, 34);
            textoResultadoFinal.rectTransform.anchorMin = textoResultadoFinal.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textoResultadoFinal.color = Color.white;

            GameObject buttonObject = new GameObject("BotaoReiniciar", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(painelFimDeJogo.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 35);
            buttonRect.sizeDelta = new Vector2(280, 70);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.55f, 0.25f, 1);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(ReiniciarJogo);

            TextMeshProUGUI buttonLabel = CriarTexto(buttonObject.transform, "TextoBotao", Vector2.zero, new Vector2(260, 60), TextAlignmentOptions.Center, 28);
            buttonLabel.text = "JOGAR NOVAMENTE";
            buttonLabel.color = Color.white;
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = buttonLabel.rectTransform.offsetMax = Vector2.zero;

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(null);
            }

            painelFimDeJogo.SetActive(false);
        }

        private static TextMeshProUGUI CriarTexto(Transform parent, string nome, Vector2 posicao, Vector2 tamanho, TextAlignmentOptions alinhamento, float fonte)
        {
            GameObject textoObject = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
            textoObject.transform.SetParent(parent, false);
            RectTransform rect = textoObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicao;
            rect.sizeDelta = tamanho;
            TextMeshProUGUI texto = textoObject.GetComponent<TextMeshProUGUI>();
            texto.alignment = alinhamento;
            texto.fontSize = fonte;
            texto.color = Color.white;
            texto.raycastTarget = false;
            return texto;
        }
    }
}
