using UnityEngine;
using Unity.Netcode;

/// <summary>
/// アバターのボール（ExerciseBall）の色を変更・同期するクラス。
/// </summary>
public class AvatarColorController : NetworkBehaviour
{
    [Header("コンポーネント設定")]
    [SerializeField]
    [Tooltip("色を変更する対象のRenderer（ExerciseBall）。未指定の場合は子オブジェクトから自動検索します。")]
    private Renderer targetBallRenderer;

    // ボールの色（Hue）を同期するためのNetworkVariable (0.0 - 1.0)
    private NetworkVariable<float> ballHue = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // シェーダープロパティのID
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int HuePropertyId = Shader.PropertyToID("_Hue");
    /// <summary>
    /// 対象となる ExerciseBall の Renderer を検索します。
    /// </summary>
    private void FindTargetRenderer()
    {
        // 階層を問わず子オブジェクトから Renderer を検索
        targetBallRenderer = GetComponentInChildren<Renderer>();
        
        if (targetBallRenderer != null)
        {
            Debug.Log($"[AvatarColorController] 対象の Renderer を検出しました: {targetBallRenderer.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[AvatarColorController] 対象の Renderer が見つかりませんでした。");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ballHue.OnValueChanged += OnBallHueChanged;
        
        // Rendererが未設定の場合は再検索
        if (targetBallRenderer == null)
        {
            FindTargetRenderer();
        }
        
        ApplyHueToRenderer(ballHue.Value);
    }

    public override void OnNetworkDespawn()
    {
        ballHue.OnValueChanged -= OnBallHueChanged;
        base.OnNetworkDespawn();
    }

    private void OnBallHueChanged(float previousValue, float newValue)
    {
        ApplyHueToRenderer(newValue);
    }

    private void ApplyHueToRenderer(float hue)
    {
        if (targetBallRenderer != null)
        {
            Color newColor = Color.HSVToRGB(hue, 1f, 1f);

            // URP Litなどの _BaseColor を更新
            if (targetBallRenderer.material.HasProperty(BaseColorPropertyId))
            {
                targetBallRenderer.material.SetColor(BaseColorPropertyId, newColor);
            }
            // Built-in Standardなどの _Color を更新
            else if (targetBallRenderer.material.HasProperty(ColorPropertyId))
            {
                targetBallRenderer.material.SetColor(ColorPropertyId, newColor);
            }
            
            // カスタムシェーダー用の _Hue プロパティも念のため更新
            if (targetBallRenderer.material.HasProperty(HuePropertyId))
            {
                targetBallRenderer.material.SetFloat(HuePropertyId, hue);
            }
        }
    }

    /// <summary>
    /// 色相（Hue）を設定します。Ownerのみ実行可能です。
    /// </summary>
    /// <param name="hue">0.0～1.0のHue値</param>
    public void SetHue(float hue)
    {
        if (!IsOwner) return;
        ballHue.Value = hue;
    }
}
