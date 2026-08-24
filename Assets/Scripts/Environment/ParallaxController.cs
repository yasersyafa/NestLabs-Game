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

        [Tooltip("Background themes swapped in as the player climbs. List in ascending heightThreshold order.")]
        [SerializeField] private List<ParallaxStage> stages = new();

        private float lastPlayerY;
        private int currentStageIndex = -1;

        private void Awake()
        {
            foreach (var layer in layers) layer.Initialize();
            if (player != null) lastPlayerY = player.position.y;
            CheckStageTransition(lastPlayerY);
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

            CheckStageTransition(currentY);
        }

        private void CheckStageTransition(float playerY)
        {
            int newStageIndex = currentStageIndex;
            for (int i = 0; i < stages.Count; i++)
            {
                if (playerY >= stages[i].heightThreshold) newStageIndex = i;
            }

            if (newStageIndex == currentStageIndex || newStageIndex < 0) return;

            var stage = stages[newStageIndex];
            int count = Mathf.Min(layers.Count, stage.layerSprites.Count);
            for (int i = 0; i < count; i++)
            {
                layers[i].SetSprite(stage.layerSprites[i]);
            }

            currentStageIndex = newStageIndex;
        }
    }
}
