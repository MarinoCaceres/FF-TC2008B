using UnityEngine;

namespace SharkTank.Simulator
{
    /// <summary>
    /// Plays and pauses the background tracks for player and judge turns.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BGMManager : MonoBehaviour
    {
        private enum TrackMode
        {
            Player,
            Judges
        }

        [Header("Background music")]
        [Tooltip("Looping background track used while the player is typing.")]
        [SerializeField] private AudioClip playerTypingTrack;
        [Tooltip("Looping background track used while the judges are speaking.")]
        [SerializeField] private AudioClip judgeSpeakingTrack;

        [Header("Track volumes (0 = mute, 1 = full volume)")]
        [Range(0f, 1f)]
        [SerializeField] private float playerTypingVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeSpeakingVolume = 1f;

        private AudioSource playerTypingSource;
        private AudioSource judgeSpeakingSource;
        private TrackMode activeTrackMode;
        private bool playerTrackHasStarted;
        private bool judgeTrackHasStarted;

        private void Awake()
        {
            playerTypingSource = CreateMusicSource(playerTypingTrack);
            judgeSpeakingSource = CreateMusicSource(judgeSpeakingTrack);
            ApplyVolumes();
        }

        private void Update()
        {
            ApplyVolumes();
        }

        private void Start()
        {
            if (playerTypingTrack == null || judgeSpeakingTrack == null)
            {
                Debug.LogWarning("Assign both background music tracks on BGMManager.", this);
            }
        }

        private void OnEnable()
        {
            if (playerTrackHasStarted || judgeTrackHasStarted)
            {
                SetActiveTrack(activeTrackMode);
            }
        }

        private void OnDisable()
        {
            PauseMusicSource(playerTypingSource);
            PauseMusicSource(judgeSpeakingSource);
        }

        /// <summary>
        /// Resumes the player's track, preserving its playback position.
        /// </summary>
        public void PlayPlayerTrack()
        {
            SetActiveTrack(TrackMode.Player);
        }

        /// <summary>
        /// Resumes the judges' track, preserving its playback position.
        /// </summary>
        public void PlayJudgeTrack()
        {
            SetActiveTrack(TrackMode.Judges);
        }

        private AudioSource CreateMusicSource(AudioClip track)
        {
            if (track == null)
            {
                return null;
            }

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = track;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplyVolumes()
        {
            if (playerTypingSource != null)
            {
                playerTypingSource.volume = playerTypingVolume;
            }

            if (judgeSpeakingSource != null)
            {
                judgeSpeakingSource.volume = judgeSpeakingVolume;
            }
        }

        private void SetActiveTrack(TrackMode mode)
        {
            activeTrackMode = mode;
            if (mode == TrackMode.Player)
            {
                PauseMusicSource(judgeSpeakingSource);
                ResumeMusicSource(playerTypingSource, ref playerTrackHasStarted);
            }
            else
            {
                PauseMusicSource(playerTypingSource);
                ResumeMusicSource(judgeSpeakingSource, ref judgeTrackHasStarted);
            }
        }

        private static void ResumeMusicSource(AudioSource source, ref bool hasStarted)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            if (hasStarted)
            {
                if (!source.isPlaying)
                {
                    source.UnPause();
                }

                return;
            }

            source.Play();
            hasStarted = true;
        }

        private static void PauseMusicSource(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }
        }
    }
}
