using System.Collections.Generic;
using SharkTank.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.Simulator
{
    /// <summary>
    /// Manages sound effects for UI clicks, judge feedback, and transitions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SFXManager : MonoBehaviour
    {
        [Header("One-shot sound effects")]
        [Tooltip("Played when any button other than HelpButton is clicked.")]
        [SerializeField] private AudioClip buttonClickSound;
        [Tooltip("Played when HelpButton is clicked.")]
        [SerializeField] private AudioClip helpButtonClickSound;
        [Tooltip("Played when a judge presents an offer.")]
        [SerializeField] private AudioClip judgeOfferSound;
        [Tooltip("Played when a judge says they are out.")]
        [SerializeField] private AudioClip judgeOutSound;
        [Tooltip("Played when an out judge begins disappearing.")]
        [SerializeField] private AudioClip judgeDisappearSound;

        [Header("Judge talking sounds")]
        [Tooltip("Short voice sound played as Judge 1's characters appear.")]
        [SerializeField] private AudioClip judgeOneTalkingSound;
        [Tooltip("Short voice sound played as Judge 2's characters appear.")]
        [SerializeField] private AudioClip judgeTwoTalkingSound;
        [Tooltip("Short voice sound played as Judge 3's characters appear.")]
        [SerializeField] private AudioClip judgeThreeTalkingSound;

        [Header("Platform rise sound")]
        [Tooltip("Looped while the player platform rises at the start of the simulation.")]
        [SerializeField] private AudioClip platformRiseSound;

        [Header("Volumes (0 = mute, 1 = full volume)")]
        [Range(0f, 1f)]
        [SerializeField] private float buttonClickVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float helpButtonClickVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeOfferVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeOutVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeDisappearVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeOneTalkingVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeTwoTalkingVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeThreeTalkingVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float platformRiseVolume = 1f;

        [Header("Speech references")]
        [SerializeField] private JudgeFeedbackSequence judgeFeedbackSequence;
        [SerializeField] private SpeechTypewriter speechTypewriter;

        private readonly List<Button> hookedButtons = new List<Button>();
        private AudioSource buttonClickSource;
        private AudioSource helpButtonClickSource;
        private AudioSource judgeOfferSource;
        private AudioSource judgeOutSource;
        private AudioSource judgeDisappearSource;
        private AudioSource judgeOneTalkingSource;
        private AudioSource judgeTwoTalkingSource;
        private AudioSource judgeThreeTalkingSource;
        private AudioSource platformRiseSource;
        private int currentSpeakingJudge = -1;

        private void Awake()
        {
            buttonClickSource = CreateAudioSource();
            helpButtonClickSource = CreateAudioSource();
            judgeOfferSource = CreateAudioSource();
            judgeOutSource = CreateAudioSource();
            judgeDisappearSource = CreateAudioSource();
            judgeOneTalkingSource = CreateAudioSource();
            judgeTwoTalkingSource = CreateAudioSource();
            judgeThreeTalkingSource = CreateAudioSource();
            platformRiseSource = CreateAudioSource();
            judgeOneTalkingSource.loop = true;
            judgeTwoTalkingSource.loop = true;
            judgeThreeTalkingSource.loop = true;
            platformRiseSource.loop = true;
            ApplyVolumes();
            if (judgeFeedbackSequence == null)
            {
                judgeFeedbackSequence = FindAnyObjectByType<JudgeFeedbackSequence>();
            }

            if (speechTypewriter == null)
            {
                speechTypewriter = FindAnyObjectByType<SpeechTypewriter>();
            }
        }

        private void Update()
        {
            ApplyVolumes();
        }

        private void OnEnable()
        {
            HookButtonSounds();

            if (judgeFeedbackSequence != null)
            {
                judgeFeedbackSequence.OnJudgeOfferPresented += PlayJudgeOfferSound;
                judgeFeedbackSequence.OnJudgeOutPresented += PlayJudgeOutSound;
                judgeFeedbackSequence.OnJudgeDisappearing += PlayJudgeDisappearSound;
                judgeFeedbackSequence.OnJudgeSpeakingStarted += HandleJudgeSpeakingStarted;
            }

            if (speechTypewriter != null)
            {
                speechTypewriter.OnTypingStarted += StartJudgeTalkingSound;
                speechTypewriter.OnFinished += StopJudgeTalkingSound;
            }
        }

        private void Start()
        {
            WarnIfMissingSound(buttonClickSound, nameof(buttonClickSound));
            WarnIfMissingSound(helpButtonClickSound, nameof(helpButtonClickSound));
            WarnIfMissingSound(judgeOfferSound, nameof(judgeOfferSound));
            WarnIfMissingSound(judgeOutSound, nameof(judgeOutSound));
            WarnIfMissingSound(judgeDisappearSound, nameof(judgeDisappearSound));
            WarnIfMissingSound(judgeOneTalkingSound, nameof(judgeOneTalkingSound));
            WarnIfMissingSound(judgeTwoTalkingSound, nameof(judgeTwoTalkingSound));
            WarnIfMissingSound(judgeThreeTalkingSound, nameof(judgeThreeTalkingSound));
            WarnIfMissingSound(platformRiseSound, nameof(platformRiseSound));

            if (judgeFeedbackSequence == null)
            {
                Debug.LogWarning("SFXManager could not find a JudgeFeedbackSequence in the scene.", this);
            }

            if (speechTypewriter == null)
            {
                Debug.LogWarning("SFXManager could not find a SpeechTypewriter in the scene.", this);
            }
        }

        private void OnDisable()
        {
            UnhookButtonSounds();

            if (judgeFeedbackSequence != null)
            {
                judgeFeedbackSequence.OnJudgeOfferPresented -= PlayJudgeOfferSound;
                judgeFeedbackSequence.OnJudgeOutPresented -= PlayJudgeOutSound;
                judgeFeedbackSequence.OnJudgeDisappearing -= PlayJudgeDisappearSound;
                judgeFeedbackSequence.OnJudgeSpeakingStarted -= HandleJudgeSpeakingStarted;
            }

            if (speechTypewriter != null)
            {
                speechTypewriter.OnTypingStarted -= StartJudgeTalkingSound;
                speechTypewriter.OnFinished -= StopJudgeTalkingSound;
            }

            StopJudgeTalkingSound();
            StopPlatformRiseSound();
        }

        private void HookButtonSounds()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                if (button.gameObject.name == "HelpButton")
                {
                    button.onClick.AddListener(PlayHelpButtonClickSound);
                }
                else
                {
                    button.onClick.AddListener(PlayButtonClickSound);
                }

                hookedButtons.Add(button);
            }
        }

        private void UnhookButtonSounds()
        {
            foreach (Button button in hookedButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(PlayButtonClickSound);
                button.onClick.RemoveListener(PlayHelpButtonClickSound);
            }

            hookedButtons.Clear();
        }

        private void PlayButtonClickSound()
        {
            PlayOneShot(buttonClickSource, buttonClickSound);
        }

        private void PlayHelpButtonClickSound()
        {
            PlayOneShot(helpButtonClickSource, helpButtonClickSound);
        }

        private void PlayJudgeOfferSound(int judgeIndex)
        {
            PlayOneShot(judgeOfferSource, judgeOfferSound);
        }

        private void PlayJudgeOutSound(int judgeIndex)
        {
            PlayOneShot(judgeOutSource, judgeOutSound);
        }

        private void PlayJudgeDisappearSound(int judgeIndex)
        {
            PlayOneShot(judgeDisappearSource, judgeDisappearSound);
        }

        private void HandleJudgeSpeakingStarted(int judgeIndex)
        {
            StopJudgeTalkingSound();
            currentSpeakingJudge = judgeIndex;
        }

        private void StartJudgeTalkingSound()
        {
            AudioSource source;
            AudioClip sound;
            switch (currentSpeakingJudge)
            {
                case 0:
                    source = judgeOneTalkingSource;
                    sound = judgeOneTalkingSound;
                    break;
                case 1:
                    source = judgeTwoTalkingSource;
                    sound = judgeTwoTalkingSound;
                    break;
                case 2:
                    source = judgeThreeTalkingSource;
                    sound = judgeThreeTalkingSound;
                    break;
                default:
                    return;
            }

            if (source != null && sound != null && !source.isPlaying)
            {
                source.clip = sound;
                source.Play();
            }
        }

        private void StopJudgeTalkingSound()
        {
            StopAudioSource(judgeOneTalkingSource);
            StopAudioSource(judgeTwoTalkingSource);
            StopAudioSource(judgeThreeTalkingSource);
        }

        public void StartPlatformRiseSound()
        {
            if (platformRiseSound == null || platformRiseSource == null)
            {
                return;
            }

            platformRiseSource.Stop();
            platformRiseSource.clip = platformRiseSound;
            platformRiseSource.loop = true;
            platformRiseSource.Play();
        }

        public void StopPlatformRiseSound()
        {
            if (platformRiseSource != null)
            {
                platformRiseSource.Stop();
                platformRiseSource.clip = null;
            }
        }

        private static void StopAudioSource(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
                source.clip = null;
            }
        }

        private AudioSource CreateAudioSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplyVolumes()
        {
            buttonClickSource.volume = buttonClickVolume;
            helpButtonClickSource.volume = helpButtonClickVolume;
            judgeOfferSource.volume = judgeOfferVolume;
            judgeOutSource.volume = judgeOutVolume;
            judgeDisappearSource.volume = judgeDisappearVolume;
            judgeOneTalkingSource.volume = judgeOneTalkingVolume;
            judgeTwoTalkingSource.volume = judgeTwoTalkingVolume;
            judgeThreeTalkingSource.volume = judgeThreeTalkingVolume;
            platformRiseSource.volume = platformRiseVolume;
        }

        private static void PlayOneShot(AudioSource source, AudioClip sound)
        {
            if (sound != null && source != null)
            {
                source.PlayOneShot(sound);
            }
        }

        private void WarnIfMissingSound(AudioClip sound, string fieldName)
        {
            if (sound == null)
            {
                Debug.LogWarning("Assign an AudioClip to SFXManager." + fieldName + ".", this);
            }
        }
    }
}
