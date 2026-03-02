using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// コマンドライン引数を一元管理・解析するクラス。
/// アプリケーション全体でこのクラスを通して引数にアクセスします。
/// </summary>
public class CommandLineParser : MonoBehaviour
{
    public static CommandLineParser Instance { get; private set; }

    [Header("Parsed Arguments")]
    public string Mode { get; private set; }
    public ushort Port { get; private set; }
    public string AssetUrl { get; private set; }
    public string ServerIp { get; private set; }
    public bool SummonAvatar { get; private set; }
    public string AvatarGlbPath { get; private set; }
    public bool EnableMicrobit { get; private set; }

    [Header("Default Values")]
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private string defaultServerIp = "127.0.0.1";
    [SerializeField] private bool defaultSummonAvatar = true;
    [SerializeField] private bool defaultEnableMicrobit = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ParseArguments();
    }

    private void ParseArguments()
    {
        // 初期値の設定
        Port = defaultPort;
        ServerIp = defaultServerIp;
        SummonAvatar = defaultSummonAvatar;
        EnableMicrobit = defaultEnableMicrobit;

        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (i + 1 >= args.Length) continue; // 最後の要素なら値がないのでスキップ

            switch (args[i])
            {
                case "-mode":
                    Mode = args[i + 1].ToUpper();
                    break;
                case "-port":
                    if (ushort.TryParse(args[i + 1], out ushort parsedPort))
                        Port = parsedPort;
                    break;
                case "-assetUrl":
                    AssetUrl = args[i + 1];
                    break;
                case "-serverIp":
                    ServerIp = args[i + 1];
                    break;
                case "-summonAvatar":
                    if (bool.TryParse(args[i + 1], out bool parsedSummon))
                        SummonAvatar = parsedSummon;
                    break;
                case "-avatarGlb":
                    AvatarGlbPath = args[i + 1];
                    break;
                case "-enableMicrobit":
                    if (bool.TryParse(args[i + 1], out bool parsedEnable))
                        EnableMicrobit = parsedEnable;
                    break;
            }
        }
        
        Debug.Log("[CommandLineParser] 解析完了: " +
                  $"Mode={Mode}, Port={Port}, AssetUrl={AssetUrl}, " +
                  $"ServerIp={ServerIp}, SummonAvatar={SummonAvatar}, " +
                  $"AvatarGlbPath={AvatarGlbPath}, EnableMicrobit={EnableMicrobit}");
    }
}
