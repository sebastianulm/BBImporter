using System;
using UnityEngine;

namespace BBImporter
{
    public class BBVertex : IEquatable<BBVertex>
    {
        public readonly Vector3 position;
        public readonly Vector2 uv;

        public BBVertex(Vector3 position, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
        }
        public bool Equals(BBVertex other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return position.Equals(other.position) && uv.Equals(other.uv);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BBVertex) obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (position.GetHashCode() * 397) ^ uv.GetHashCode();
            }
        }
        public static bool operator ==(BBVertex left, BBVertex right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(BBVertex left, BBVertex right)
        {
            return !Equals(left, right);
        }
        public BBVertex Transform(Matrix4x4 orientation)
        {
            return new BBVertex(orientation .MultiplyPoint3x4(position), uv);
        }
        public override string ToString()
        {
            return $"[{position}]";
        }
    }
}