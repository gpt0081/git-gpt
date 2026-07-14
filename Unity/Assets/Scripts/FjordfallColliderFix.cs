using System.Collections;
using UnityEngine;

namespace Fjordfall
{
    /// <summary>Safety net for Unity primitive colliders during the first manual-test build.</summary>
    internal sealed class FjordfallColliderFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            new GameObject("Fjordfall Collider Fix").AddComponent<FjordfallColliderFix>();
        }

        private IEnumerator Start()
        {
            yield return null;
            GameObject top = GameObject.Find("Island Top");
            if (top != null)
            {
                foreach (Collider collider in top.GetComponents<Collider>())
                {
                    if (!(collider is MeshCollider)) collider.enabled = false;
                }
            }
            Destroy(gameObject);
        }
    }
}
