using NestLabs.Shared.Culling;
using NUnit.Framework;

namespace NestLabs.Tests
{
    /// <summary>
    /// Pure math, no scene dependency. The hysteresis rule under test: entering view uses the raw
    /// camera band, leaving view requires clearing the band by <c>margin</c> first - this is what
    /// stops something like a swinging obstacle's own excursion from flickering visibility every
    /// cycle when the raw boundary sits inside its arc.
    /// </summary>
    public sealed class ScreenVisibilityTests
    {
        private const float ViewMinY = 0f;
        private const float ViewMaxY = 10f;
        private const float Margin = 2f;

        [Test]
        public void PointInsideBand_IsVisible_RegardlessOfCurrentState()
        {
            Assert.IsTrue(ScreenVisibility.IsInView(5f, ViewMinY, ViewMaxY, Margin, currentlyVisible: true));
            Assert.IsTrue(ScreenVisibility.IsInView(5f, ViewMinY, ViewMaxY, Margin, currentlyVisible: false));
        }

        [Test]
        public void PointJustOutsideBand_WhileVisible_StaysVisible_UntilMarginCleared()
        {
            // 1 unit past the top edge, margin is 2 - hysteresis should hold it visible.
            Assert.IsTrue(ScreenVisibility.IsInView(11f, ViewMinY, ViewMaxY, Margin, currentlyVisible: true));
        }

        [Test]
        public void PointJustOutsideBand_WhileInvisible_StaysInvisible_NoEarlyEntry()
        {
            // Same point as above, but starting invisible - entry uses the raw band, not the margin.
            Assert.IsFalse(ScreenVisibility.IsInView(11f, ViewMinY, ViewMaxY, Margin, currentlyVisible: false));
        }

        [Test]
        public void PointPastMargin_WhileVisible_FlipsInvisible()
        {
            Assert.IsFalse(ScreenVisibility.IsInView(13f, ViewMinY, ViewMaxY, Margin, currentlyVisible: true));
        }

        [Test]
        public void Span_OverlappingBand_IsVisible_EvenWhenCenterIsOutside()
        {
            // A wall segment taller than the view: bottom edge below the view, top edge inside it.
            Assert.IsTrue(ScreenVisibility.IsInView(-5f, 1f, ViewMinY, ViewMaxY, Margin, currentlyVisible: false));
        }

        [Test]
        public void Span_EntirelyBelowBand_WhileInvisible_StaysInvisible()
        {
            Assert.IsFalse(ScreenVisibility.IsInView(-10f, -5f, ViewMinY, ViewMaxY, Margin, currentlyVisible: false));
        }
    }
}
