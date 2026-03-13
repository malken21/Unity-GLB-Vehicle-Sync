using UnityEngine;
using Unity.Netcode;

public class AvatarColorController : NetworkBehaviour
{
    [SerializeField]
    private Renderer targetBallRenderer = default;

    private NetworkVariable<float> ballHue = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int HuePropertyId = Shader.PropertyToID("_Hue");

    private void FindTargetRenderer()
    {
        targetBallRenderer = GetComponentInChildren<Renderer>();
        
        if (targetBallRenderer != null)
        {
            Debug.Log($"[AvatarColorController] Target renderer detected: {targetBallRenderer.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[AvatarColorController] Target renderer not found.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ballHue.OnValueChanged += OnBallHueChanged;
        
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

            if (targetBallRenderer.material.HasProperty(BaseColorPropertyId))
            {
                targetBallRenderer.material.SetColor(BaseColorPropertyId, newColor);
            }
            else if (targetBallRenderer.material.HasProperty(ColorPropertyId))
            {
                targetBallRenderer.material.SetColor(ColorPropertyId, newColor);
            }
            
            if (targetBallRenderer.material.HasProperty(HuePropertyId))
            {
                targetBallRenderer.material.SetFloat(HuePropertyId, hue);
            }
        }
    }

    public void SetHue(float hue)
    {
        if (!IsOwner) return;
        ballHue.Value = hue;
    }
}
