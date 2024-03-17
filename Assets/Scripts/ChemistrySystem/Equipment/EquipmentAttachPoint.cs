using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentAttachPoint : MonoBehaviour
{
    [SerializeField]
    Equipment equipment;

    private void Start()
    {
        if (equipment is null)
            equipment = transform.parent.GetComponent<Equipment>();
    }

    private void OnTriggerEnter(Collider other)
    {
        equipment.OnEquipmentTriggerEnter(other);
    }
    private void OnTriggerExit(Collider other)
    {
        equipment.OnEquipmentTriggerExit(other);
    }
}
