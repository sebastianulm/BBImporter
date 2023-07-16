using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BBImporter
{
    [ScriptedImporter(1, "bbmodel")]
    public class BBModelImporter : ScriptedImporter
    {
        [SerializeField] private Material materialTemplate;
        [SerializeField] private MeshImportMode importMode;
        [SerializeField] private bool filterHidden;
        [SerializeField] private string ignoreName;

        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Smoothness = Shader.PropertyToID("_Glossiness");


        private Vector2 Resolution;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            //var basePath = Path.GetDirectoryName(ctx.assetPath) + "/" + Path.GetFileNameWithoutExtension(ctx.assetPath);
            string file = File.ReadAllText(ctx.assetPath);
            var obj = JObject.Parse(file);
            var materials = LoadMaterials(ctx, obj);
            Resolution = LoadResolution(obj);
            switch (importMode)
            {
                case MeshImportMode.MergeAllIntoOneObject:
                {
                    var importer = new BBModelImportMerged(Resolution, filterHidden, ignoreName, materials);
                    importer.ParseOutline(ctx, obj);
                    break;
                }
                case MeshImportMode.SeparateObjects:
                {
                    var importer = new BBModelImportSeparate(Resolution, filterHidden, ignoreName, materials);
                    importer.ParseOutline(ctx,obj);
                    break;
                }
                case MeshImportMode.WithHierarchyAndAnimations:
                {
                    var importer = new BBModelImportHierarchy(Resolution, filterHidden, ignoreName, materials);
                    importer.ParseOutline(ctx, obj);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private Vector3 LoadResolution(JObject obj)
        {
            return new Vector3(obj["resolution"]["width"].Value<float>(), obj["resolution"]["height"].Value<float>());
        }
        private List<Material> LoadMaterials(AssetImportContext ctx, JObject obj)
        {
            var ret = new List<Material>();
            if (!obj["textures"].HasValues)
            {
                if (materialTemplate == null)
                {
                    materialTemplate = new Material(Shader.Find("Standard"));
                    materialTemplate.SetFloat(Metallic, 0f);
                    materialTemplate.SetFloat(Smoothness, 0f);
                }
                Material mat = Instantiate(materialTemplate);
                ret.Add(mat);
                ctx.AddObjectToAsset(mat.name, mat);
                return ret;
            }
            foreach (var token in obj["textures"])
            {
                var texture = token.ToObject<BBTexture>();
                if (materialTemplate == null)
                {
                    materialTemplate = new Material(Shader.Find("Standard"));
                    materialTemplate.SetFloat(Metallic, 0f);
                    materialTemplate.SetFloat(Smoothness, 0f);
                }
                Material mat = Instantiate(materialTemplate);
                if (texture != null)
                {
                    string[] texData = texture.source.Split(',');
                    Debug.Assert(texData[0] == "data:image/png;base64");
                    Texture2D tex = new Texture2D(2, 2);
                    tex.filterMode = FilterMode.Point;
                    var texBytes = Convert.FromBase64String(texData[1]);
                    tex.LoadImage(texBytes);
                    mat.mainTexture = tex;
                    tex.name = texture.name;
                    ctx.AddObjectToAsset(texture.uuid, tex);
                }
                mat.name = token["name"].Value<string>();
                var guid = token["uuid"].Value<string>();
                ctx.AddObjectToAsset(guid, mat);
                ret.Add(mat);
            }
            return ret;
        }
    }

    public enum MeshImportMode
    {
        WithHierarchyAndAnimations,
        MergeAllIntoOneObject,
        SeparateObjects,
    }
}