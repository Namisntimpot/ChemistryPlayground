using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenableWindow : MonoBehaviour
{
    public Transform Hinges;
    public float degree = 60;
    bool opened = false;
    
    public void OpenOrCloseWindow()
    {
        if (opened)
        {
            transform.RotateAround(Hinges.position, Vector3.up, degree);
            RoomEnvironment.ventilation -= 1;
        }
        else
        {
            transform.RotateAround(Hinges.position, Vector3.up, -degree);
            RoomEnvironment.ventilation += 1;
        }
        opened = !opened;

        // �ı�ȫ�ֻ�����״̬!
    }
}
