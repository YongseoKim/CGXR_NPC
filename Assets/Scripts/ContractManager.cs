using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContractManager : MonoBehaviour
{
    public Button[] buttons; // �����ϴ� ��ư
    public Color defaultColor = Color.white; // �⺻ ����
    public Color clickedColor = Color.green; // Ŭ�� �� ����

    private Image[] buttonImages;
    private bool[] isClicked; // �� ��ư�� ���¸� ���� (�ʷϻ����� ����)

    void Start()
    {
        // �� ��ư �⺻ �̹��� ������Ʈ ����
        buttonImages = new Image[buttons.Length];

        // �� ��ư Ŭ�� ���� ���� �迭
        isClicked = new bool[buttons.Length];

        // �� ��ư �⺻ ���� ���� �� Ŭ�� �̺�Ʈ ���
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttonImages[i] = buttons[i].GetComponent<Image>();
            buttonImages[i].color = defaultColor; // �⺻ ���� ����
            isClicked[i] = false; // ó���� Ŭ������ ���� ����

            // ��ư Ŭ�� �̺�Ʈ�� ���� �ٸ� �ε����� ���� �޼ҵ� ����
            buttons[i].onClick.AddListener(() => OnButtonClick(index));
        }
    }

    void OnButtonClick(int index)
    {
        // ��ư�� �̹� Ŭ���Ǿ� �ʷϻ��̸� �⺻ ��������, �ƴϸ� �ʷϻ����� ����
        if (isClicked[index])
        {
            buttonImages[index].color = defaultColor;
            isClicked[index] = false;
        }
        else
        {
            buttonImages[index].color = clickedColor;
            isClicked[index] = true;
        }
    }
}
