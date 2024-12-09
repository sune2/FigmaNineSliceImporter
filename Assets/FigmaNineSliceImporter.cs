using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class FigmaNineSliceImporter : ScriptableObject
{
    /// <summary>
    /// Figma APIのトークン
    /// </summary>
    public string token;

    /// <summary>
    /// Figmaのファイルキー
    /// </summary>
    public string fileKey;

    /// <summary>
    /// 取り込み対象のレイヤー名の正規表現
    /// </summary>
    public string targetRegex;

    /// <summary>
    /// 出力先のディレクトリ
    /// </summary>
    public DefaultAsset output;
}
