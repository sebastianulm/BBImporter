using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

[ScriptedImporter(1, "bbmodel")]
public class BBModelImporter : ScriptedImporter
{
    public Material materialTemplate;
    private static readonly int Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int Smoothness = Shader.PropertyToID("_Glossiness");
    public override void OnImportAsset(AssetImportContext ctx)
    {
        //var basePath = Path.GetDirectoryName(ctx.assetPath) + "/" + Path.GetFileNameWithoutExtension(ctx.assetPath);
        string file = File.ReadAllText(ctx.assetPath);

        var obj = JObject.Parse(file);
        var materials = LoadMaterials(ctx, obj);
       
        LoadGroup(ctx, obj, obj["outliner"], "", materials);
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
    private void LoadGroup(AssetImportContext ctx, JObject obj, JToken currentGroup, string currentPrefix, List<Material> material)
    {
        List<Vector2> textureSizes = new List<Vector2>();
        foreach (var mat in material)
        {
            if (mat.mainTexture != null)
            {
                textureSizes.Add(new Vector2(mat.mainTexture.width, mat.mainTexture.height));
            }
        }
        foreach (var entry in currentGroup)
        {
            switch (entry.Type)
            {
                case JTokenType.String:
                    var cubeMesh = LoadMesh(obj, entry.Value<string>(), currentPrefix, textureSizes);
                    if (cubeMesh != null)
                    {
                        GameObject go = new GameObject(cubeMesh.name);
                        var filter = go.AddComponent<MeshFilter>();
                        var renderer = go.AddComponent<MeshRenderer>();
                        filter.sharedMesh = cubeMesh;
                        renderer.materials = material.ToArray();
                        ctx.AddObjectToAsset(entry.Value<string>(), cubeMesh);
                        ctx.AddObjectToAsset(entry.Value<string>(), go);
                        if (ctx.mainObject == null) 
                            ctx.SetMainObject(go);
                    }
                        
                    break;
                case JTokenType.Object:
                    LoadGroup(ctx, obj, entry["children"], entry["name"].Value<string>() + "/", material);
                    break;
                default:
                    Debug.Log("Unhandled type " + entry.Type);
                    break;
            }
        }
    }
    private Mesh LoadMesh(JObject file, string guid, string currentPrefix, List<Vector2> textureSizes)
    {
        var element = file["elements"].First(x => x.Value<string>("uuid") == guid);
        var type = element["type"];
        if (type == null)
        {
            return MakeCubeMesh(element, currentPrefix, textureSizes);
        }
        else if (type.Value<string>() == "mesh")
        {
            return MakeMesh(element, currentPrefix, textureSizes);
        }
        return null;
    }
    private Mesh MakeMesh(JToken element, string currentPrefix, List<Vector2> textureSizes)
    {
        var bbMesh = element.ToObject<BBMesh>();
        var indices = new List<int>();
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        Dictionary<int, List<int>> triangles = new Dictionary<int, List<int>>();
        foreach (var faceEntry in bbMesh.faces)
        {
            if (faceEntry.Value.vertices.Length == 3)
            {
                int start = vertices.Count;
                vertices.Add(ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[0]]));
                vertices.Add(ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[1]]));
                vertices.Add(ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[2]]));
                if (textureSizes.Count > faceEntry.Value.texture)
                {
                    var texSize = textureSizes[faceEntry.Value.texture];
                    uvs.Add(ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[0]]) / texSize);
                    uvs.Add(ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[1]]) / texSize);
                    uvs.Add(ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[2]]) / texSize);
                }

                if (!triangles.TryGetValue(faceEntry.Value.texture, out var list))
                {
                    list = new List<int>();
                    triangles[faceEntry.Value.texture] = list;
                }
                for (int i = 0; i < 3; i++)
                {
                    list.Add(start + i);   
                }
            }
            else if (faceEntry.Value.vertices.Length == 4)
            {
                var quadVertices = new[]
                {
                    ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[0]]), ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[1]]), ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[2]]), ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[3]])
                };
                Vector2[] quadUVs = null;
                if (textureSizes.Count > faceEntry.Value.texture)
                {
                    quadUVs = new Vector2[]
                    {
                        ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[0]]), ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[1]]), ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[2]]), ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[3]]),
                    };
                    var texSize = textureSizes[faceEntry.Value.texture];
                    for (var i = 0; i < quadUVs.Length; i++)
                    {
                        quadUVs[i] /= texSize;
                    }
                }
                QuadToTris(quadVertices, quadUVs, out var triVertices, out var triUVs);
                var start = vertices.Count;
                vertices.AddRange(triVertices);
                if (triUVs != null)
                {
                    uvs.AddRange(triUVs);
                }
                if (!triangles.TryGetValue(faceEntry.Value.texture, out var list))
                {
                    list = new List<int>();
                    triangles[faceEntry.Value.texture] = list;
                }
                for (int i = 0; i < 6; i++)
                {
                    list.Add(start + i);   
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        var ret = new Mesh();
        ret.name = currentPrefix + bbMesh.name;
        ret.SetVertices(vertices);
        if (uvs.Count > 0)
        {
            ret.SetUVs(0,uvs);
        }
        ret.subMeshCount = triangles.Count;
        foreach (var subMesh in triangles.OrderBy(x => x.Key))
        {
            ret.SetTriangles(subMesh.Value, subMesh.Key);
        }
        ret.RecalculateNormals();
        ret.RecalculateBounds();
        return ret;
    }
    private Mesh MakeCubeMesh(JToken element, string currentPrefix, List<Vector2> textureSizes)
    {
        var from = ReadVector3(element["from"]);
        var to = ReadVector3(element["to"]);
        var origin = ReadVector3(element["origin"]);
        float inflate = element["inflate"]?.Value<float>() ?? 0f;
        var angles = ReadVector3(element["rotation"]);

        var ret = new Mesh
        {
            name = currentPrefix + element["name"].Value<string>(),
            vertices = MakeCubeVertices(from, to, origin, inflate, angles),
        };
        ret.SetTriangles(MakeCubeTriangles(), 0);
        ret.RecalculateNormals();
        ret.RecalculateBounds();
        return ret;
    }

    private Vector3 ReadVector3(JToken arr)
    {
        if (arr == null) return Vector3.zero;
        var ret = new Vector3();
        ret.x = arr[0].Value<float>();
        ret.y = arr[1].Value<float>();
        ret.z = arr[2].Value<float>();
        return ret;
    }
    private Vector3 ReadVector3(float[] arr)
    {
        var ret = new Vector3();
        Debug.Assert(arr.Length == 3);
        ret.x = arr[0];
        ret.y = arr[1];
        ret.z = arr[2];
        return ret;
    }
    private Vector2 ReadVector2(float[] arr)
    {
        var ret = new Vector2();
        Debug.Assert(arr.Length == 2);
        ret.x = arr[0];
        ret.y = arr[1];
        return ret;
    }

    public static void QuadToTris(Vector3[] quad, Vector2[] quadUVs, out Vector3[] vertices, out Vector2[] uvs)
    {
        Debug.Assert(quad.Length == 4);
        var a = quad[0];
        var b = quad[1];
        var c = quad[2];
        var d = quad[3];

        var ba = b - a;
        var ca = c - a;
        var da = d - a;
        //Quads are sometimes sorted bad. Original code uses some really odd way of sorthing this. This is simpler, but different, and might break.
        var normal = Vector3.Cross(ba, ca);
        var acAngle = Vector3.SignedAngle(ba, ca, normal);
        var adAngle = Vector3.SignedAngle(ba, da, normal);

        if (acAngle > adAngle)
        {
            vertices = new[] {a, b, c, d, c, b};
            if (quadUVs != null)
            {
                uvs = new[] {quadUVs[0], quadUVs[1], quadUVs[2], quadUVs[3], quadUVs[2], quadUVs[1]};
            }
            else
            {
                uvs = null;
            }
        }
        else
        {
            vertices = new[] {a, b, c, c, d, a};
            if (quadUVs != null)
            {
                uvs = new[] {quadUVs[0], quadUVs[1], quadUVs[2], quadUVs[2], quadUVs[3], quadUVs[0]};
            }
            else
            {
                uvs = null;
            }
        }
    }
    public static Vector3[] MakeCubeVertices(Vector3 from, Vector3 to, Vector3 origin, float inflate, Vector3 angles)
    {
        SortComponents(ref from, ref to);
        Vector3 center = (from + to) * 0.5f;

        var rot = Quaternion.Euler(angles);

        var offset = origin;

        from.x -= inflate;
        from.y -= inflate;
        from.z -= inflate;

        to.x += inflate;
        to.y += inflate;
        to.z += inflate;


        var c0 = (rot * new Vector3(from.x, from.y, to.z) - offset) + offset;
        var c1 = (rot * new Vector3(to.x, from.y, to.z) - offset) + offset;
        var c2 = (rot * new Vector3(to.x, from.y, from.z) - offset) + offset;
        var c3 = (rot * new Vector3(from.x, from.y, from.z) - offset) + offset;

        var c4 = (rot * new Vector3(from.x, to.y, to.z) - offset) + offset;
        var c5 = (rot * new Vector3(to.x, to.y, to.z) - offset) + offset;
        var c6 = (rot * new Vector3(to.x, to.y, from.z) - offset) + offset;
        var c7 = (rot * new Vector3(from.x, to.y, from.z) - offset) + offset;

        return new Vector3[]
        {
            // Bottom
            c0, c1, c2, c3,
            // Left
            c7, c4, c0, c3,
            // Front
            c4, c5, c1, c0,
            // Back
            c6, c7, c3, c2,
            // Right
            c5, c6, c2, c1,
            // Top
            c7, c6, c5, c4,
        };
    }
    public int[] MakeCubeTriangles()
    {
        return new int[]
        {
            // Bottom
            3, 1, 0, 3, 2, 1,
            // Left
            7, 5, 4, 7, 6, 5,
            // Front
            11, 9, 8, 11, 10, 9,
            // Back
            15, 13, 12, 15, 14, 13,
            // Right
            19, 17, 16, 19, 18, 17,
            // Top
            23, 21, 20, 23, 22, 21,
        };
    }

    public static void SortComponents(ref Vector3 a, ref Vector3 b)
    {
        if (a.x > b.x)
        {
            float tmp = a.x;
            a.x = b.x;
            b.x = tmp;
        }
        if (a.y > b.y)
        {
            float tmp = a.y;
            a.y = b.y;
            b.y = tmp;
        }
        if (a.z > b.z)
        {
            float tmp = a.z;
            a.z = b.z;
            b.z = tmp;
        }
    }

    private class BBMesh
    {
        public int color;
        public float[] origin;
        public float[] rotation;
        public bool visibility;
        public string name;
        public Dictionary<string, float[]> vertices;
        public Dictionary<string, BBFace> faces;
        public string type;
        public string uuid;
    }

    private class BBFace
    {
        public Dictionary<string, float[]> uv;
        public string[] vertices;
        public int texture;
    }

    private class BBTexture
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