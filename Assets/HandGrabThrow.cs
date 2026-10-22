using UnityEngine;

/// <summary>
/// Segura um cubo enquanto a mão está fechada e o lança quando a mão abre.
/// Não calcula velocidade nem mira — só aplica um impulso fixo na direção
/// "para frente" (eixo +Z do mundo, ou de um transform de referência).
/// </summary>
public class HandGrabThrow : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Script HandTracking que expõe MaoFechada e os handPoints.")]
    public HandTracking handTracking;

    [Tooltip("O cubo que será segurado/lançado. Deve ter Rigidbody.")]
    public Rigidbody cubo;

    [Tooltip("Ponto da palma: normalmente o handPoints[0] (pulso) ou [9] (base do médio).")]
    public Transform palmAnchor;

    [Header("Segurar")]
    [Tooltip("Deslocamento local aplicado ao cubo quando preso à palma.")]
    public Vector3 holdOffset = Vector3.zero;

    [Header("Lançamento")]
    [Tooltip("Impulso aplicado no momento de soltar, no espaço do mundo.")]
    public Vector3 throwImpulse = new Vector3(0f, 1.5f, 6f);

    [Tooltip("Se atribuído, o impulso é aplicado na direção 'forward' desse transform.\n" +
             "Se nulo, usa o eixo +Z do mundo.")]
    public Transform throwDirectionReference;

    [Header("Física do cubo quando solto")]
    public bool useGravityOnThrow = true;

    // ---- Estado interno ----
    private enum State { Solto, Segurando, Lancado }
    private State _state = State.Solto;
    private bool _prevMaoFechada = false;
    private Transform _cuboOriginalParent;
    private RigidbodyInterpolation _interpolationOriginal;
    private CollisionDetectionMode _collisionOriginal;

    void Start()
    {
        if (cubo == null)
        {
            Debug.LogError("[HandGrabThrow] 'cubo' não atribuído.", this);
            enabled = false;
            return;
        }
        if (handTracking == null)
        {
            Debug.LogError("[HandGrabThrow] 'handTracking' não atribuído.", this);
            enabled = false;
            return;
        }
        if (palmAnchor == null)
        {
            Debug.LogError("[HandGrabThrow] 'palmAnchor' não atribuído.", this);
            enabled = false;
            return;
        }

        _cuboOriginalParent = cubo.transform.parent;
        _interpolationOriginal = cubo.interpolation;
        _collisionOriginal = cubo.collisionDetectionMode;

        // Estado inicial: solto, com física normal
        SetCuboKinematic(false);
    }

    void Update()
    {
        bool maoFechada = handTracking.MaoFechada;

        // Transição aberta -> fechada : PEGA
        if (!_prevMaoFechada && maoFechada && _state == State.Solto)
        {
            Grab();
        }
        // Transição fechada -> aberta : SOLTA / LANÇA
        else if (_prevMaoFechada && !maoFechada && _state == State.Segurando)
        {
            Release();
        }

        _prevMaoFechada = maoFechada;
    }

    void LateUpdate()
    {
        // Mantém o cubo preso à palma (roda depois do HandTracking)
        if (_state == State.Segurando && cubo != null)
        {
            cubo.position = palmAnchor.TransformPoint(holdOffset);
            cubo.rotation = palmAnchor.rotation;
        }

        // Volta ao estado "Solto" um frame depois do lançamento
        if (_state == State.Lancado)
            _state = State.Solto;
    }

    // ------------------------------------------------------------------

    private void Grab()
    {
        _state = State.Segurando;

        // Para a física para não "brigar" com a posição da palma
        SetCuboKinematic(true);
        cubo.linearVelocity = Vector3.zero;   // Unity 6
        cubo.angularVelocity = Vector3.zero;
        cubo.useGravity = false;

        // Posiciona imediatamente na palma
        cubo.position = palmAnchor.TransformPoint(holdOffset);
        cubo.rotation = palmAnchor.rotation;

        // Opcional: parentear para organizar a hierarquia
        cubo.transform.SetParent(palmAnchor, worldPositionStays: true);

        Debug.Log($"[HandGrabThrow] Pegou. Parent='{palmAnchor.name}'");
    }

    private void Release()
    {
        _state = State.Lancado;

        // Desparenteia mantendo a posição atual
        cubo.transform.SetParent(_cuboOriginalParent, worldPositionStays: true);

        // Devolve a física
        SetCuboKinematic(false);
        cubo.useGravity = useGravityOnThrow;
        cubo.linearVelocity = Vector3.zero;
        cubo.angularVelocity = Vector3.zero;

        // Direção do impulso
        Vector3 dir = throwDirectionReference != null
            ? throwDirectionReference.forward
            : Vector3.forward;

        // Impulso = magnitude do throwImpulse * direção + componente Y configurável
        // Para simplificar: o usuário define throwImpulse já no eixo do mundo.
        // Se throwDirectionReference estiver setado, aplicamos na direção dele.
        Vector3 impulse = throwDirectionReference != null
            ? dir * throwImpulse.magnitude
            : throwImpulse;

        cubo.AddForce(impulse, ForceMode.Impulse);

        Debug.Log($"[HandGrabThrow] Lançou. Impulso={impulse}");
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Liga/desliga kinematic de forma segura (evita erro ao trocar
    /// collisionDetectionMode com o Rigidbody já em movimento).
    /// </summary>
    private void SetCuboKinematic(bool kinematic)
    {
        if (kinematic)
        {
            // Antes de ficar kinematic, usa um modo de colisão compatível
            cubo.collisionDetectionMode = CollisionDetectionMode.Discrete;
            cubo.interpolation = RigidbodyInterpolation.None;
            cubo.isKinematic = true;
        }
        else
        {
            cubo.isKinematic = false;
            cubo.interpolation = _interpolationOriginal;
            cubo.collisionDetectionMode = _collisionOriginal;
        }
    }
}
