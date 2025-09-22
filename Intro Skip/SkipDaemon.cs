using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace IntroSkip
{
    internal class SkipDaemon : IInitializable, ITickable
    {
        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly IVRPlatformHelper _vrPlatformHelper;
        private readonly ISkipDisplayService _skipDisplayService;
        private AudioTimeSyncController _audioTimeSyncController;
        private readonly IReadonlyBeatmapData _readonlyBeatmapData;
        private readonly AudioTimeSyncController.InitData _initData;
        private readonly Rect _headSpaceRect = new Rect(1, 1, 2, 2);

        private float _introSkipTime = -1f;
        private float _outroSkipTime = -1f;
        private bool _skippableOutro = false;
        private bool _skippableIntro = false;
        private float _lastObjectSkipTime = -1f;

        public bool CanSkip => InIntroPhase || InOutroPhase;
        public bool InIntroPhase => (Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time < _introSkipTime) && _skippableIntro;
        public bool InOutroPhase => Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time > _lastObjectSkipTime && Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time < _outroSkipTime && _skippableOutro;
        public bool WantsToSkip => _audioTimeSyncController.state == IAudioTimeSource.State.Playing && (VRPlatformUtils.TriggerValueDefaultImplementation(XRNode.LeftHand) >= .8 || VRPlatformUtils.TriggerValueDefaultImplementation(XRNode.RightHand) >= .8 || Input.GetKey(KeyCode.I));

        public SkipDaemon(Config config, SiraLog siraLog, IVRPlatformHelper vrPlatformHelper, ISkipDisplayService skipDisplayService, AudioTimeSyncController audioTimeSyncController, IReadonlyBeatmapData readonlyBeatmapData, AudioTimeSyncController.InitData initData)
        {
            _config = config;
            _siraLog = siraLog;
            _initData = initData;
            _vrPlatformHelper = vrPlatformHelper;
            _skipDisplayService = skipDisplayService;
            _readonlyBeatmapData = readonlyBeatmapData;
            _audioTimeSyncController = audioTimeSyncController;
        }

        public void Initialize()
        {
            _skippableIntro = false;
            _skippableOutro = false;
            _introSkipTime = -1;
            _outroSkipTime = -1;
            _lastObjectSkipTime = -1;

            var beatmapDataItems = _readonlyBeatmapData.allBeatmapDataItems;
            float firstObjectTime = _initData.audioClip.length;
            float lastObjectTime = -1f;

            int objectCount = 0;

            foreach (var item in beatmapDataItems)
            {
                if (item is NoteData note || (item is ObstacleData obstacle && IsObstacleInHeadArea(obstacle)))
                {
                    objectCount++;
                    if (item.time < firstObjectTime)
                        firstObjectTime = item.time;
                    if (item.time > lastObjectTime)
                        lastObjectTime = item.time;
                }
            }

            if (objectCount == 0)
                return;

            if (firstObjectTime > 5f)
            {
                _skippableIntro = _config.AllowIntroSkip;
                _introSkipTime = firstObjectTime - 2f;
            }
            if ((_initData.audioClip.length - lastObjectTime) >= 5f)
            {
                _skippableOutro = _config.AllowOutroSkip;
                _outroSkipTime = _initData.audioClip.length - 1.5f;
                _lastObjectSkipTime = lastObjectTime + 0.5f;
            }
            _siraLog.Debug($"Skippable Intro: {_skippableIntro} | Skippable Outro: {_skippableOutro}");
            _siraLog.Debug($"First Object Time: {firstObjectTime} | Last Object Time: {lastObjectTime}");
            _siraLog.Debug($"Intro Skip Time: {_introSkipTime} | Outro Skip Time: {_outroSkipTime}");
        }

        public void Tick()
        {
            if (CanSkip)
            {
                if (!_skipDisplayService.Active)
                    _skipDisplayService.Show();

                if (WantsToSkip)
                {
                    _vrPlatformHelper.TriggerHapticPulse(XRNode.LeftHand, 0.1f, 0.2f, 1);
                    _vrPlatformHelper.TriggerHapticPulse(XRNode.RightHand, 0.1f, 0.2f, 1);
                    if (InIntroPhase)
                    {
                        Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time = _introSkipTime;
                        _skippableIntro = false;
                    }
                    else if (InOutroPhase)
                    {
                        Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time = _outroSkipTime;
                        _skippableOutro = false;
                    }
                }
            }
            else if (_skipDisplayService.Active && !CanSkip)
            {
                _skipDisplayService.Hide();
                return;
            }
        }

        private bool IsObstacleInHeadArea(ObstacleData data)
        {
            var dataRect = new Rect(data.lineIndex, (int)data.lineLayer, data.width, data.height);
            return _headSpaceRect.Overlaps(dataRect);
        }
    }
}