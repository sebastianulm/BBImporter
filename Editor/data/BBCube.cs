using System.Collections.Generic;

namespace BBImporter
{
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
}