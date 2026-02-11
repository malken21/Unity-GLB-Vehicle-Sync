using System;
using System.Threading.Tasks;
using UnityEngine;
using GLTFast;
using Unity.Netcode;
using Unity.Collections;

public class EnvironmentLoader : NetworkBehaviour
{
    private readonly NetworkVariable<FixedString512Bytes> environmentUrlNetwork = new NetworkVariable<FixedString512Bytes>();
    private GameObject currentEnvironment;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        environmentUrlNetwork.OnValueChanged += OnEnvironmentUrlChanged;

        if (!environmentUrlNetwork.Value.IsEmpty)
        {
            _ = LoadEnvironment(environmentUrlNetwork.Value.ToString());
        }

        if (IsServer)
        {
            // Initial environment URL if needed, or wait for trigger
            // string defaultEnv = ConnectionManager.Instance.serverUrl + "/default_stage.glb";
            // SetEnvironmentUrlServerRpc(defaultEnv);
        }
    }

    private void OnEnvironmentUrlChanged(FixedString512Bytes previousValue, FixedString512Bytes newValue)
    {
        if (!newValue.IsEmpty)
        {
            _ = LoadEnvironment(newValue.ToString());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnvironmentUrlServerRpc(string url)
    {
        environmentUrlNetwork.Value = new FixedString512Bytes(url);
    }

    public async Task LoadEnvironment(string url)
    {
        Debug.Log($"[EnvironmentLoader] Loading from {url}...");
        var gltf = new GltfImport();
        var settings = new ImportSettings
        {
            generateMipMaps = true,
            anisotropicFilterLevel = 3,
            nodeNameMethod = ImportSettings.NameImportMethod.OriginalUnique
        };

        var success = await gltf.Load(url, settings);

        if (success)
        {
            if (currentEnvironment != null)
            {
                Destroy(currentEnvironment);
            }

            currentEnvironment = new GameObject("DynamicEnvironment");
            currentEnvironment.transform.SetParent(transform, false);

            await gltf.InstantiateMainSceneAsync(currentEnvironment.transform);
            
            // Re-bake or update NavMesh if necessary here
            Debug.Log("[EnvironmentLoader] Environment loaded successfully.");
        }
        else
        {
            Debug.LogError($"[EnvironmentLoader] Failed to load environment from {url}");
        }
    }
}
