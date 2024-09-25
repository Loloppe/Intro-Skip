using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using System;
using Zenject;

namespace IntroSkip.UI
{
    internal class SettingsHost : IInitializable, IDisposable
    {
        private readonly Config _config;

        [UIValue("intro-skip-toggle")]
        public bool IntroSkipToggle
        {
            get => _config.AllowIntroSkip;
            set => _config.AllowIntroSkip = value;
        }

        [UIValue("outro-skip-toggle")]
        public bool OutroSkipToggle
        {
            get => _config.AllowOutroSkip;
            set => _config.AllowOutroSkip = value;
        }

        public SettingsHost(Config config)
        {
            _config = config;
        }

        public void Initialize()
        {
            GameplaySetup.Instance.AddTab("Intro Skip", "IntroSkip.UI.modifier-ui.bsml", this);
        }

        public void Dispose()
        {
            GameplaySetup.Instance.RemoveTab("Intro Skip");
        }

        [UIAction("set-intro-skip-toggle")]
        protected void SetIntro(bool value) => IntroSkipToggle = value;

        [UIAction("set-outro-skip-toggle")]
        protected void SetOutro(bool value) => OutroSkipToggle = value;
    }
}