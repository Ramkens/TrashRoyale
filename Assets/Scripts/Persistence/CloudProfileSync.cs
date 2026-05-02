using UnityEngine;
using TrashRoyale.Net;

namespace TrashRoyale.Persistence
{
    /// <summary>
    /// Best-effort cloud sync for <see cref="PlayerProfile"/>. Lives as
    /// a hidden, persistent <see cref="MonoBehaviour"/> so it can run
    /// coroutines without coupling save() to a specific scene's
    /// component.
    ///
    /// Behavior:
    ///   * Push debounced: <see cref="QueuePush"/> coalesces multiple
    ///     saves in a 1-second window into a single HTTP request to
    ///     /profile.
    ///   * Skipped when not logged in or in <see cref="AuthClient.OfflineMode"/>.
    ///   * Failure is silent — local <c>PlayerPrefs</c> is canon. If
    ///     the request fails the next save will retry.
    ///
    /// Pull happens explicitly on login (LoginBootstrap calls
    /// <see cref="PullAndOverwriteLocal"/>).
    /// </summary>
    public class CloudProfileSync : MonoBehaviour
    {
        static CloudProfileSync _instance;
        string _pendingJson;
        bool _flushScheduled;
        const float DebounceSeconds = 1.0f;

        public static void EnsureBooted()
        {
            if (_instance != null) return;
            var go = new GameObject("CloudProfileSync");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CloudProfileSync>();
        }

        public static void QueuePush(string json)
        {
            EnsureBooted();
            if (!AuthClient.IsLoggedIn) return;
            if (AuthClient.OfflineMode) return;
            _instance._pendingJson = json;
            if (!_instance._flushScheduled)
            {
                _instance._flushScheduled = true;
                _instance.StartCoroutine(_instance.DebouncedFlush());
            }
        }

        System.Collections.IEnumerator DebouncedFlush()
        {
            yield return new WaitForSeconds(DebounceSeconds);
            _flushScheduled = false;
            var snapshot = _pendingJson;
            _pendingJson = null;
            if (string.IsNullOrEmpty(snapshot)) yield break;
            yield return AuthClient.PushProfile(snapshot, r =>
            {
                if (!r.ok) Debug.LogWarning("[CloudProfileSync] push failed: " + r.error);
            });
        }

        /// <summary>
        /// Called on login. Pulls the cloud profile if any, overwrites
        /// the local one. Caller hands a runner to drive the coroutine
        /// (so this works from non-MonoBehaviour callers via the
        /// singleton instance below).
        /// </summary>
        public static void PullAndOverwriteLocal(System.Action<bool> done)
        {
            EnsureBooted();
            _instance.StartCoroutine(_instance.DoPull(done));
        }

        System.Collections.IEnumerator DoPull(System.Action<bool> done)
        {
            yield return AuthClient.FetchProfile(r =>
            {
                if (!r.ok || string.IsNullOrEmpty(r.profileJson))
                {
                    done?.Invoke(false);
                    return;
                }
                // Overwrite local. We don't need to merge: the server
                // copy is authoritative for newly-logged-in clients.
                PlayerPrefs.SetString("trashroyale.profile", r.profileJson);
                PlayerPrefs.Save();
                done?.Invoke(true);
            });
        }
    }
}
