using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BBImporter
{
    public class BBOutliner
    {
        public string name;
        public float[] origin;
        public int color;
        public string uuid;
        public bool export;
        public bool mirror_uv;
        public bool isOpen;
        public bool locked;
        public bool visibility;
        public int autouv;
    }
}