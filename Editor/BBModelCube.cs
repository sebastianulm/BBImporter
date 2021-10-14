using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace Editor
{
    public class BBModelCube
    {
        private readonly BBModelImporter.BBCube cube;
        private Vector3 from;
        private Vector3 to;
        private Vector3 origin;
        private float inflate;
        private Quaternion rotation;
        private Vector3[] boxVertices = new Vector3[8];

        public BBModelCube(JToken element)
        {
            cube = element.ToObject<BBModelImporter.BBCube>();
            from = BBModelUtil.ReadVector3(cube.from);
            to = BBModelUtil.ReadVector3(cube.to);
            SortComponents(ref @from, ref to);
            origin = BBModelUtil.ReadVector3(cube.origin);
            inflate = cube.inflate;
            rotation = BBModelUtil.ReadQuaternion(cube.rotation);
            MakeBoxVertices();
        }
        public void GetMesh(List<BBVertex> vertices, Dictionary<int, List<int>> triangles, List<Vector2> textureSizes)
        {
            this.DebugDrawVertices();
            foreach (var face in cube.faces)
            {
                int material = face.Value.texture;
                var boxUVs = MakeBoxUVs(face.Key, face.Value.uv, textureSizes[material]);
                switch (face.Key)
                {
                    case "north":
                        AddFace(face.Value.texture, vertices, triangles, new[]
                        {
                            0, 5, 7, 0, 7, 2
                        }, boxUVs);
                        break;
                    case "south":
                        AddFace(face.Value.texture, vertices, triangles, new[]
                        {
                            4, 1, 3, 4, 3, 6
                        }, boxUVs);
                        break;
                    case "east":
                        AddFace(face.Value.texture, vertices, triangles, new[]
                        {
                            1, 0, 2, 1, 2, 3
                        }, boxUVs);
                        break;
                    case "west":
                        AddFace(face.Value.texture, vertices, triangles, new[]
                        {
                            5, 4, 6, 5, 6, 7
                        }, boxUVs);
                        break;
                    case "up":
                        AddFace(face.Value.texture, vertices, triangles, new[]
                        {
                            5, 0, 1, 5, 1, 4
                        }, boxUVs);
                        break;
                    case "down":
                        AddFace(face.Value.texture, vertices, triangles, new[]
                        {
                            6, 3, 2, 6, 2, 7
                        }, boxUVs);
                        break;
                }
            }
        }

        private void AddFace(int material, List<BBVertex> vertices, Dictionary<int, List<int>> triangles, int[] boxOffsets, Vector2[] boxUVs)
        {
            for (int i = 0; i < 6; i++)
            {
                BBVertex vertex = new BBVertex(boxVertices[boxOffsets[i]], boxUVs[i]);
                var triIdx = vertices.Count;
                vertices.Add(vertex);
                if (!triangles.TryGetValue(material, out var list))
                {
                    list = new List<int>();
                    triangles[material] = list;
                }
                list.Add(triIdx);
            }
        }
        // Face => TopLeft, BottomLeft, topRight, TopRight, BottomLeft, BottomRight
        private Vector2[] MakeBoxUVs(string faceKey, float[] faceUVs, Vector2 textureSize)
        {
            var topLeft = new Vector2(faceUVs[0] / textureSize.x, faceUVs[1] / textureSize.y);
            var topRight = new Vector2(faceUVs[2] / textureSize.x, faceUVs[1] / textureSize.y);
            var bottomLeft = new Vector2(faceUVs[0] / textureSize.x, faceUVs[3] / textureSize.y);
            var bottomRight = new Vector2(faceUVs[2] / textureSize.x, faceUVs[3] / textureSize.y);
            switch (faceKey)
            {
                case "north": //+z axis
                    return new[]
                    {
                        topRight, topLeft, bottomLeft, Vector2.zero, Vector2.zero, Vector2.zero,
                    };
                case "south": //-z axis
                    return new[]
                    {
                        topRight, topLeft, bottomLeft, topLeft, bottomRight, bottomLeft,
                    };
                case "east": //+x axis
                    return new[]
                    {
                        topRight, topLeft, bottomLeft, topLeft, bottomRight, bottomLeft,
                    };
                case "west": //+x axis
                    return new[]
                    {
                        topRight, topLeft, bottomLeft, topLeft, bottomRight, bottomLeft,
                    };
                case "up": //+y axis
                    return new[]
                    {
                        topRight, topLeft, bottomLeft, topRight, bottomLeft, topLeft,
                    };
                case "down": //-y axis
                    return new[]
                    {
                        topRight, topLeft, bottomLeft, topLeft, bottomRight, bottomLeft,
                    };
            }
            throw new NotImplementedException();
        }
        private void MakeBoxVertices()
        {
            //X
            boxVertices[4].x = from[0] - inflate;
            boxVertices[5].x = from[0] - inflate;
            boxVertices[6].x = from[0] - inflate;
            boxVertices[7].x = from[0] - inflate;
            //Y
            boxVertices[2].y = from[1] - inflate;
            boxVertices[3].y = from[1] - inflate;
            boxVertices[6].y = from[1] - inflate;
            boxVertices[7].y = from[1] - inflate;
            //Z
            boxVertices[3].z = from[2] - inflate;
            boxVertices[4].z = from[2] - inflate;
            boxVertices[1].z = from[2] - inflate;
            boxVertices[6].z = from[2] - inflate;

            //X
            boxVertices[0].x = to[0] + inflate;
            boxVertices[1].x = to[0] + inflate;
            boxVertices[2].x = to[0] + inflate;
            boxVertices[3].x = to[0] + inflate;
            //Y
            boxVertices[0].y = to[1] + inflate;
            boxVertices[1].y = to[1] + inflate;
            boxVertices[4].y = to[1] + inflate;
            boxVertices[5].y = to[1] + inflate;
            //Z
            boxVertices[0].z = to[2] + inflate;
            boxVertices[2].z = to[2] + inflate;
            boxVertices[5].z = to[2] + inflate;
            boxVertices[7].z = to[2] + inflate;
            for (var i = 0;
                i < boxVertices.Length;
                i++)
            {
                boxVertices[i] = (rotation * (boxVertices[i] - origin)) + origin;
            }
        }
        private void DebugDrawVertices()
        {
            Debug.DrawLine(Vector3.zero, boxVertices[0] * 1.1f, Color.red, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[1] * 1.1f, Color.green, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[2] * 1.1f, Color.blue, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[3] * 1.1f, Color.yellow, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[4] * 1.1f, Color.magenta, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[5] * 1.1f, Color.cyan, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[6] * 1.1f, Color.white, 2000);
            Debug.DrawLine(Vector3.zero, boxVertices[7] * 1.1f, Color.black, 2000);
        }
        private static void SortComponents(ref Vector3 a, ref Vector3 b)
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
    }
}