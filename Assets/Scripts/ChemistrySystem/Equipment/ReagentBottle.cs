using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ReagentBottle : Equipment
{
    [SerializeField]
    InputActionAsset inputActionAsset;

    bool functionMode = false;  // functionMode指倒液体模式..
    Container targetEquipment = null;    // 限制一下，只能倒入Container!

    public string identification_name;  // 药品的识别名.
    public string reactant_name;
    public Reactant.StateOfMatter state;

    Material material;

    float lastAddReactant = 0;

    [SerializeField]
    float toppleOffset = 0;

    // Start is called before the first frame update
    void Start()
    {
        Init();
        // eqenv等在这里不重要...先不管
        // 材质在singleEquipmentBoard里设置了，其实不是很好...
    }
    private void OnEnable()
    {
        foreach (var actionMap in new string[] { "XRI LeftHand Interaction", "XRI RightHand Interaction" })
        {
            InputAction action = inputActionAsset.FindActionMap(actionMap).FindAction("Function Toggle");
            //action.started += switchFunctionMode;
            action.performed += switchFunctionMode;
        }
    }
    private void OnDisable()
    {
        foreach (var actionMap in new string[] { "XRI LeftHand Interaction", "XRI RightHand Interaction" })
        {
            InputAction action = inputActionAsset.FindActionMap(actionMap).FindAction("Function Toggle");
            //action.started -= switchFunctionMode;
            action.performed -= switchFunctionMode;
        }
    }

    void switchFunctionMode(InputAction.CallbackContext context)
    {
        if (isGrabbed)
        {
            if (functionMode)
            {
                if(targetEquipment is not null)
                    targetEquipment.liquidEmitter.VolumePerSimTime = 0f;
                targetEquipment = null;
            }
            functionMode = !functionMode;
        }
    }

    public override void OnControllerSelectExit()
    {
        base.OnControllerSelectExit();
        functionMode = false;   // 强制关闭.
        if (targetEquipment is not null)
        {
            targetEquipment.liquidEmitter.VolumePerSimTime = 0f;
            targetEquipment = null;
        }
    }

    //bool attachEnterPerformed = false;
    public override void OnEquipmentTriggerEnter(Collider other)
    {
        //if (!functionMode)
        //{
        base.OnEquipmentTriggerEnter(other);
        //    attachEnterPerformed = true;
        //}
        //else
        //{
        if(functionMode)
            TargetEnter(other);
        //}
    }

    public override void OnEquipmentTriggerExit(Collider other)
    {
        //if(attachEnterPerformed)
        base.OnEquipmentTriggerExit(other);
        //attachEnterPerformed = false;
        if (functionMode)
        {
            TargetExit(other);
        }
    }

    /// <summary>一定是对方的collider撞到了自己的trigger，并且自己isGrabbed.</summary>
    void TargetEnter(Collider other)
    {
        //Debug.Log("TargetEnter(): functionMode = " + functionMode + ", other.name = " + other.name);
        if (!isGrabbed || other.isTrigger)
            return;
        if(other.TryGetComponent(out Container other_equipment))
        {
            Debug.Log("Target Enter: " + other_equipment.name);
            targetEquipment = other_equipment;
        }
    }

    void TargetExit(Collider other)
    {
        if (!isGrabbed || other.isTrigger)
            return;
        if(other.TryGetComponent(out Container other_equipment) && targetEquipment == other_equipment)
        {
            //Debug.Log("Target Exit: " + other_equipment.name);
            targetEquipment.liquidEmitter.VolumePerSimTime = 0f;
            targetEquipment = null;
        }
    }

    private void Update()
    {
        if(functionMode && targetEquipment != null)
        {
            // **好一点的做法应该是把修改liquidEmmiter的部分集成在Container中的AddReactant中，降低一点耦合..**
            if (ShouldOutFlow()) {
                targetEquipment.liquidEmitter.VolumePerSimTime = targetEquipment.flowSpeed;  // 精妙点的做法就是根据向下倾斜程度插值..?
                if(Time.time - lastAddReactant > 5f)
                {
                    lastAddReactant = Time.time;
                    Container container = targetEquipment;
                    if (state == Reactant.StateOfMatter.Solution)
                    {
                        float v = 0.01f; //L
                        Reactant[] r = new Reactant[2]
                        {
                            Reactant.Create_Liquidity("water", 10000, v),
                            new Reactant(reactant_name, Reactant.StateOfMatter.Solution, ReactionConfig.reagents_identification_name_to_property[identification_name].concentration * v),
                        };
                        if(r[0].reactant_id > r[1].reactant_id)   // 保证顺序。
                        {
                            var tmp = r[0]; r[0] = r[1]; r[1] = tmp;
                        }
                        container.AddReactant(r);
                    }
                    else if (state == Reactant.StateOfMatter.Liquidity)
                    {
                        Reactant r = Reactant.Create_Liquidity(reactant_name, 10000, 0.01f);
                        container.AddReactant(r);
                    }
                }
            }
            else
                targetEquipment.liquidEmitter.VolumePerSimTime = 0f;
        }
    }

    /// <summary>用一个比较特殊的办法判断是否倒出：当attachpoint低于自己的轴心的时候...</summary>
    bool ShouldOutFlow()
    {
        return attachPoints[0].position.y + toppleOffset < transform.position.y;
    }
}
