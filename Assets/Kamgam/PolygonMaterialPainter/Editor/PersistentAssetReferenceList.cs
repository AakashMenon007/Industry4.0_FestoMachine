using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
    public class PersistentAssetReferenceList<T> where T : UnityEngine.Object
    {
        public static char Delimiter = '|';

        public string StorageKey = null;
        public List<PersistentAssetReference<T>> References;

        /// <summary>
        /// Flag is set to true whenever an asset is added or removed.
        /// </summary>
        public bool HasChanged = false;

        public bool AddNullValues = false;

        public PersistentAssetReferenceList(string storageKey)
        {
            StorageKey = storageKey;
            References = new List<PersistentAssetReference<T>>();
        }

        public PersistentAssetReferenceList(string storageKey, string data)
        {
            StorageKey = storageKey;
            Deserialize(data);
        }

        public PersistentAssetReferenceList()
        {
            References = new List<PersistentAssetReference<T>>();
        }

        /// <summary>
        /// Adds only if not yet contained.<br />
        /// Null values will always be added.
        /// </summary>
        /// <param name="asset"></param>
        public void AddAsset(T asset)
        {
            if (References == null)
                References = new List<PersistentAssetReference<T>>();

            bool contains = false;
            foreach (var r in References)
            {
                if (r.Asset == asset)
                {
                    contains = true;
                    break;
                }
            }
            if (!contains || (AddNullValues && asset == null))
            {
                var reference = new PersistentAssetReference<T>(asset);
                References.Add(reference);
                HasChanged = true;
            }
        }

        public void RemoveAsset(T asset)
        {
            if (References == null)
            {
                References = new List<PersistentAssetReference<T>>();
                return;
            }

            PersistentAssetReference<T> refToRemove = null;
            foreach (var r in References)
            {
                if (r.Asset == asset)
                {
                    refToRemove = r;
                    break;
                }
            }
            if (refToRemove != null)
            {
                References.Remove(refToRemove);
                HasChanged = true;
            }
        }

        public T GetAt(int index)
        {
            if (References == null || References.Count <= index)
                return default;

            if (References[index] == null)
                return default;

            return References[index].Asset;
        }

        public void SetAt(int index, T asset)
        {
            if (References == null || References.Count <= index)
                return;

            if (References[index] == null)
            {
                References[index] = new PersistentAssetReference<T>(asset);
                HasChanged = true;
            }
            
            if (References[index].Asset != asset)
            {
                References[index].Asset = asset;
                HasChanged = true;
            }
        }

        public string Serialize()
        {
            string str = "";
            bool first = true;
            foreach (var r in References)
            {
                if (!first)
                {
                    str += Delimiter;
                }
                str += r.Serialize();
                first = false;
            }
            return str;
        }

        public void Deserialize(string data)
        {
            var str = data.Split(Delimiter);

            if (References == null)
            {
                References = new List<PersistentAssetReference<T>>();
            }
            References.Clear();

            foreach (var s in str)
            {
                var r = new PersistentAssetReference<T>(null, s);
                References.Add(r);
            }
        }

        public void Save()
        {
            var serializedData = Serialize();
            EditorPrefs.SetString(StorageKey, serializedData);
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
