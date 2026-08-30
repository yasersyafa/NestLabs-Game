using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NestLabs.UI
{
    /// <summary>
    /// Menu intro beat, run once on enable:
    /// <list type="number">
    /// <item>show image 1,</item>
    /// <item>show image 2,</item>
    /// <item>activate the TapToStart object,</item>
    /// <item>on the first screen tap, deactivate TapToStart and activate the next object.</item>
    /// </list>
    ///
    /// No animation: the images are plain <see cref="GameObject"/>s toggled with SetActive in order.
    /// They start hidden so nothing flashes during <c>delayBeforeStart</c>.
    ///
    /// Self-contained: MenuScene carries no VContainer scope, so the tap is a code-built
    /// <see cref="InputAction"/> mirroring <c>TouchPlayerInput</c> (the project runs the new Input
    /// System only, <c>activeInputHandler: 1</c>).
    /// </summary>
    public sealed class MenuIntroSequence : MonoBehaviour
    {
        [Header("Step 1-2: images, shown in order")]
        [SerializeField] private GameObject image1;
        [SerializeField] private GameObject image2;
        [SerializeField] private float delayBeforeStart = 0f;
        [SerializeField] private float gapBetweenImages = 0.1f;

        [Header("Step 3-4: hand-off")]
        [SerializeField] private GameObject tapToStart;
        [SerializeField] private GameObject nextObject;

        [Header("Start button target")]
        // Loaded by name, not build index, so reordering Build Settings can't misroute it.
        [SerializeField] private string gameSceneName = "GameScene";

        private InputAction tapAction;
        private bool tapArmed;
        private bool tapped;

        private void Awake()
        {
            tapAction = new InputAction("MenuTap", InputActionType.Button);
            tapAction.AddBinding("<Touchscreen>/primaryTouch/press");
            tapAction.AddBinding("<Mouse>/leftButton");
            tapAction.performed += OnTap;
        }

        private void OnEnable()
        {
            tapAction.Enable();
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            tapAction.Disable();
        }

        private void OnDestroy()
        {
            tapAction.performed -= OnTap;
            tapAction.Dispose();
        }

        // Only latches once the sequence has armed it (step 4), so a tap during the intro is ignored.
        private void OnTap(InputAction.CallbackContext _)
        {
            if (tapArmed) tapped = true;
        }

        /// <summary>
        /// Hooked to the Start button's <c>onClick</c>. Sync load: MenuScene is tiny and carries no
        /// container, so there is nothing to stream and no state to keep alive across the swap.
        /// </summary>
        public void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private IEnumerator Run()
        {
            tapArmed = false;
            tapped = false;

            // Start state: prompt, next and both images hidden.
            if (tapToStart != null) tapToStart.SetActive(false);
            if (nextObject != null) nextObject.SetActive(false);
            if (image1 != null) image1.SetActive(false);
            if (image2 != null) image2.SetActive(false);

            if (delayBeforeStart > 0f) yield return new WaitForSecondsRealtime(delayBeforeStart);

            // 1. show image 1
            if (image1 != null) image1.SetActive(true);

            if (gapBetweenImages > 0f) yield return new WaitForSecondsRealtime(gapBetweenImages);

            // 2. show image 2
            if (image2 != null) image2.SetActive(true);

            // 3. activate TapToStart
            if (tapToStart != null) tapToStart.SetActive(true);

            // 4. first tap -> swap TapToStart for nextObject
            tapArmed = true;
            yield return new WaitUntil(() => tapped);

            if (tapToStart != null) tapToStart.SetActive(false);
            if (nextObject != null) nextObject.SetActive(true);
        }
    }
}
