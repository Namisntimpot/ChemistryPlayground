using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ��һ�����������ŵ㣬һ���Ƚϸߡ�һ���Ƚϵͣ��Ƚϵ͵��Ǹ��������еĵ�һ��.
/// </summary>
public class SolidReagentEquipment : Equipment
{
    public string reagent_name;    //eqname �� identification_name
    [SerializeField]
    InputActionAsset actionAsset;

    Container targetContainer = null;

    // Start is called before the first frame update
    void Start()
    {
        Init();

        if(attachPoints[0].position.y > attachPoints[1].position.y)  // ����.
        {
            var tmp = attachPoints[1];
            attachPoints[1] = attachPoints[0];
            attachPoints[0] = tmp;
        }
    }
    private void OnEnable()
    {
        foreach(string map_name in new string[] { "XRI LeftHand Interaction", "XRI RightHand Interaction" })
        {
            InputAction action = actionAsset.FindActionMap(map_name).FindAction("Function Toggle");
            action.performed += OnFunctionKey;
        }
    }
    private void OnDisable()
    {
        foreach (string map_name in new string[] { "XRI LeftHand Interaction", "XRI RightHand Interaction" })
        {
            InputAction action = actionAsset.FindActionMap(map_name).FindAction("Function Toggle");
            action.performed -= OnFunctionKey;
        }
    }

    void OnFunctionKey(InputAction.CallbackContext context)
    {
        if (isHovered && attachedTo != null)
        {
            attach_from_to.Item1 = 1 - attach_from_to.Item1;
            var pos = transform.position;
            pos.y += (attachPoints[1-attach_from_to.Item1].position.y - attachPoints[attach_from_to.Item1].position.y);
            transform.position = pos;
            //CheckAttachment();

            //TODO �ѷ�Ӧ��ӵ�������.
            if (targetContainer != null)
            {
                if (attach_from_to.Item1 == 1)
                {
                    Reactant reactant = Reactant.Create_Solidity(reagent_name, 10000000, 0);
                    targetContainer.AddReactant(reactant);
                }
                else
                {
                    targetContainer.RemoveReactant(reagent_name);
                }
            }
        }
    }

    public override void OnEquipmentTriggerEnter(Collider other)  // ע��һ���޶����Ǳ��˵�colliderײ���Լ���trigger.
    {
        base.OnEquipmentTriggerEnter(other);
        if(!other.isTrigger && other.TryGetComponent(out Container container))
        {
            targetContainer = container;
        }
    }

    public override void OnEquipmentTriggerExit(Collider other)
    {
        base.OnEquipmentTriggerExit(other);
        if(!other.isTrigger && other.TryGetComponent(out Container container) && targetContainer == container)
        {
            targetContainer = null;
        }
    }
}
