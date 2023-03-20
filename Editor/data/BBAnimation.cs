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

            if (animators != null)
            {
                foreach (var kv in animators)
                {
                    if (kv.Value.HasChannel(BBKeyFrameChannel.position))
                        AddPositionChannel(kv.Value, ret, groups[kv.Key]);
                    if (kv.Value.HasChannel(BBKeyFrameChannel.rotatiom))
                        AddRotationChannel(kv.Value, ret, groups[kv.Key]);
                    if (kv.Value.HasChannel(BBKeyFrameChannel.scale))
                        AddScaleChannel(kv.Value, ret, groups[kv.Key]);
                }
            }
            else
            {
                Debug.LogWarning($"No bone data found for animation {name} uuid {uuid}");
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
                var dataPoints = bbKeyFrame.GetDataPoints();
                curveX.AddKey(bbKeyFrame.time, dataPoints.x);
                curveZ.AddKey(bbKeyFrame.time, dataPoints.y);
                curveY.AddKey(bbKeyFrame.time, dataPoints.z);
            }
            var path = GetPath(groupObject.transform);
            clip.SetCurve(path, typeof(Transform), "position.x", curveX);
            clip.SetCurve(path, typeof(Transform), "position.y", curveY);
            clip.SetCurve(path, typeof(Transform), "position.z", curveZ);
        }
        private void AddScaleChannel(BBAnimator animator, AnimationClip clip, GameObject groupObject)
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
            var path = GetPath(groupObject.transform);
            clip.SetCurve(path, typeof(Transform), "localScale.x", curveX);
            clip.SetCurve(path, typeof(Transform), "localScale.y", curveY);
            clip.SetCurve(path, typeof(Transform), "localScale.z", curveZ);
        }
        private void AddRotationChannel(BBAnimator animator, AnimationClip clip, GameObject groupObject)
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
            var path = GetPath(groupObject.transform);
            clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
        }
        private string GetPath(Transform trans)
        {
            List<string> parents = new List<string>();
            while (trans.transform != trans.root)
            {
                parents.Add(trans.name);
                trans = trans.parent;
            }
            parents.Reverse();
            return string.Join("/", parents);
        }
    }
}