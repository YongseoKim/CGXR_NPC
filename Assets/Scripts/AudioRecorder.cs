using Samples.Whisper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class AudioRecorder : MonoBehaviour
{
    public Button recordButton;
    public Button nextButton;
    public string flaskServerUrl = "https://cfd9-163-239-126-56.ngrok-free.app/upload";
    public AudioSource audioSource;
    private bool isRecording = false;
    private AudioClip recordedClip;
    private Image buttonImage; // ��ư�� �̹��� ������Ʈ�� ����Ͽ� ������ ����

    void Start()
    {
        recordButton.onClick.AddListener(ToggleRecording);
        nextButton.onClick.AddListener(GoToListenScene);
        buttonImage = recordButton.GetComponent<Image>(); // ��ư�� �̹��� ������Ʈ�� ������
        buttonImage.color = Color.white; // �ʱ� ��ư ������ ������� ����
    }

    void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            isRecording = true;
            recordedClip = Microphone.Start(null, false, 60, 44100); // 60�� ���� �ִ� ����
            Debug.Log("Recording started...");
            buttonImage.color = Color.red; // ���� ���� �� ��ư ������ ���������� ����
        }
        else
        {
            Debug.LogWarning("No microphone detected!");
        }
    }

    void StopRecording()
    {
        if (isRecording)
        {
            Microphone.End(null);
            isRecording = false;
            Debug.Log("Recording stopped.");
            SaveRecording();
            buttonImage.color = Color.white; // ������ ������ �� ��ư ������ �ٽ� ������� ����
        }
    }

    void SaveRecording()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "recordedAudio.wav");
        SavWav.Save(filePath, recordedClip);
        PlayerPrefs.SetString("SavedAudioPath", filePath);
        PlayerPrefs.Save();
        StartCoroutine(UploadWav(filePath));
    }

    // Flask ������ ����� ���� ���ε� �Լ�
    IEnumerator UploadWav(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);

        // List<IMultipartFormSection>�� ����Ͽ� ������ ������ �� �����͸� ����
        List<IMultipartFormSection> form = new List<IMultipartFormSection>();
        form.Add(new MultipartFormFileSection("file", fileData, "recordedAudio.wav", "audio/wav"));

        // UnityWebRequest.Post�� ����Ͽ� ������ �� �����ͷ� ����
        UnityWebRequest request = UnityWebRequest.Post(flaskServerUrl, form);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ���信�� ���͸�ũ�� ������ �����͸� �޾Ƽ� ����
            string watermarkedFilePath = Path.Combine(Application.persistentDataPath, "watermarkedAudio.wav");
            File.WriteAllBytes(watermarkedFilePath, request.downloadHandler.data);
            Debug.Log("File received successfully and saved to: " + watermarkedFilePath);
            GoToListenScene();  // ���� ���ε尡 ���������� �Ϸ�� �� �� ��ȯ
        }
        else
        {
            Debug.LogError("Error uploading audio: " + request.error);
        }
    }


    // ���� ListenScene���� ��ȯ
    void GoToListenScene()
    {
        SceneManager.LoadScene("ListenScene"); // ListenScene���� ��ȯ
    }

}
