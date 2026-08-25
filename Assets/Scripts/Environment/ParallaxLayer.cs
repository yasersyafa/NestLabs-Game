using UnityEngine;

namespace Nestlabs.Environment
{
    // One infinitely-tiled background layer. Position is recomputed from the player's absolute
    // Y each frame (never accumulated), so there is no drift to correct for — Mathf.Repeat wraps
    // the tile pair to whichever copy sits closest to the player.
    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("Inspector label only, e.g. 'Sky', 'Mountains', 'Near Cliffs'.")]
        public string label = "Layer";

        [Tooltip("Two SpriteRenderers with identical sprites, same size. Exact starting Y doesn't matter — both get repositioned every frame.")]
        public SpriteRenderer tileA;
        public SpriteRenderer tileB;

        [Tooltip("How much this layer moves on screen relative to the player. 1 = fully glued to the player (moves with them, reads as near). 0 = fixed in world space, so it slides across the screen at full speed as the player climbs (reads as far). Counter-intuitive name if you expect 'near/far' to sort the other way — it's screen-space motion, not world-space motion.")]
        [Range(0f, 1f)]
        public float scrollFactor = 0.5f;

        private float tileHeight;
        private float tileRestY;
        private float playerRestY;

        public void Initialize(float initialPlayerY)
        {
            tileHeight = tileA != null ? tileA.bounds.size.y : 0f;
            tileRestY = tileA != null ? tileA.transform.position.y : 0f;
            playerRestY = initialPlayerY;
        }

        public void SetSprite(Sprite sprite)
        {
            if (tileA != null) tileA.sprite = sprite;
            if (tileB != null) tileB.sprite = sprite;
            tileHeight = tileA != null ? tileA.bounds.size.y : tileHeight;
        }

        public void UpdatePosition(float playerY)
        {
            if (tileA == null || tileB == null || tileHeight <= 0f) return;

            float idealY = tileRestY + (playerY - playerRestY) * scrollFactor;
            float baseY = playerY - Mathf.Repeat(playerY - idealY, tileHeight);

            var posA = tileA.transform.position;
            tileA.transform.position = new Vector3(posA.x, baseY, posA.z);

            var posB = tileB.transform.position;
            tileB.transform.position = new Vector3(posB.x, baseY + tileHeight, posB.z);
        }
    }
}
