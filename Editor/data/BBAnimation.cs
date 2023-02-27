using System.Collections.Generic;

namespace BBImporter
{
    public class BBAnimation
    {
        public string uuid;
        public string name;
        public string loop;
        public bool @override;
        public float length;
        public int snapping;
        public bool selected;
        public string anim_time_update;
        public string blend_weight;
        public string start_delay;
        public string loop_delay;
        public Dictionary<string, BBAnimator> animators;
    }
}