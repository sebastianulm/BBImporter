using System;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BBImporter
{
    public class BBModelImportMerged : IBBMeshImporter
    {
        private readonly Vector2 resolution;
        private readonly bool filterHidden;
        private readonly string ignoreName;
        private readonly BBMeshParser meshParser;
        public BBModelImportMerged(in Vector2 resolution, bool filterHidden, string ignoreName, List<Material> material)
        {
            this.resolution = resolution;
            this.filterHidden = filterHidden;
            this.ignoreName = ignoreName;
            this.meshParser = new BBMeshParser(material, resolution);
        }
        public void ParseOutline(AssetImportContext ctx, JObject file)
        {
            ParseRecursive(file["outliner"], file);
            var guid = file["model_identifier"]?.Value<string>();
            var name = file["name"].Value<string>();
            if (string.IsNullOrEmpty(guid))
                guid = name;
            var go = meshParser.BakeMesh(ctx, name, guid, Vector3.zero);
            ctx.AddObjectToAsset(guid, go);
            ctx.SetMainObject(go);
        }
        private void ParseRecursive(JToken currentGroup, JObject file)
        {
            foreach (var entry in currentGroup)
            {
                switch (entry.Type)
                {
                    case JTokenType.String:
                        var guid = entry.Value<string>();
                        var element = file["elements"].First(x => x.Value<string>("uuid") == guid);
                        if (element["visibility"]?.Value<bool>() == false && filterHidden)
                            continue;
                        if (element["name"]?.Value<string>().Equals(ignoreName, StringComparison.InvariantCultureIgnoreCase) == true)
                            continue;
                        ParseElement(element);
                        break;
                    case JTokenType.Object:
                        //TODO: Handle visible = false here
                        ParseRecursive(entry["children"], file);
                        break;
                    default:
                        Debug.Log("Unhandled type " + entry.Type);
                        break;
                }
            }
        }
        private void ParseElement(JToken element)
        {
            var type = element["type"].Value<string>();
            switch (type)
            {
                case "cube":
                    meshParser.ParseCube(element);
                    break;
                case "mesh":
                    meshParser.ParseMesh(element);
                    break;
                case "locator":
                    
                    break;
            }
        }
    }
}