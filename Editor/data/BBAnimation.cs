using System;
using System.Collections.Generic;
using UnityEngine;

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
        public AnimationClip ToClip(Dictionary<string, GameObject> groups)
        {
            AnimationClip ret = new AnimationClip()
            {
                name = name,
                frameRate = snapping,
                wrapMode = GetWrapMode(),
                legacy = true,
            };

            foreach (var kv in animators)
            {
                if (kv.Value.HasChannel(BBKeyFrameChannel.position))
                    AddPositionChannel(kv.Value, ret, groups[kv.Key]);
                if (kv.Value.HasChannel(BBKeyFrameChannel.rotatiom))
                    AddRotationChannel(kv.Value, ret);
                if (kv.Value.HasChannel(BBKeyFrameChannel.scale))
                    AddScaleChannel(kv.Value, ret);
            }
            return ret;
        }
        public WrapMode GetWrapMode()
        {
            switch (loop)
            {
                case "once": return WrapMode.Once;
                case "loop": return WrapMode.Loop;
                case "hold": return WrapMode.ClampForever;
                default: throw new NotImplementedException($"Wrap mode {loop} is not yet implemented");
            }
        }
        private void AddPositionChannel(BBAnimator animator, AnimationClip clip, GameObject groupObject)
        {
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();
            foreach (var bbKeyFrame in animator.keyframes)
            {
                if (bbKeyFrame.GetChannel() != BBKeyFrameChannel.position)
                    continue;
                var position = groupObject.transform.position;
                var dataPoints = bbKeyFrame.GetDataPoints();
                curveX.AddKey(bbKeyFrame.time, dataPoints.x + position.x);
                curveZ.AddKey(bbKeyFrame.time, dataPoints.y + position.z);
                curveY.AddKey(bbKeyFrame.time, dataPoints.z + position.y);
            }
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localPosition.x", curveX);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localPosition.y", curveY);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localPosition.z", curveZ);
        }
        private void AddScaleChannel(BBAnimator animator, AnimationClip clip)
        {
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();
            foreach (var bbKeyFrame in animator.keyframes)
            {
                if (bbKeyFrame.GetChannel() != BBKeyFrameChannel.scale)
                    continue;
                var dataPoints = bbKeyFrame.GetDataPoints();
                curveX.AddKey(bbKeyFrame.time, dataPoints.x);
                curveY.AddKey(bbKeyFrame.time, dataPoints.y);
                curveZ.AddKey(bbKeyFrame.time, dataPoints.z);
            }
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localScale.x", curveX);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localScale.y", curveY);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localScale.z", curveZ);
        }
        private void AddRotationChannel(BBAnimator animator, AnimationClip clip)
        {
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();
            var curveW = new AnimationCurve();
            foreach (var bbKeyFrame in animator.keyframes)
            {
                if (bbKeyFrame.GetChannel() != BBKeyFrameChannel.rotatiom)
                    continue;
                var dataPoints = bbKeyFrame.GetDataPoints();
                var quat = Quaternion.Euler(dataPoints.x, dataPoints.y, dataPoints.z);
                curveX.AddKey(bbKeyFrame.time, quat.x);
                curveY.AddKey(bbKeyFrame.time, quat.y);
                curveZ.AddKey(bbKeyFrame.time, quat.z);
                curveW.AddKey(bbKeyFrame.time, quat.w);
            }
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localRotation.x", curveX);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localRotation.y", curveY);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localRotation.z", curveZ);
            clip.SetCurve(animator.name + "-Group", typeof(Transform), "localRotation.w", curveW);
        }
    }
}