using System.Collections.Generic;
using UnityEngine;

namespace BBImporter
{
    public class BBAnimator
    {
        public string name;
        public string type;
        public List<BBKeyFrame> keyframes;
        public bool HasChannel(BBKeyFrameChannel channel)
        {
            foreach (var keyframe in keyframes)
            {
                if (keyframe.GetChannel() == channel)
                    return true;
            }
            return false;
        }
    }
}