using System.Collections.Generic;
using SharkTank.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SharkTank.Simulator
{
    /// <summary>
    /// Plays one-shot sound effects for UI clicks and judge decisions.
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

        [Header("Volumes (0 = mute, 1 = full volume)")]
        [Range(0f, 1f)]
        [SerializeField] private float buttonClickVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float helpButtonClickVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeOfferVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float judgeOutVolume = 1f;

        private readonly List<Button> hookedButtons = new List<Button>();
        private AudioSource buttonClickSource;
        private AudioSource helpButtonClickSource;
        private AudioSource judgeOfferSource;
        private AudioSource judgeOutSource;
        private JudgeFeedbackSequence judgeFeedbackSequence;

        private void Awake()
        {
            buttonClickSource = CreateAudioSource();
            helpButtonClickSource = CreateAudioSource();
            judgeOfferSource = CreateAudioSource();
            judgeOutSource = CreateAudioSource();
            ApplyVolumes();
            judgeFeedbackSequence = FindAnyObjectByType<JudgeFeedbackSequence>();
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
            }
        }

        private void Start()
        {
            WarnIfMissingSound(buttonClickSound, nameof(buttonClickSound));
            WarnIfMissingSound(helpButtonClickSound, nameof(helpButtonClickSound));
            WarnIfMissingSound(judgeOfferSound, nameof(judgeOfferSound));
            WarnIfMissingSound(judgeOutSound, nameof(judgeOutSound));

            if (judgeFeedbackSequence == null)
            {
                Debug.LogWarning("SFXManager could not find a JudgeFeedbackSequence in the scene.", this);
            }
        }

        private void OnDisable()
        {
            UnhookButtonSounds();

            if (judgeFeedbackSequence != null)
            {
                judgeFeedbackSequence.OnJudgeOfferPresented -= PlayJudgeOfferSound;
                judgeFeedbackSequence.OnJudgeOutPresented -= PlayJudgeOutSound;
            }
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
