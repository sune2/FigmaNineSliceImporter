using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[CustomEditor(typeof(FigmaNineSliceImporter))]
public class FigmaNineSliceImporterEditor : Editor
{
    private FigmaNineSliceImporter _importer;
    private Regex _targetNameRegex;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Import"))
        {
            _importer = (FigmaNineSliceImporter)target;
            _targetNameRegex = new Regex(_importer.targetRegex);
            Import();
        }
    }

    private void Import()
    {
        var url = $"https://api.figma.com/v1/files/{_importer.fileKey}";
        var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-FIGMA-TOKEN", _importer.token);
        request.SendWebRequest().completed += _ =>
        {
            if (request.error != null)
            {
                Debug.LogError(request.error);
                Debug.LogError(request.downloadHandler.text);
                return;
            }

            var response = JsonConvert.DeserializeObject<FileResponse>(request.downloadHandler.text);
            var targetInfoList = new List<TargetInfo>();
            BuildTargetInfoList(response.document, targetInfoList);

            if (targetInfoList.Count == 0)
            {
                Debug.Log("対象が見つかりませんでした");
                return;
            }

            ImportImage(targetInfoList);
        };
    }

    private void ImportImage(List<TargetInfo> targetInfoList)
    {
        var idListString = string.Join(",", targetInfoList.Select(targetInfo => targetInfo.id));
        var url = $"https://api.figma.com/v1/images/{_importer.fileKey}?ids={idListString}&format=png";
        var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-FIGMA-TOKEN", _importer.token);
        request.SendWebRequest().completed += _ =>
        {
            if (request.error != null)
            {
                Debug.LogError(request.error);
                Debug.LogError(request.downloadHandler.text);
                return;
            }

            var response = JsonConvert.DeserializeObject<ImageResponse>(request.downloadHandler.text);

            foreach (var targetInfo in targetInfoList)
            {
                var imageUrl = response.images[targetInfo.id];
                DownloadImage(imageUrl, targetInfo);
            }
        };
    }

    /// <summary>
    /// 画像のダウンロードとインポート設定の適用
    /// </summary>
    private void DownloadImage(string url, TargetInfo targetInfo)
    {
        var request = UnityWebRequest.Get(url);
        request.SendWebRequest().completed += _ =>
        {
            if (request.error != null)
            {
                Debug.LogError(request.error);
                return;
            }

            var bytes = request.downloadHandler.data;
            var directory = AssetDatabase.GetAssetPath(_importer.output);
            var savePath = $"{directory}/{targetInfo.name}.png";
            File.WriteAllBytes(savePath, bytes);
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(savePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = targetInfo.borders;
            importer.SaveAndReimport();
        };
    }

    /// <summary>
    /// インポート対象のターゲットの情報
    /// </summary>
    private class TargetInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public string id;

        /// <summary>
        /// レイヤー名
        /// </summary>
        public string name;

        /// <summary>
        /// ボーダー
        /// </summary>
        public Vector4 borders;
    }

    /// <summary>
    /// ターゲット情報のリストを構築する
    /// </summary>
    private void BuildTargetInfoList(Node node, List<TargetInfo> result)
    {
        if (node.type == "TEXT") return; // テキストは対象外
        if (_targetNameRegex.IsMatch(node.name))
        {
            result.Add(GetTargetInfo(node));
            return;
        }

        if (node.children == null)
        {
            return;
        }

        foreach (var child in node.children)
        {
            BuildTargetInfoList(child, result);
        }
    }

    /// <summary>
    /// TargetInfoの取得
    /// ボーダーは子要素のレイアウト制約と位置から取得
    /// </summary>
    private TargetInfo GetTargetInfo(Node node)
    {
        var bounds = node.absoluteRenderBounds;
        var left = 0f;
        var top = 0f;
        var right = 0f;
        var bottom = 0f;

        foreach (var child in node.children)
        {
            switch (child.constraints.horizontal)
            {
                case "LEFT":
                    left = Mathf.Max(left, child.absoluteRenderBounds.MaxX - bounds.x);
                    break;
                case "RIGHT":
                    right = Mathf.Max(right, bounds.MaxX - child.absoluteRenderBounds.x);
                    break;
            }

            switch (child.constraints.vertical)
            {
                case "TOP":
                    top = Mathf.Max(top, child.absoluteRenderBounds.MaxY - bounds.y);
                    break;
                case "BOTTOM":
                    bottom = Mathf.Max(bottom, bounds.MaxY - child.absoluteRenderBounds.y);
                    break;
            }
        }

        return new TargetInfo
        {
            id = node.id,
            name = node.name,
            borders = new Vector4(left, top, right, bottom)
        };
    }
}
