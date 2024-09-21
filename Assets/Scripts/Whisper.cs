using OpenAI;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;
        [SerializeField] private ChatGPT chatGPT;
        [SerializeField] private Image microphoneIcon;

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi();
        private float nextBlink = 0.0f;
        private bool blinkState = false;
        private float blinkSpeed = 1.0f;

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new Dropdown.OptionData(device));
            }
            recordButton.onClick.AddListener(ToggleRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);
#endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }

        private void ToggleRecording()
        {
            if (isRecording)
            {
                EndRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private string CleanInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // �ѱ� ���� �� ��Ÿ Ư�� ���ڸ� ����
            string cleanedText = System.Text.RegularExpressions.Regex.Replace(input, @"[^\u0000-\u007F]+", "");

            // ASCII�� ��ȯ�Ͽ� ��ȯ
            return cleanedText;
        }

        private string RemoveKoreanAndSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // �ѱ۰� Ư�� ���� ���� (���� �� ���ĺ�, ���ڸ� �����)
            string result = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9\s]", "");
            return result;
        }

        private void StartRecording()
        {
            if (isRecording) return;

            // ��� ������ ����ũ ��ġ ��� ��������
            string[] devices = Microphone.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("No microphone devices found.");
                return;
            }

            Debug.Log("Recording started.");
            isRecording = true;
            nextBlink = Time.time + (1.0f / blinkSpeed);

            var index = PlayerPrefs.GetInt("user-mic-device-index", 0);
            if (index < 0 || index >= devices.Length)
            {
                Debug.LogError("Invalid microphone index selected.");
                return;
            }

            string selectedDevice = devices[index];

            // ����ũ ��ġ �̸��� �״�� ���
            Debug.Log("Selected Microphone: " + selectedDevice);

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);

            if (minFreq == 0 && maxFreq == 0)
            {
                maxFreq = 44100; // �⺻ ���ļ� ����
            }

            try
            {
                clip = Microphone.Start(selectedDevice, false, duration, maxFreq);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to start recording with the selected microphone. Exception: " + ex.Message);
                return;
            }

            if (clip == null)
            {
                Debug.LogError("Failed to start recording with the selected microphone.");
                isRecording = false;
            }
            else
            {
                Debug.Log("Recording started with " + selectedDevice);
                isRecording = true;

                // ������ ���������� ���۵� �Ŀ� ����ũ �̸����� �ѱ� �� Ư�� ���� ����
                string sanitizedDeviceName = RemoveKoreanAndSpecialCharacters(selectedDevice);
                Debug.Log("Sanitized Microphone Name: " + sanitizedDeviceName);
            }
        }


        private string NormalizeDeviceName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return deviceName;

            // UTF-8�� ���ڵ� �� �ٽ� ���ڵ�
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(deviceName);
            string utf8String = Encoding.UTF8.GetString(utf8Bytes);

            // ASCII�� ��ȯ�Ͽ� �� ASCII ���� ����
            byte[] asciiBytes = Encoding.ASCII.GetBytes(utf8String);
            string asciiString = Encoding.ASCII.GetString(asciiBytes);

            // �� ���ڿ��� ��ȯ�Ǹ� ���� ���ڿ��� ��ȯ
            if (string.IsNullOrEmpty(asciiString))
            {
                Debug.LogWarning($"Cannot normalize the device name: {deviceName}, using original.");
                return deviceName;
            }

            return asciiString;
        }

        private async void EndRecording()
        {
            Debug.Log("Recording ended.");
            isRecording = false;

#if !UNITY_WEBGL
            Microphone.End(null);
#endif

            // ������ AudioClip�� WAV ���Ϸ� ����
            byte[] data = SaveWav.Save(fileName, clip);

            // Whisper API ��û
            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() { Data = data, Name = "audio.wav" },
                Model = "whisper-1",
                Language = "en"
            };

            var res = await openai.CreateAudioTranscription(req);

            if (!res.Equals(default(CreateAudioResponse)) && !string.IsNullOrEmpty(res.Text))
            {
                DisplayProcessedText(res.Text);

                if (chatGPT != null)
                {
                    chatGPT.ReceiveTextFromWhisper(res.Text);
                }
            }
            else
            {
                Debug.LogError("Failed to get transcription from Whisper.");
            }
        }

        private void DisplayProcessedText(string text)
        {
            if (message != null)
            {
                message.text = text;
            }
            else
            {
                Debug.LogError("UI Text component is not assigned.");
            }

            Debug.Log("Whisper processed text: " + text);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!isRecording)
                {
                    Debug.Log("Space bar pressed - starting recording.");
                    StartRecording();
                }
            }

            if (isRecording)
            {
                time += Time.deltaTime;
                BlinkMicrophoneIcon();

                if (time >= duration)
                {
                    Debug.Log($"Recording reached its duration limit of {duration} seconds.");
                    time = 0;
                    ToggleRecording();
                }
            }
        }

        private void BlinkMicrophoneIcon()
        {
            if (Time.time >= nextBlink)
            {
                blinkState = !blinkState;
                nextBlink = Time.time + (1.0f / blinkSpeed);
            }
        }
    }
}
