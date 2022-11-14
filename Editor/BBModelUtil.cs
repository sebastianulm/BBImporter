using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BBImporter
{
    public static class BBModelUtil
    {
        internal static Vector3 ReadVector3(JToken arr)
        {
            if (arr == null) return Vector3.zero;
            var ret = new Vector3();
            ret.x = arr[0].Value<float>();
            ret.y = arr[1].Value<float>();
            ret.z = -arr[2].Value<float>();
            return ret;
        }
        internal static Vector3 ReadVector3(float[] arr)
        {
            var ret = new Vector3();
            if (arr == null) return Vector3.zero;
            ret.x = arr[0];
            ret.y = arr[1];
            ret.z = -arr[2];
            return ret;
        }
        internal static Quaternion ReadQuaternion(float[] arr)
        {
            if (arr == null) return Quaternion.identity;
            return Quaternion.Euler(arr[0], -arr[1], -arr[2]);
        }
        internal static Vector2 ReadVector2(float?[] arr)
        {
            var ret = new Vector2();
            Debug.Assert(arr.Length == 2);
            ret.x = arr[0].HasValue?arr[0].Value:0;
            ret.y = arr[1].HasValue?arr[1].Value:0;
            return ret;
        }
    }
}