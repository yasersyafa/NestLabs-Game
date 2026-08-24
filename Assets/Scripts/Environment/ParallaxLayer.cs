using UnityEngine;

namespace Nestlabs.Environment
{
    // One scrolling background layer. Two tiles (A/B) are alternated to create
    // an infinite vertical loop as the player climbs.
    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("Inspector label only, e.g. 'Sky', 'Mountains', 'Near Cliffs'.")]
        public string label = "Layer";

        [Tooltip("Two SpriteRenderers with identical sprites, stacked directly on top of each other.")]
        public SpriteRenderer tileA;
        public SpriteRenderer tileB;

        [Tooltip("0 = static/far background, 1 = moves exactly with the player (near).")]
        [Range(0f, 1f)]
        public float scrollFactor = 0.5f;

        private float tileHeight;

        public void Initialize()
        {
            tileHeight = tileA != null ? tileA.bounds.size.y : 0f;
        }

        public void SetSprite(Sprite sprite)
        {
            if (tileA != null) tileA.sprite = sprite;
            if (tileB != null) tileB.sprite = sprite;
            tileHeight = tileA != null ? tileA.bounds.size.y : tileHeight;
        }

        public void ApplyDelta(float deltaY)
        {
            float offset = deltaY * scrollFactor;
            if (tileA != null) tileA.transform.position += new Vector3(0f, offset, 0f);
            if (tileB != null) tileB.transform.position += new Vector3(0f, offset, 0f);
        }

        public void RecycleTiles(float playerY, float recycleMargin)
        {
            if (tileA == null || tileB == null || tileHeight <= 0f) return;
            TryRecycle(tileA.transform, tileB.transform, playerY, recycleMargin);
            TryRecycle(tileB.transform, tileA.transform, playerY, recycleMargin);
        }

        private void TryRecycle(Transform tile, Transform other, float playerY, float recycleMargin)
        {
            float topEdge = tile.position.y + tileHeight * 0.5f;
            if (playerY - topEdge > recycleMargin)
            {
                tile.position = new Vector3(tile.position.x, other.position.y + tileHeight, tile.position.z);
            }
        }
    }
}
