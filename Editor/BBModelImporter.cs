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
        [SerializeField] private bool combineMeshes;
        [SerializeField] private bool filterHidden;
    
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Smoothness = Shader.PropertyToID("_Glossiness");

        private Vector3 Resolution;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            //var basePath = Path.GetDirectoryName(ctx.assetPath) + "/" + Path.GetFileNameWithoutExtension(ctx.assetPath);
            string file = File.ReadAllText(ctx.assetPath);

            var obj = JObject.Parse(file);
            var materials = LoadMaterials(ctx, obj);
            Resolution = LoadResolution(obj);
            if (combineMeshes)
            {
                CombineGroup(ctx, obj, materials);
            }
            else
            {
                LoadGroup(ctx, obj, materials);
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
                ctx.AddObjectToAsset(mat.name, mat);
                ret.Add(mat);
            }
            return ret;
        }
        private void CombineGroup(AssetImportContext ctx, JObject file, List<Material> material)
        {
            BBModelMesh mesh = new BBModelMesh(material, Resolution);
            void CombineGroupRecursive(JToken currentGroup, string currentPrefix)
            {
                foreach (var entry in currentGroup)
                {
                    switch (entry.Type)
                    {
                        case JTokenType.String:
                            var guid = entry.Value<string>();
                            var element = file["elements"].First(x => x.Value<string>("uuid") == guid);
                            if (element["visibility"]?.Value<bool>() == false && filterHidden) continue;
                            mesh.AddElement(file, element);
                            break;
                        case JTokenType.Object:
                            //TODO: Handle visible = false here
                            CombineGroupRecursive(entry["children"], entry["name"].Value<string>() + "/");
                            break;
                        default:
                            Debug.Log("Unhandled type " + entry.Type);
                            break;
                    }
                }
            }
            CombineGroupRecursive(file["outliner"], "");
            mesh.BakeGameObject(ctx, file["name"].Value<string>());
        }
    
        private void LoadGroup(AssetImportContext ctx, JObject file, List<Material> material)
        {
            void LoadGroupRecursively(JToken currentGroup, string currentPrefix)
            {
                foreach (var entry in currentGroup)
                {
                    switch (entry.Type)
                    {
                        case JTokenType.String:
                            var guid = entry.Value<string>();
                            var element = file["elements"].First(x => x.Value<string>("uuid") == guid);
                            if (element["visibility"]?.Value<bool>() == false && filterHidden) continue;
                            var mesh = new BBModelMesh(material, Resolution);
                            mesh.AddElement(file, element);
                            var name = file["elements"].First(x => x.Value<string>("uuid") == entry.Value<string>()).Value<string>("name");
                            mesh.BakeGameObject(ctx, currentPrefix + name);
                            break;
                        case JTokenType.Object:
                            //TODO: Handle visible = false here
                            LoadGroupRecursively(entry["children"], entry["name"].Value<string>() + "/");
                            break;
                        default:
                            Debug.Log("Unhandled type " + entry.Type);
                            break;
                    }
                }
            }
            LoadGroupRecursively(file["outliner"], "");
        }
        public class BBMesh
        {
            public int color;
            public float[] origin;
            public float[] rotation;
            public bool? visibility;
            public string name;
            public Dictionary<string, float[]> vertices;
            public Dictionary<string, BBMeshFace> faces;
            public string type;
            public string uuid;
        }

        public class BBMeshFace
        {
            public Dictionary<string, float?[]> uv;
            public string[] vertices;
            public int texture;
        }

        public class BBCube
        {
            public string name;
            public bool rescale;
            public float[] from;
            public float[] to;
            public int autouv;
            public int color;
            public bool locked;
            public float[] rotation;
            public float[] origin;
            public float inflate;
            public bool? visibility;
            public Dictionary<string, BBCubeFace> faces;
            public string uuid;
        }

        public class BBCubeFace
        {
            public float[] uv;
            public int rotation;
            public int texture;
        }

        public class BBTexture
        {
            public string path;
            public string name;
            public string folder;
            public string @namespace;
            public string id;
            public bool particle;
            public string render_mode;
            public bool visible;
            public string mode;
            public bool saved;
            public string uuid;
            public string relative_path;
            public string source;
        }
    }
}