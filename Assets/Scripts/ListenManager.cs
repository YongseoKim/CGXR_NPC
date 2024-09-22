using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class ListenManager : MonoBehaviour
{
    public Button playButton;
    public Button yesButton;
    public Button noButton;
    public AudioSource audioSource;
    private string filePath;

    void Start()
    {
        // PlayerPrefs���� ����� ����� ���� ��� �ҷ�����
        filePath = PlayerPrefs.GetString("SavedAudioPath", "");

        if (File.Exists(filePath))
        {
            // ����� ������ �ε��ϰ� ����� �غ�
            StartCoroutine(LoadAudio(filePath));
        }
        else
        {
            Debug.LogWarning("No audio file found at: " + filePath);
        }

        playButton.onClick.AddListener(PlayAudio);

        yesButton.onClick.AddListener(GoYesScene);

        noButton.onClick.AddListener(GoNoScene);
    }

    IEnumerator LoadAudio(string path)
    {
        using (WWW www = new WWW("file://" + path))
        {
            yield return www;

            audioSource.clip = www.GetAudioClip(false, false);
            Debug.Log("Audio loaded from: " + path);
        }
    }

    void PlayAudio()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            Debug.Log("Playing audio...");
        }
        else
        {
            Debug.LogWarning("No audio clip loaded to play.");
        }
    }

    void GoYesScene()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();  // ����� ����� ���߰� �� ��ȯ
        }
        SceneManager.LoadScene("YesScene");
    }

    void GoNoScene()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();  // ����� ����� ���߰� �� ��ȯ
        }
        SceneManager.LoadScene("NoScene");
    }
}
