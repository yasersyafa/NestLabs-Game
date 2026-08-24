using System.Collections.Generic;
using UnityEngine;

namespace Nestlabs.Environment
{
    // Drives all background layers off the player's vertical position.
    // Add more layers by dragging new entries into `layers` in the Inspector —
    // no code changes required.
    public class ParallaxController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private List<ParallaxLayer> layers = new();

        [Tooltip("Distance below the player a tile's top edge must fall before it recycles above the layer. Roughly the camera's visible half-height plus a safety margin.")]
        [SerializeField] private float recycleMargin = 8f;

        private float lastPlayerY;

        private void Awake()
        {
            foreach (var layer in layers) layer.Initialize();
            if (player != null) lastPlayerY = player.position.y;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            float currentY = player.position.y;
            float deltaY = currentY - lastPlayerY;
            lastPlayerY = currentY;

            foreach (var layer in layers)
            {
                layer.ApplyDelta(deltaY);
                layer.RecycleTiles(currentY, recycleMargin);
            }
        }
    }
}
