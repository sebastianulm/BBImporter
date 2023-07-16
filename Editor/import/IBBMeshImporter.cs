using Newtonsoft.Json.Linq;
using UnityEditor.AssetImporters;

namespace BBImporter
{
    public interface IBBMeshImporter
    {
        void ParseOutline(AssetImportContext ctx, JObject file);

    }
}