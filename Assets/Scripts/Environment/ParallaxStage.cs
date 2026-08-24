using System.Collections.Generic;
using UnityEngine;

namespace Nestlabs.Environment
{
    // A background theme that becomes active once the player climbs past
    // `heightThreshold`. Add stages to ParallaxController in ascending
    // threshold order.
    [System.Serializable]
    public class ParallaxStage
    {
        [Tooltip("Inspector label only, e.g. 'Forest', 'Cave', 'Sky'.")]
        public string label = "Stage";

        [Tooltip("Player Y position at which this stage becomes active.")]
        public float heightThreshold = 20f;

        [Tooltip("One sprite per layer, in the same order as ParallaxController's Layers list.")]
        public List<Sprite> layerSprites = new();
    }
}
