using System;
using System.Collections.Generic;
using UnityEngine;

namespace BBImporter
{
    public class BBKeyFrame
    {
        public string channel;
        public List<Dictionary<string, string>> data_points;
        public string uuid;
        public float time;
        public int color;
        public string interpolation;
        public bool bezier_linked;
        public List<float> bezier_left_time;
        public List<int> bezier_left_value;
        public List<float> bezier_right_time;
        public List<int> bezier_right_value;
        public BBKeyFrameChannel GetChannel()
        {
            switch (channel)
            {
                case "rotation": return BBKeyFrameChannel.rotatiom;
                case "position": return BBKeyFrameChannel.position;
                case "scale": return BBKeyFrameChannel.scale;
                default: throw new NotImplementedException($"Channel {channel} is not yet implemented");
            }
        }
        public Vector3 GetDataPoints()
        {
            var xStr = data_points[0]["x"].Trim();
            var yStr = data_points[0]["y"].Trim();
            var zStr = data_points[0]["z"].Trim();
            var xVal = string.IsNullOrEmpty(xStr)?0:float.Parse(xStr);
            var yVal = string.IsNullOrEmpty(yStr)?0:float.Parse(yStr);
            var zVal = string.IsNullOrEmpty(zStr)?0:float.Parse(zStr);
            return new Vector3(xVal, yVal, zVal);
        }
    }
    
}