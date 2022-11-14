using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BBImporter
{
    public class ImportGizmo : MonoBehaviour
    {
        public static ImportGizmo GetImportGizmo()
        {
            var importGizmo = UnityEngine.Object.FindObjectOfType<ImportGizmo>();
            if (importGizmo != null)
                return importGizmo;
            var go = new GameObject();
            return go.AddComponent<ImportGizmo>();
        }
        
        private List<LineEntry> lines = new List<LineEntry>();
        private List<HandleEntry> handles = new List<HandleEntry>();
        private List<PlaneEntry> planes = new List<PlaneEntry>();


        public void OnDrawGizmosSelected()
        {
            foreach (var lineEntry in lines)
            {
                Gizmos.color = lineEntry.color;
                Gizmos.DrawLine(lineEntry.from, lineEntry.to);
            }
            foreach (var handleEntry in handles)
            {
                Handles.color = handleEntry.color;
                Handles.Label(handleEntry.pos, handleEntry.text);
            }
            foreach (var plane in planes)
            {
                Handles.color = plane.color;
                var planeCenter = plane.plane.normal * plane.plane.distance;
                Handles.DrawSolidDisc(planeCenter, plane.plane.normal, 2);
                Handles.DrawLine(planeCenter, planeCenter + plane.plane.normal);
            }
        }
        public void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            lines.Add(new LineEntry(){color = color, from = from, to = to});
        }
        public void DrawText(Vector3 pos, string text, Color color)
        {
            handles.Add(new HandleEntry(){color = color, pos = pos, text = text});
        }
        public void DrawPlane(Plane plane, Color color)
        {
            planes.Add(new PlaneEntry(){color =  color, plane = plane});
        }
        private struct LineEntry
        {
            public Color color;
            public Vector3 from;
            public Vector3 to;
        }

        private struct HandleEntry
        {
            public Color color;
            public string text;
            public Vector3 pos;
        }

        private struct PlaneEntry
        {
            public Color color;
            public Plane plane;
        }
    }
}