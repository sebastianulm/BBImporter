using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BBImporter
{
    public class BBModelMesh
    {
        private readonly List<Material> materials;
        private readonly List<Vector2> textureSizes;
        private List<BBVertex> vertices;
        private Dictionary<int, List<int>> triangles;
        public BBModelMesh(List<Material> materials)
        {
            this.materials = materials;
            textureSizes = new List<Vector2>();
            vertices = new List<BBVertex>();
            triangles = new Dictionary<int, List<int>>();
            foreach (var mat in materials)
            {
                if (mat.mainTexture != null)
                {
                    textureSizes.Add(new Vector2(mat.mainTexture.width, mat.mainTexture.height));
                }
            }
            if (textureSizes.Count <= 0)
            {
                textureSizes.Add(Vector2.one);
            }
        }
        public void AddElement(JObject file, JToken element)
        {
            var type = element["type"];
            if (type == null)
            {
                ParseCube(element);
            }
            else if (type.Value<string>() == "mesh")
            {
                ParseMesh(element);
            }
        }
        public void BakeGameObject(AssetImportContext ctx, string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices.Select(x => x.position).ToArray();
            mesh.uv = vertices.Select(x => x.uv).ToArray();
            mesh.subMeshCount = triangles.Count;
            int count = 0;
            foreach (var submesh in triangles.OrderBy(x => x.Key))
            {
                mesh.SetTriangles(submesh.Value.ToArray(), count++);
            }
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            ctx.AddObjectToAsset(mesh.name, mesh);
            GameObject go = new GameObject();
            go.name = name;
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = triangles
                .Where(x => x.Value.Count > 0)
                .OrderBy(x => x.Key)
                .Select(x => materials[x.Key]).ToArray();
            ctx.AddObjectToAsset(name, go);
            if (ctx.mainObject == null)
                ctx.SetMainObject(go);
        }
        private void ParseCube(JToken element)
        {
            var bbCube = new BBModelCube(element);
            bbCube.GetMesh(vertices, triangles, textureSizes);
        }
        private void ParseMesh(JToken element)
        {
            var bbMesh = element.ToObject<BBModelImporter.BBMesh>();
            //Fix visibility
            foreach (var faceEntry in bbMesh.faces)
            {
                if (faceEntry.Value.vertices.Length == 3)
                {
                    CreateMeshTriangle(bbMesh, faceEntry);
                }
                else if (faceEntry.Value.vertices.Length == 4)
                {
                    CreateMeshQuad(bbMesh, faceEntry);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
        private void CreateMeshTriangle(BBModelImporter.BBMesh bbMesh, KeyValuePair<string, BBModelImporter.BBMeshFace> faceEntry)
        {
            int materialNum = faceEntry.Value.texture;
            if (!triangles.TryGetValue(materialNum, out var triangleList))
            {
                triangleList = new List<int>();
                triangles[materialNum] = triangleList;
            }
            for (var i = 2; i >= 0; i--)
            {
                Vector3 pos = BBModelUtil.ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[i]]);
                Vector2 uv = Vector2.zero;
                if (textureSizes.Count > materialNum)
                {
                    var texSize = textureSizes[materialNum];
                    uv = BBModelUtil.ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[0]]) / texSize;
                }
                var vert = new BBVertex(pos, uv);
                var num = vertices.Count;
                vertices.Add(vert);
                triangleList.Add(num);
            }
        }
        private void CreateMeshQuad(BBModelImporter.BBMesh bbMesh, KeyValuePair<string, BBModelImporter.BBMeshFace> faceEntry)
        {
            int materialNum = faceEntry.Value.texture;
            if (!triangles.TryGetValue(materialNum, out var triangleList))
            {
                triangleList = new List<int>();
                triangles[materialNum] = triangleList;
            }
            var quadVertices = new[]
            {
                BBModelUtil.ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[0]]), BBModelUtil.ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[1]]), BBModelUtil.ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[2]]), BBModelUtil.ReadVector3(bbMesh.vertices[faceEntry.Value.vertices[3]])
            };
            Vector2[] quadUVs = null;
            if (textureSizes.Count > materialNum)
            {
                quadUVs = new[]
                {
                    BBModelUtil.ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[0]]), BBModelUtil.ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[1]]), BBModelUtil.ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[2]]), BBModelUtil.ReadVector2(faceEntry.Value.uv[faceEntry.Value.vertices[3]]),
                };
            }
            QuadToTris(quadVertices, quadUVs, out var triVertices, out var triUVs);
            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = triVertices[i];
                Vector2 uv = Vector2.zero;
                if (textureSizes.Count > materialNum)
                {
                    var texSize = textureSizes[faceEntry.Value.texture];
                    uv = triUVs[i] / texSize;
                }
                var vert = new BBVertex(pos, uv);
                var num = vertices.Count;
                vertices.Add(vert);
                triangleList.Add(num);
            }
        }
        /*
    private static Vector3[] MakeCubeVertices(Vector3 from, Vector3 to, Vector3 origin, float inflate, Vector3 angles)
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
    private int[] MakeCubeTriangles()
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
    */
        private static void QuadToTris(Vector3[] quad, Vector2[] quadUVs, out Vector3[] vertices, out Vector2[] uvs)
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
                vertices = new[]
                {
                    //a, b, c, d, c, b
                    b, c, d, c, b, a
                };
                if (quadUVs != null)
                {
                    uvs = new[]
                    {
                        //quadUVs[0], quadUVs[1], quadUVs[2], quadUVs[3], quadUVs[2], quadUVs[1]
                        quadUVs[1], quadUVs[2], quadUVs[3], quadUVs[2], quadUVs[1], quadUVs[0]
                    };
                }
                else
                {
                    uvs = null;
                }
            }
            else
            {
                vertices = new[]
                {
                    //a, b, c, c, d, a
                    a, d, c, c, b, a
                };
                if (quadUVs != null)
                {
                    uvs = new[]
                    {
                        //quadUVs[0], quadUVs[1], quadUVs[2], quadUVs[2], quadUVs[3], quadUVs[0]
                        quadUVs[0], quadUVs[3], quadUVs[2], quadUVs[2], quadUVs[1], quadUVs[0]
                    };
                }
                else
                {
                    uvs = null;
                }
            }
        }
    }
}