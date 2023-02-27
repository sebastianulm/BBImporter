using System.Collections.Generic;
using UnityEngine;

namespace BBImporter
{
    public class BBKeyFrame
    {
        public string channel;
        public List<Vector3> data_points;
        public string uuid;
        public int time;
        public int color;
        public string interpolation;
        public bool bezier_linked;
        public List<float> bezier_left_time;
        public List<int> bezier_left_value;
        public List<float> bezier_right_time;
        public List<int> bezier_right_value;
    }
}