using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentBoard : MonoBehaviour
{
    public bool isActive = true;
    public Transform generationPoint;

    [HideInInspector]
    public int hoveredBoardNum = 0;
    // Start is called before the first frame update

    private void Awake()
    {
        ReactionConfig.init();
    }

    void Start()
    {
        //ReactionConfig.init();    // ȫ�ֳ�ʼ��.

        // �޸�����ӵĽű�. ��ʵ����Ӧ�ø���ReactionConfig�ļ��ؽ����̬�����ģ����������ֱ�Ӵ����������ж���. ����ֱ���ڶ��ӵĽű��������. ��������ֻ���ռ�gameobject.
    }

    public void SwitchActivation()
    {
        isActive = !isActive;
        int cnt = transform.childCount;
        // �ҷ�����ǰ�����������ĳ��Զ�ʧ���ˣ�����nullexception.
        // ���������ͼ��start����ǰ�����������������(gameobject or SingleEquipmentBoard[])�����ᱣ��NullException
        for (int i = 0; i < cnt; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }
}
