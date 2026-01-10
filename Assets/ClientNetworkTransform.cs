using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent] // 同じオブジェクトに複数つけられないようにする
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// ここで false を返すことで、サーバー権限ではなくクライアント権限（Owner権限）になります。
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
