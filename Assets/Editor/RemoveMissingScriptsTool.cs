using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WordPuzzle.EditorTools
{
    /// <summary>
    /// Task 38 follow-up — one-shot cleanup for the pre-existing "The referenced script (Unknown) on this
    /// Behaviour is missing!" console errors: these are MonoBehaviour component slots whose backing script
    /// was deleted (a dangling GUID reference). <see cref="GameObjectUtility.RemoveMonoBehavioursWithMissingScript"/>
    /// only removes components whose script is NULL, so it can NEVER remove a working component — it's safe.
    /// Run from the menu, then SAVE the scene to persist the cleanup.
    /// </summary>
    public static class RemoveMissingScriptsTool
    {
        [MenuItem("Tools/Cleanup/Remove Missing Scripts In Open Scenes")]
        public static void RemoveInOpenScenes()
        {
            int totalRemoved = 0, objectsAffected = 0;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                int sceneRemoved = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        if (n > 0) { sceneRemoved += n; objectsAffected++; }
                    }
                }
                if (sceneRemoved > 0) EditorSceneManager.MarkSceneDirty(scene);
                totalRemoved += sceneRemoved;
            }
            Debug.Log($"[Cleanup] Removed {totalRemoved} missing-script component(s) from {objectsAffected} " +
                      $"GameObject(s) across open scenes. SAVE the scene to persist.");
        }
    }
}
