using System;
using UnityEngine;

namespace TrashRoyale.UI
{
    /// <summary>
    /// Lightweight horizontal-swipe gesture detector for the main menu.
    /// Listens to global input (touches + mouse) so it works across the
    /// whole screen — including over buttons — without having to wrap
    /// the existing UI hierarchy.
    ///
    /// Unity's UGUI Button does not consume pointer-up events that drag
    /// off the button, so a true swipe never triggers the button's
    /// click while a stationary tap (no horizontal movement above the
    /// threshold) leaves the button click untouched. The two systems
    /// coexist cleanly.
    /// </summary>
    public class MenuSwipeHandler : MonoBehaviour
    {
        public Action OnSwipeLeft;   // finger goes ← (next-tab feeling)
        public Action OnSwipeRight;  // finger goes → (prev-tab feeling)

        // Minimum horizontal travel relative to screen width to count as
        // a swipe. 18% of width feels right on phones — small enough to
        // be discoverable, large enough not to fire on jittery taps.
        const float MinDistanceFraction = 0.18f;
        // Horizontal must dominate vertical by this much, otherwise the
        // gesture is treated as a vertical scroll attempt and ignored.
        const float HorizontalDominance = 1.5f;

        Vector2 _startPos;
        bool _tracking;

        void Update()
        {
            // Touch first (mobile is the primary target).
            if (Input.touchSupported && Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) Begin(t.position);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) End(t.position);
                return;
            }
            // Editor / desktop builds — mirror with mouse so the
            // gesture is testable without a touchscreen.
            if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) End(Input.mousePosition);
        }

        void Begin(Vector2 pos)
        {
            _startPos = pos;
            _tracking = true;
        }

        void End(Vector2 pos)
        {
            if (!_tracking) return;
            _tracking = false;
            var delta = pos - _startPos;
            float minDist = Mathf.Max(40f, Screen.width * MinDistanceFraction);
            if (Mathf.Abs(delta.x) < minDist) return;
            if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * HorizontalDominance) return;
            if (delta.x < 0) OnSwipeLeft?.Invoke();
            else OnSwipeRight?.Invoke();
        }
    }
}
