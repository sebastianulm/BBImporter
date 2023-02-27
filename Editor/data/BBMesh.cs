using System.Collections.Generic;

namespace BBImporter
{
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
}