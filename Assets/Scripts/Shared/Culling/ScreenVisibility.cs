namespace NestLabs.Shared.Culling
{
    // Pure math, no UnityEngine.Object dependency, so it's directly EditMode testable without a
    // scene.
    public static class ScreenVisibility
    {
        // Hysteresis: while already visible, an instance must clear the view band by `margin`
        // before it's allowed to flip invisible; while already invisible, it only needs to touch
        // the raw band to flip visible. This is what stops something like SwingObstacle's own
        // swing excursion (independent of camera movement) from flickering visibility every cycle
        // when the raw boundary happens to sit inside its arc.
        public static bool IsInView(
            float minEdgeY, float maxEdgeY, float viewMinY, float viewMaxY, float margin, bool currentlyVisible)
        {
            float loY = currentlyVisible ? viewMinY - margin : viewMinY;
            float hiY = currentlyVisible ? viewMaxY + margin : viewMaxY;
            return maxEdgeY >= loY && minEdgeY <= hiY;
        }

        // Convenience for point-sized instances (obstacles, nodes) vs. the span form above (wall
        // segments, which are tall enough that a center-only check would cull them while still
        // partly on screen).
        public static bool IsInView(float y, float viewMinY, float viewMaxY, float margin, bool currentlyVisible) =>
            IsInView(y, y, viewMinY, viewMaxY, margin, currentlyVisible);
    }
}
