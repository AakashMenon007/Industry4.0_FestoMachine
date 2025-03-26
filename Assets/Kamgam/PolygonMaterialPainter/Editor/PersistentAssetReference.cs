using UnityEditor;

namespace Kamgam.PolygonMaterialPainter
{
    public class PersistentAssetReference<T> where T : UnityEngine.Object
    {
        public static char Delimiter = '#';
        public string StorageKey = null;

        public T Asset;

        public PersistentAssetReference(T asset)
        {
            Asset = asset;
        }

        public PersistentAssetReference(string storageKey, T asset)
        {
            StorageKey = storageKey;
            Asset = asset;
        }

        public PersistentAssetReference(string storageKey, string data)
        {
            StorageKey = storageKey;
            Deserialize(data);
        }

        public string Serialize()
        {
            var path = AssetDatabase.GetAssetPath(Asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            return guid;
        }

        public void Deserialize(string data)
        {
            var guid = data;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Asset = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public void Save()
        {
            EditorPrefs.SetString(StorageKey, Serialize());
        }

        public void Load()
        {
            var data = EditorPrefs.GetString(StorageKey, "");
            Deserialize(data);
        }

        public void Clear()
        {
            EditorPrefs.DeleteKey(StorageKey);
        }
    }
}
