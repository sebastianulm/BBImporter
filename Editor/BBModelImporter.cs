using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BBImporter
{
    [ScriptedImporter(1, "bbmodel")]
    public class BBModelImporter : ScriptedImporter
    {
        [SerializeField] private Material materialTemplate;
        [SerializeField] private MeshImportMode importMode;
        [SerializeField] private bool filterHidden;
        [SerializeField] private string ignoreName;
        [SerializeField] private bool addAnimation;
        
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");
        private static readonly int Smoothness = Shader.PropertyToID("_Glossiness");

        private Dictionary<string, GameObject> groups;

        private Vector3 Resolution;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            groups = new Dictionary<string, GameObject>();
            //var basePath = Path.GetDirectoryName(ctx.assetPath) + "/" + Path.GetFileNameWithoutExtension(ctx.assetPath);
            string file = File.ReadAllText(ctx.assetPath);
            var obj = JObject.Parse(file);
            var materials = LoadMaterials(ctx, obj);
            Resolution = LoadResolution(obj);
            switch (importMode)
            {
                case MeshImportMode.MergeAllIntoOneObject:
                    CombineGroup(ctx, obj, materials);
                    break;
                case MeshImportMode.SeparateObjects:
                    LoadGroup(ctx, obj, materials);
                    break;
                case MeshImportMode.WithHierarchyAndAnimations:
                    LoadHierarchy(ctx, obj, materials);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var animations = LoadAnimations(ctx, obj, groups);
        }
        private List<BBAnimation> LoadAnimations(AssetImportContext ctx, JObject obj, Dictionary<string, GameObject> groups)
        {
            var ret = new List<BBAnimation>();
            var animToken = obj["animations"];
            if (animToken is { HasValues: true })
            {
                var mainGO = ctx.mainObject as GameObject;
                Animation animation = null; 
                if (addAnimation && mainGO != null)
                    animation = mainGO.AddComponent<Animation>();
                foreach (var token in obj["animations"])
                {
                    var anim = token.ToObject<BBAnimation>();
                    ret.Add(anim);
                    var clip = anim.ToClip(groups);
                    ctx.AddObjectToAsset(anim.name, clip);
                    if (animation != null) 
                        animation.AddClip(clip, clip.name);
                }
            }
            return ret;
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
                            if (element["visibility"]?.Value<bool>() == false && filterHidden) 
                                continue;
                            if (element["name"]?.Value<string>().Equals(ignoreName, StringComparison.InvariantCultureIgnoreCase) == true) 
                                continue;
                            //Outline process origin. Needs to be subtracted from the mesh positions
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
            var guid = file["model_identifier"]?.Value<string>();
            var name = file["name"].Value<string>();
            if (string.IsNullOrEmpty(guid))
                guid = name;
            var go = mesh.BakeGameObject(ctx, name, guid, Vector3.zero);
            ctx.AddObjectToAsset(guid, go);
            ctx.SetMainObject(go);
        }
    
        private void LoadGroup(AssetImportContext ctx, JObject file, List<Material> material)
        {
            void LoadGroupRecursively(JToken currentGroup)
            {
                foreach (var entry in currentGroup)
                {
                    switch (entry.Type)
                    {
                        case JTokenType.String:
                            var guid = entry.Value<string>();
                            var element = file["elements"].First(x => x.Value<string>("uuid") == guid);
                            if (element["visibility"]?.Value<bool>() == false && filterHidden) 
                                continue;
                            var mesh = new BBModelMesh(material, Resolution);
                            var origin = element["origin"]?.Values<float>()?.ToArray().ReadVector3();
                            mesh.AddElement(file, element);
                            var goName = file["elements"].First(x => x.Value<string>("uuid") == entry.Value<string>()).Value<string>("name");
                            var newGO = mesh.BakeGameObject(ctx, goName, guid, origin??Vector3.zero);
                            ctx.AddObjectToAsset(guid, newGO);
                            if (ctx.mainObject == null)
                                ctx.SetMainObject(newGO);
                            break;
                        case JTokenType.Object:
                            //TODO: Handle visible = false here
                            LoadGroupRecursively(entry["children"]);
                            break;
                        default:
                            Debug.Log("Unhandled type " + entry.Type);
                            break;
                    }
                }
            }
            LoadGroupRecursively(file["outliner"]);
        }
        private void LoadHierarchy(AssetImportContext ctx, JObject file, List<Material> material)
        {
            void LoadGroupRecursively(JToken currentGroup, GameObject parent)
            {
                foreach (var entry in currentGroup)
                {
                    switch (entry.Type)
                    {
                        case JTokenType.String:
                            var guid = entry.Value<string>();
                            var element = file["elements"].First(x => x.Value<string>("uuid") == guid);
                            if (element["visibility"]?.Value<bool>() == false && filterHidden) 
                                continue;
                            var mesh = new BBModelMesh(material, Resolution);
                            var origin = element["origin"]?.Values<float>()?.ToArray().ReadVector3();
                            mesh.AddElement(file, element);
                            var goName = file["elements"].First(x => x.Value<string>("uuid") == entry.Value<string>()).Value<string>("name");
                            var go = mesh.BakeGameObject(ctx, goName, guid, origin??Vector3.zero);
                            go.transform.position = origin??Vector3.zero;
                            go.transform.SetParent(parent.transform, true);
                            break;
                        case JTokenType.Object:
                            var outliner = entry.ToObject<BBOutliner>();
                            var boneGO = new GameObject(outliner.name + "-Group");
                            boneGO.transform.position = outliner.origin.ReadVector3();
                            boneGO.transform.SetParent(parent.transform, true);
                            groups.Add(outliner.uuid, boneGO);
                            LoadGroupRecursively(entry["children"], boneGO);
                            break;
                        default:
                            Debug.Log("Unhandled type " + entry.Type);
                            break;
                    }
                }
            }
            var guid = file["model_identifier"]?.Value<string>();
            var nameOfObject = file["name"].Value<string>();
            if (string.IsNullOrEmpty(guid))
                guid = nameOfObject;
            var rootGO = new GameObject();
            ctx.AddObjectToAsset(guid, rootGO);
            ctx.SetMainObject(rootGO);
            LoadGroupRecursively(file["outliner"], rootGO);
        }
    }

    public enum MeshImportMode
    {
        WithHierarchyAndAnimations,
        MergeAllIntoOneObject,
        SeparateObjects,
    }
}