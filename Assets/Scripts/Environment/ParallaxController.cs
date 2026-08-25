using System.Collections.Generic;
using UnityEngine;

namespace Nestlabs.Environment
{
    // Drives all background layers off the player's absolute vertical position (see
    // ParallaxLayer — no delta accumulation, no drift). Add more layers by dragging new
    // entries into `layers` in the Inspector — no code changes required.
    public class ParallaxController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private List<ParallaxLayer> layers = new();

        [Tooltip("Background themes swapped in as the player climbs. List in ascending heightThreshold order.")]
        [SerializeField] private List<ParallaxStage> stages = new();

        private int currentStageIndex = -1;

        private void Awake()
        {
            if (player == null) return;

            float initialPlayerY = player.position.y;
            foreach (var layer in layers) layer.Initialize(initialPlayerY);
            CheckStageTransition(initialPlayerY);
        }

        private void LateUpdate()
        {
            if (player == null) return;

            float currentY = player.position.y;
            foreach (var layer in layers) layer.UpdatePosition(currentY);

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
