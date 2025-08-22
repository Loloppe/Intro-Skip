using BeatSaberMarkupLanguage;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace IntroSkip.Displays
{
    internal class SkipDisplayService : ISkipDisplayService
    {
        private TextMeshProUGUI? _skipPromptText;
        private GameObject? _skipPromptObject;

        private bool Created => _skipPromptText != null && _skipPromptObject != null;

        public bool Active => _skipPromptObject != null && _skipPromptObject.activeSelf;

        public void Show()
        {
            if (!Created)
            {
                _skipPromptObject = new GameObject("Intro Skip Prompt");
                _skipPromptObject.transform.position = new Vector3(-2.5f, 2.2f, 7.0f);
                _skipPromptObject.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
                _skipPromptObject.SetActive(false);

                Canvas canvas = _skipPromptObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.enabled = false;

                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;

                RectTransform canvasRect = (canvas.transform as RectTransform)!;
                canvasRect!.sizeDelta = new Vector2(100, 50);

                _skipPromptText = BeatSaberUI.CreateCurvedUIText(canvasRect, "Press Trigger To Skip");
                _skipPromptText.rectTransform.anchoredPosition = new Vector2(0, 10f);
                RectTransform textTransform = (_skipPromptText.transform as RectTransform)!;
                textTransform.SetParent(canvas.transform, false);
                textTransform.sizeDelta = new Vector2(100, 20);
                _skipPromptText.fontSize = 15f;
                canvas.enabled = true;

                _skipPromptObject.layer = 5; //UI
            }
            _skipPromptObject!.SetActive(true);
        }

        public void Hide()
        {
            if (Created)
            {
                _skipPromptObject!.SetActive(false);
            }
        }
    }
}