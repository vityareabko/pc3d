// ResourceService.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public interface IResourceService
{
    T Load<T>(string path) where T : UnityEngine.Object;
    bool TryLoad<T>(string path, out T asset) where T : UnityEngine.Object;
    T[] LoadAll<T>(string folderPath) where T : UnityEngine.Object;

    Task<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : UnityEngine.Object;
    Task<T[]> LoadAllAsync<T>(string folderPath, CancellationToken ct = default) where T : UnityEngine.Object;

    GameObject Instantiate(string prefabPath, Transform parent = null, bool worldPositionStays = false);
    GameObject Instantiate(string prefabPath, Vector3 position, Quaternion rotation, Transform parent = null);

    T InstantiateSo<T>(string path) where T : ScriptableObject;

    void Unload(string path);
    void ClearCache(bool unloadUnusedAssets = false);
}

public sealed class ResourceService : IResourceService
{
    // Singleton for convenience (or inject IResourceService where you prefer)
    public static IResourceService Instance { get; } = new ResourceService();

    // Cache: path -> asset
    private readonly Dictionary<string, UnityEngine.Object> _cache = new(StringComparer.Ordinal);

    // -------- Sync API --------

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        path = Normalize(path);
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Resources path is null/empty", nameof(path));

        if (_cache.TryGetValue(path, out var cached))
            return cached as T;

        var asset = Resources.Load<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"[ResourceService] Asset not found at Resources/{path}");

        _cache[path] = asset;
        return asset;
    }

    public bool TryLoad<T>(string path, out T asset) where T : UnityEngine.Object
    {
        try
        {
            asset = Load<T>(path);
            return true;
        }
        catch
        {
            asset = null;
            return false;
        }
    }

    public T[] LoadAll<T>(string folderPath) where T : UnityEngine.Object
    {
        folderPath = Normalize(folderPath);
        var arr = Resources.LoadAll<T>(folderPath);
        // put in cache
        foreach (var a in arr)
        {
            if (a == null) continue;
            var key = MakeCacheKey(folderPath, a.name);
            _cache[key] = a;
        }
        return arr;
    }

    // -------- Async API --------

    public async Task<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : UnityEngine.Object
    {
        path = Normalize(path);
        if (_cache.TryGetValue(path, out var cached))
            return cached as T;

        var req = Resources.LoadAsync<T>(path);
        while (!req.isDone)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);
            await Task.Yield();
        }

        var asset = req.asset as T;
        if (asset == null)
            throw new InvalidOperationException($"[ResourceService] Asset not found at Resources/{path}");

        _cache[path] = asset;
        return asset;
    }

    public async Task<T[]> LoadAllAsync<T>(string folderPath, CancellationToken ct = default) where T : UnityEngine.Object
    {
        // Resources API не даёт настоящей async для LoadAll, обойдём через Task.Run + sync load
        return await Task.Run(() =>
        {
            if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();
            return LoadAll<T>(folderPath);
        }, ct);
    }

    // -------- Instantiate helpers --------

    public GameObject Instantiate(string prefabPath, Transform parent = null, bool worldPositionStays = false)
    {
        var prefab = Load<GameObject>(prefabPath);
        return UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
    }

    public GameObject Instantiate(string prefabPath, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var prefab = Load<GameObject>(prefabPath);
        return UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
    }

    /// <summary>
    /// Loads ScriptableObject and returns a runtime clone (safe to modify).
    /// </summary>
    public T InstantiateSo<T>(string path) where T : ScriptableObject
    {
        var original = Load<T>(path);
        return UnityEngine.ScriptableObject.Instantiate(original);
    }

    // -------- Cache management --------

    public void Unload(string path)
    {
        path = Normalize(path);
        if (_cache.Remove(path))
        {
            // Внимание: Resources.UnloadAsset работает только для загруженных через Resources и не для GameObject/Component
            // Для текстур/аудио/материалов — ок. Для префабов обычно не требуется.
            // Если нужно жёстко — вызови ClearCache(true) и пусть Unity сам выгрузит неиспользуемые.
        }
    }

    public async void ClearCache(bool unloadUnusedAssets = false)
    {
        _cache.Clear();
        if (unloadUnusedAssets)
        {
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }

    // -------- Internals --------

    // Normalizes "Assets/Resources/Folder/Item.asset" -> "Folder/Item"
    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        path = path.Replace('\\', '/');

        int i = path.IndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
        if (i >= 0)
            path = path[(i + "Resources/".Length)..];

        if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            path = path[..^".asset".Length];

        if (path.StartsWith("/")) path = path[1..];
        return path;
    }

    // Key helper for LoadAll caching: "folder/name"
    private static string MakeCacheKey(string folder, string name)
    {
        if (string.IsNullOrEmpty(folder)) return name;
        if (folder[^1] == '/') return folder + name;
        return folder + "/" + name;
    }
}
