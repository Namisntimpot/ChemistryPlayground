using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using com.zibra.liquid.Manipulators;

/// <summary>该脚本是所有实验仪器的基类，定义一些通用的基本性质(化简)，比如名字，是否可烧，等等.</summary>
public class Equipment : MonoBehaviour
{
    public struct EqEnv
    {
        public float temperature;   // K
        public float pressure;      // 压强, kPa.
        public float waterVol;      // 水的体积，V.
    }

    static int COUNT = 0;
    [HideInInspector]
    public int uuid;       // 场上仪器的单独id
    public string eqname;      // 仪器的名字
    public bool heatable = false;   // 是否可以加热，不再细分多少度
    public bool reagentContainable = false;    // 是否可以装药剂，不再细分是何种药剂
    [ConditionalHide(nameof(reagentContainable), true)]
    public float flowSpeed = 0.0000001f;
    [ConditionalHide(nameof(reagentContainable), true)]
    public ZibraLiquidEmitter liquidEmitter = null;
    [ConditionalHide(nameof(reagentContainable), true)]
    public MeshRenderer liquidRenderer = null;

    protected bool isReagentCollector;    // 是否可以用来取药的
    public EqEnv env;
    public float volume;    // (给容器的)容积.
    protected float R;      // R=PV/(NT) 它不变！
    public Transform[] attachPoints;
    public bool enableAttachTo = true;
    public int maxAttachedNum = 1;
    public int isAttachedNum = 0;
    //[HideInInspector]
    //public Quaternion[] attachPointsInitRotation;
    public bool attachAsChild = false;  // attach的同时，把自己作为对方的儿子.

    public Equipment attachedTo = null;   // 表示这个装置连接到了别的装置上。
    protected (int, int) attach_from_to;     // (from, to), 第一个from是自己的，第二个to是attachedTo的.


    protected bool isGrabbed = false;
    protected bool isHovered = false;
    //[HideInInspector]
    //public Rigidbody m_rigidbody = null;

    /// <summary>
    /// 继承了Equipment后又重新实现了Start，则下面这个Start会被覆盖.
    /// </summary>
    private void Start()
    {
        Init();
        // eqenv等只在pathway中有用.
    }

    protected virtual void Init()
    {
        uuid = AllocUUID();
        //attachPointsInitRotation = new Quaternion[attachPoints.Length];
        //for(int i=0; i<attachPoints.Length; ++i)
        //{
        //    attachPointsInitRotation[i] = attachPoints[i].rotation;
        //}
    }

    public static int AllocUUID()
    {
        int uuid = COUNT;
        COUNT += 1;
        return uuid;
    }
    /// <summary>
    /// 把B连接到A上，连接点分别是pointB, pointA.
    /// </summary>
    public static void TransformAttach(Equipment A, Equipment B, Transform pointA, Transform pointB)
    {
        Vector3 toRotate = pointA.eulerAngles - pointB.eulerAngles;
        B.transform.Rotate(toRotate, Space.World);
        Vector3 toMove = pointA.position - pointB.position;
        //B.TransformEquipment(toMove, toRotate);
        B.transform.position += toMove;
    }

    public void OnControllerHoverEnter()
    {
        isHovered = true;
        if (attachedTo == null)
            GetComponent<Rigidbody>().isKinematic = true;
    }

    public void OnControllerHoverExit()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (attachedTo == null)
            rigidbody.isKinematic = false;
        else
            rigidbody.isKinematic = true;
        isHovered = false;
    }

    public virtual void OnControllerSelectEnter()
    {
        isGrabbed = true;
    }
    public virtual void OnControllerSelectExit()
    {
        CheckAttachment();
        isGrabbed = false;
    }

    public virtual void OnEquipmentTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Equipment"))
            return;
        AttachEnter(other);
    }

    public virtual void OnEquipmentTriggerExit(Collider other)
    {
        if (!other.CompareTag("Equipment"))
            return;
        AttachExit(other);
    }

    /// <summary>把双方距离最近的连接点相连..对方应该被抓住，而自己没有被抓住. 自己和对方的刚体都应该取消. 一定要是对方collider, 撞到了我方trigger!</summary>
    public void AttachEnter(Collider other)
    {
        // 不要被自己的触发，如果是自己被拿着撞向对方，也不要触发. 如果自己已经被什么东西连着了，也不要触发. 如果是被触发器碰到了触发器，更不要触发...
        if (isAttachedNum >= maxAttachedNum || isGrabbed || other.gameObject == gameObject || other.isTrigger)
            return;
        //XRGrabInteractable xRGrabInteractable = GetComponent<XRGrabInteractable>();
        //if (xRGrabInteractable != null && xRGrabInteractable.isSelected)
        //    return;
        // 找到要attach的两个点.
        //Debug.Log(name + " Enter: " + other.gameObject.name);
        Equipment other_equipment = other.gameObject.GetComponent<Equipment>();
        if (other_equipment is null || !other_equipment.enableAttachTo || !other_equipment.isGrabbed)   // 直接限定死，对方必须正在被抓着!
            return;
        //Transform selfAttach = null, otherAttach = null;
        float dist = float.PositiveInfinity;
        int self_index = 0, other_index = 0;
        for(int i=0; i<attachPoints.Length; ++i)
        {
            for(int j=0; j<other_equipment.attachPoints.Length; ++j)
            {
                float tmp = Vector3.Distance(attachPoints[i].position, other_equipment.attachPoints[j].position);
                if(tmp < dist)
                {
                    dist = tmp;
                    //selfAttach = attachPoints[i];
                    //otherAttach = other_equipment.attachPoints[j];
                    self_index = i;  other_index = j;
                }
            }
        }
        //Vector3 toMove = selfAttach.position - otherAttach.position;   // 要把otherAttach的位置加上这么多.
        //Vector3 toRotate = selfAttach.rotation.eulerAngles - otherAttach.rotation.eulerAngles;
        //other_equipment.TransformEquipment(toMove, toRotate);
        //Equipment.TransformAttach(this, other_equipment, selfAttach, otherAttach);
        other_equipment.attachedTo = this;
        other_equipment.attach_from_to = (other_index, self_index);

        isAttachedNum += 1;
        // 对方也有可能在没有grab的情况下莫名触发.
        //if (!other_equipment.isGrabbed)
        //    other_equipment.CheckAttachment();
        //if(TryGetComponent(out Rigidbody selfRigidbody))
        //    selfRigidbody.isKinematic = true;
    }

    public void AttachExit(Collider other)
    {
        //Debug.Log(name + " trigger exit. by " + other.name);
        // 有可能是正在抽走儿子，也有可能是正在抽走父亲. 儿子.attachTo = 父亲，而父亲.isAttached = true;
        if (isGrabbed || other.gameObject == gameObject || other.isTrigger)
            return;
        //if (!(other.CompareTag("Equipment") && receiveAttachment && isAttached) || other.gameObject == gameObject)
        //    return;
        //XRGrabInteractable xRGrabInteractable = GetComponent<XRGrabInteractable>();
        //if (xRGrabInteractable != null && xRGrabInteractable.isSelected)
        //    return;
        Equipment other_equipment = other.gameObject.GetComponent<Equipment>();
        if (other_equipment is null || !other_equipment.isGrabbed)
            return;
        //Debug.Log(name + " Exit: " + other.name);
        if (attachedTo == other_equipment)  // 正在离开的那个才是“父亲”
        {
            //Debug.Log("Exit: " + other_equipment.eqname + " is father");
            attachedTo = null;
            attach_from_to = (0, 0);
            if (attachAsChild)
                transform.parent = null;
            other_equipment.isAttachedNum -= 1;
            if (TryGetComponent(out Rigidbody rigidbody))
                rigidbody.isKinematic = false;
        }
        else if(other_equipment.attachedTo == this)   // 自己是“父亲”
        {
            //Debug.Log("Exit: " + eqname + " is father");
            other_equipment.attachedTo = null;
            other_equipment.attach_from_to = (0, 0);
            //Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            //m_rigidbody.useGravity = true;
            //m_rigidbody.constraints = RigidbodyConstraints.None;

            isAttachedNum -= 1;

            //if (!other_equipment.isGrabbed)
            //{
            //    if (other_equipment.TryGetComponent(out Rigidbody rigidbody))
            //        rigidbody.isKinematic = false;
            //    if(other_equipment.attachAsChild)
            //        other_equipment.transform.parent = null;
            //}
        }
        //if(TryGetComponent(out Rigidbody selfRigidbody))
        //    selfRigidbody.isKinematic = true;
    }

    public void CheckAttachment()
    {
        if (attachedTo != null)
        {
            //Debug.Log("Check Attachment");
            Transform selfPoint = attachPoints[attach_from_to.Item1], attachToPoint = attachedTo.attachPoints[attach_from_to.Item2];
            Equipment.TransformAttach(attachedTo, this, attachToPoint, selfPoint);
            if (attachAsChild)
                transform.parent = attachedTo.transform;
            //Rigidbody rigidbody = GetComponent<Rigidbody>();
            //m_rigidbody.useGravity = false;
            //m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    //public void TransformEquipment(Vector3 move, Vector3 rotate)
    //{
    //    Transform t = transform;
    //    //while (t.parent.tag == "Equipment")
    //    //    t = t.parent;
    //    t.position += move;
    //    t.Rotate(rotate, Space.World);
    //}
}
