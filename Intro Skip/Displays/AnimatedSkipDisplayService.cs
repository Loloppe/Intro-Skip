using BeatSaberMarkupLanguage;
using System;
using TMPro;
using Tweening;
using UnityEngine;

namespace IntroSkip.Displays
{
    internal class AnimatedSkipDisplayService : ISkipDisplayService, IDisposable
    {
        private readonly TimeTweeningManager _timeTweeningManager;
        private TextMeshProUGUI? _skipPromptText;
        private GameObject? _skipPromptObject;
        private bool _enabled;

        public bool Active => _enabled;
        private bool Created => _skipPromptText != null && _skipPromptObject != null;

        public AnimatedSkipDisplayService(TimeTweeningManager timeTweeningManager)
        {
            _timeTweeningManager = timeTweeningManager;
        }

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
            _enabled = true;
            AnimateIn();
        }

        public void Hide()
        {
            if (Created)
            {
                AnimateOut();
                _enabled = false;
            }
        }

        private void AnimateIn()
        {
            if (Created)
            {
                _timeTweeningManager.KillAllTweens(_skipPromptText!);
                Tween tween = new FloatTween(_skipPromptText!.color.a, 1f, u => _skipPromptText.color = _skipPromptText.color.ColorWithAlpha(u), 0.75f, EaseType.OutQuart);
                _timeTweeningManager.AddTween(tween, _skipPromptText);
                _skipPromptObject!.SetActive(true);
            }
        }

        private void AnimateOut()
        {
            if (Created)
            {
                _timeTweeningManager.KillAllTweens(_skipPromptText!);
                Tween tween = new FloatTween(_skipPromptText!.color.a, 0f, u => _skipPromptText.color = _skipPromptText.color.ColorWithAlpha(u), 0.75f, EaseType.OutQuart);
                tween.onCompleted = tween.onKilled = delegate ()
                {
                    if (_skipPromptObject != null)
                        _skipPromptObject.SetActive(false);
                };
                _timeTweeningManager.AddTween(tween, _skipPromptText);
            }
        }

        public void Dispose()
        {
            if (_skipPromptText != null)
                _timeTweeningManager.KillAllTweens(_skipPromptText);
        }
    }
}
