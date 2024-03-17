using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Burner : Equipment
{
    [SerializeField]
    InputActionAsset inputActionAsset;

    Equipment targetEquipment;
    bool isBurning = false;

    [SerializeField]
    ParticleSystem fireSystem;

    public float fireTemprature = 900;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }
    private void OnEnable()
    {
        foreach (var actionMap in new string[] { "XRI LeftHand Interaction", "XRI RightHand Interaction" })
        {
            InputAction action = inputActionAsset.FindActionMap(actionMap).FindAction("Function Toggle");
            //action.started += switchFunctionMode;
            action.performed += switchBurningMode;
        }
    }
    private void OnDisable()
    {
        foreach (var actionMap in new string[] { "XRI LeftHand Interaction", "XRI RightHand Interaction" })
        {
            InputAction action = inputActionAsset.FindActionMap(actionMap).FindAction("Function Toggle");
            //action.started -= switchFunctionMode;
            action.performed -= switchBurningMode;
        }
    }

    void switchBurningMode(InputAction.CallbackContext context)
    {
        if (isHovered)
        {
            if (isBurning)  // 关火.
            {
                // 粒子系统关火.
                fireSystem.Stop();
                
                if(targetEquipment is not null)
                {
                    targetEquipment.env.temperature = Constant.RoomTemperature;
                }
                isBurning = false;
            }
            else
            {
                // 粒子系统开火.
                fireSystem.Play();

                if (targetEquipment is not null) {
                    Debug.Log("Heating " + targetEquipment.name);
                    targetEquipment.env.temperature = fireTemprature;   //900K.
                }
                isBurning = true;
            }
        }
    }

    public override void OnEquipmentTriggerEnter(Collider other)
    {
        base.OnEquipmentTriggerEnter(other);
        //Debug.Log(other.name + " Enter Burner");
        if(!other.isTrigger && other.gameObject != gameObject && other.TryGetComponent(out Equipment equipment) && equipment.heatable)
        {
            targetEquipment = equipment;
            if (isBurning)
                targetEquipment.env.temperature = fireTemprature;
        }
    }

    public override void OnEquipmentTriggerExit(Collider other)
    {
        base.OnEquipmentTriggerExit(other);
        //Debug.Log(other.name + " Exit Burner");
        if (!other.isTrigger && other.gameObject != gameObject && other.TryGetComponent(out Equipment equipment) && equipment == targetEquipment)
        {
            targetEquipment.env.temperature = Constant.RoomTemperature;
            targetEquipment = null;
        }
    }

}
