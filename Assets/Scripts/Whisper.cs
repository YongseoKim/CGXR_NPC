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

            // 한글 문자 및 기타 특수 문자를 제거
            string cleanedText = System.Text.RegularExpressions.Regex.Replace(input, @"[^\u0000-\u007F]+", "");

            // ASCII로 변환하여 반환
            return cleanedText;
        }

        private string RemoveKoreanAndSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 한글과 특수 문자 제거 (공백 및 알파벳, 숫자만 남기기)
            string result = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9\s]", "");
            return result;
        }

        private void StartRecording()
        {
            if (isRecording) return;

            // 사용 가능한 마이크 장치 목록 가져오기
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

            // 마이크 장치 이름을 그대로 사용
            Debug.Log("Selected Microphone: " + selectedDevice);

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);

            if (minFreq == 0 && maxFreq == 0)
            {
                maxFreq = 44100; // 기본 주파수 설정
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

                // 녹음이 성공적으로 시작된 후에 마이크 이름에서 한글 및 특수 문자 제거
                string sanitizedDeviceName = RemoveKoreanAndSpecialCharacters(selectedDevice);
                Debug.Log("Sanitized Microphone Name: " + sanitizedDeviceName);
            }
        }


        private string NormalizeDeviceName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return deviceName;

            // UTF-8로 인코딩 후 다시 디코딩
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(deviceName);
            string utf8String = Encoding.UTF8.GetString(utf8Bytes);

            // ASCII로 변환하여 비 ASCII 문자 제거
            byte[] asciiBytes = Encoding.ASCII.GetBytes(utf8String);
            string asciiString = Encoding.ASCII.GetString(asciiBytes);

            // 빈 문자열이 반환되면 원래 문자열을 반환
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

            // 녹음된 AudioClip을 WAV 파일로 저장
            byte[] data = SaveWav.Save(fileName, clip);

            // Whisper API 요청
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
