using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Story.EditorTools
{
    /// <summary>
    /// Доступ редактора к каталогам ключей сюжета (StoryKeyCatalogSO): поиск ключа, списки ключей
    /// по каталогам для выпадающего меню, добавление нового ключа в выбранный каталог.
    /// Каталогов может быть несколько (например, по главам) - в меню они идут отдельными подменю.
    /// </summary>
    public static class StoryKeyCatalogUtility
    {
        public const string DefaultCatalogPath = "Assets/Game/SO/Story/StoryKeys.asset";

        private static List<StoryKeyCatalogSO> s_Catalogs;

        static StoryKeyCatalogUtility()
        {
            EditorApplication.projectChanged += () => s_Catalogs = null;
        }

        /// <summary>
        /// Все каталоги проекта, по имени.
        /// </summary>
        public static IReadOnlyList<StoryKeyCatalogSO> Catalogs
        {
            get
            {
                if (s_Catalogs == null)
                {
                    s_Catalogs = new List<StoryKeyCatalogSO>();

                    foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(StoryKeyCatalogSO)))
                    {
                        var catalog = AssetDatabase.LoadAssetAtPath<StoryKeyCatalogSO>(AssetDatabase.GUIDToAssetPath(guid));

                        if (catalog != null)
                        {
                            s_Catalogs.Add(catalog);
                        }
                    }

                    s_Catalogs.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
                }

                s_Catalogs.RemoveAll(catalog => catalog == null);
                return s_Catalogs;
            }
        }

        public static StoryKeyCatalogSO.Entry Find(string key)
        {
            foreach (var catalog in Catalogs)
            {
                var entry = catalog.Find(key);

                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Каталоги, в которых есть ключ. Больше одного - один и тот же ключ объявлен в разных главах.
        /// </summary>
        public static List<StoryKeyCatalogSO> FindCatalogs(string key)
        {
            var result = new List<StoryKeyCatalogSO>();

            if (string.IsNullOrEmpty(key))
            {
                return result;
            }

            foreach (var catalog in Catalogs)
            {
                if (catalog.Find(key) != null)
                {
                    result.Add(catalog);
                }
            }

            return result;
        }

        /// <summary>
        /// Ключи каталога нужного вида.
        /// </summary>
        public static IEnumerable<StoryKeyCatalogSO.Entry> GetEntries(StoryKeyCatalogSO catalog, StoryKeyKind kind)
        {
            foreach (var entry in catalog.Entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key) && (kind == StoryKeyKind.Any || entry.Kind == kind))
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Имя каталога в меню. Одноимённые каталоги из разных папок различаются папкой.
        /// </summary>
        public static string GetDisplayName(StoryKeyCatalogSO catalog)
        {
            int sameNameCount = 0;

            foreach (var other in Catalogs)
            {
                if (other.name == catalog.name)
                {
                    sameNameCount++;
                }
            }

            if (sameNameCount <= 1)
            {
                return catalog.name;
            }

            var folder = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(catalog)));
            return $"{catalog.name} ({folder})";
        }

        /// <summary>
        /// Добавляет ключ в первый каталог (или создаёт каталог по умолчанию), если его нет ни в одном каталоге.
        /// </summary>
        public static void AddKey(string key, StoryKeyKind kind, string description = null)
        {
            if (string.IsNullOrEmpty(key) || Find(key) != null)
            {
                return;
            }

            AddKey(Catalogs.Count > 0 ? Catalogs[0] : CreateDefaultCatalog(), key, kind, description);
        }

        /// <summary>
        /// Добавляет ключ в конкретный каталог.
        /// </summary>
        public static void AddKey(StoryKeyCatalogSO catalog, string key, StoryKeyKind kind, string description = null)
        {
            if (catalog == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            Undo.RecordObject(catalog, "Add Story Key");

            if (catalog.Add(key, kind, description))
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }

        public static StoryKeyCatalogSO CreateDefaultCatalog()
        {
            var folder = Path.GetDirectoryName(DefaultCatalogPath)?.Replace('\\', '/');

            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            var path = AssetDatabase.GenerateUniqueAssetPath(DefaultCatalogPath);
            var catalog = ScriptableObject.CreateInstance<StoryKeyCatalogSO>();
            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            s_Catalogs = null;

            Debug.Log($"[Story] Создан каталог ключей сюжета: {path}", catalog);
            return catalog;
        }
    }
}
