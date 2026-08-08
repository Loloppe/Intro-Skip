using SiraUtil.Logging;
using System.Collections.Generic;
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

        private struct IntermissionSegment
        {
            public float StartTime;
            public float EndTime;
        }

        private float _introSkipTime = -1f;
        private float _outroSkipTime = -1f;
        private bool _skippableOutro = false;
        private bool _skippableIntermission = false;
        private bool _skippableIntro = false;
        private float _lastObjectSkipTime = -1f;

        private List<IntermissionSegment> _intermissions = new List<IntermissionSegment>();

        public bool CanSkip => InIntroPhase || InIntermissionPhase || InOutroPhase;
        public bool InIntroPhase => (Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time < _introSkipTime) && _skippableIntro;
        public bool InIntermissionPhase
        {
            get
            {
                if (!_skippableIntermission) return false;

                float time = Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time;
                foreach (var intermission in _intermissions)
                {
                    if (time > intermission.StartTime && time < intermission.EndTime)
                        return true;
                }
                return false;
            }
        }
        public bool InOutroPhase => Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time > _lastObjectSkipTime && Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time < _outroSkipTime && _skippableOutro;
        public bool WantsToSkip => _audioTimeSyncController.state == IAudioTimeSource.State.Playing && (_vrPlatformHelper.GetTriggerValue(XRNode.LeftHand) >= .8f || _vrPlatformHelper.GetTriggerValue(XRNode.RightHand) >= .8f || Input.GetKey(KeyCode.I));

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
            _skippableIntermission = false;
            _skippableOutro = false;
            _introSkipTime = -1;
            _outroSkipTime = -1;
            _lastObjectSkipTime = -1;
            _intermissions.Clear();

            var beatmapDataItems = _readonlyBeatmapData.allBeatmapDataItems;
            float firstObjectTime = _initData.audioClip.length;
            float lastObjectTime = -1f;
            float previousObjectTime = -1f;

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

                    if (previousObjectTime != -1f)
                    {
                        float gap = item.time - previousObjectTime;
                        if (gap >= 5f)
                        {
                            _intermissions.Add(new IntermissionSegment
                            {
                                StartTime = previousObjectTime + 0.5f,
                                EndTime = item.time - 2f
                            });
                        }
                    }
                    previousObjectTime = item.time;
                }
            }

            if (objectCount == 0)
                return;

            if (firstObjectTime > 5f)
            {
                _skippableIntro = _config.AllowIntroSkip;
                _introSkipTime = firstObjectTime - 2f;
            }

            if (_intermissions.Count > 0)
            {
                _skippableIntermission = _config.AllowIntermissionSkip;
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
                    else if (InIntermissionPhase)
                    {
                        float time = Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time;
                        foreach (var intermission in _intermissions)
                        {
                            if (time > intermission.StartTime && time < intermission.EndTime)
                            {
                                Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time = intermission.EndTime;
                                break;
                            }
                        }
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