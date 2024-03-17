using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using com.zibra.liquid.Manipulators;

/// <summary>�ýű�������ʵ�������Ļ��࣬����һЩͨ�õĻ�������(����)���������֣��Ƿ���գ��ȵ�.</summary>
public class Equipment : MonoBehaviour
{
    public struct EqEnv
    {
        public float temperature;   // K
        public float pressure;      // ѹǿ, kPa.
        public float waterVol;      // ˮ�������V.
    }

    static int COUNT = 0;
    [HideInInspector]
    public int uuid;       // ���������ĵ���id
    public string eqname;      // ����������
    public bool heatable = false;   // �Ƿ���Լ��ȣ�����ϸ�ֶ��ٶ�
    public bool reagentContainable = false;    // �Ƿ����װҩ��������ϸ���Ǻ���ҩ��
    [ConditionalHide(nameof(reagentContainable), true)]
    public float flowSpeed = 0.0000001f;
    [ConditionalHide(nameof(reagentContainable), true)]
    public ZibraLiquidEmitter liquidEmitter = null;
    [ConditionalHide(nameof(reagentContainable), true)]
    public MeshRenderer liquidRenderer = null;

    protected bool isReagentCollector;    // �Ƿ��������ȡҩ��
    public EqEnv env;
    public float volume;    // (��������)�ݻ�.
    protected float R;      // R=PV/(NT) �����䣡
    public Transform[] attachPoints;
    public bool enableAttachTo = true;
    public int maxAttachedNum = 1;
    public int isAttachedNum = 0;
    //[HideInInspector]
    //public Quaternion[] attachPointsInitRotation;
    public bool attachAsChild = false;  // attach��ͬʱ�����Լ���Ϊ�Է��Ķ���.

    public Equipment attachedTo = null;   // ��ʾ���װ�����ӵ��˱��װ���ϡ�
    protected (int, int) attach_from_to;     // (from, to), ��һ��from���Լ��ģ��ڶ���to��attachedTo��.


    protected bool isGrabbed = false;
    protected bool isHovered = false;
    //[HideInInspector]
    //public Rigidbody m_rigidbody = null;

    /// <summary>
    /// �̳���Equipment��������ʵ����Start�����������Start�ᱻ����.
    /// </summary>
    private void Start()
    {
        Init();
        // eqenv��ֻ��pathway������.
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
    /// ��B���ӵ�A�ϣ����ӵ�ֱ���pointB, pointA.
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

    /// <summary>��˫��������������ӵ�����..�Է�Ӧ�ñ�ץס�����Լ�û�б�ץס. �Լ��ͶԷ��ĸ��嶼Ӧ��ȡ��. һ��Ҫ�ǶԷ�collider, ײ�����ҷ�trigger!</summary>
    public void AttachEnter(Collider other)
    {
        // ��Ҫ���Լ��Ĵ�����������Լ�������ײ��Է���Ҳ��Ҫ����. ����Լ��Ѿ���ʲô���������ˣ�Ҳ��Ҫ����. ����Ǳ������������˴�����������Ҫ����...
        if (isAttachedNum >= maxAttachedNum || isGrabbed || other.gameObject == gameObject || other.isTrigger)
            return;
        //XRGrabInteractable xRGrabInteractable = GetComponent<XRGrabInteractable>();
        //if (xRGrabInteractable != null && xRGrabInteractable.isSelected)
        //    return;
        // �ҵ�Ҫattach��������.
        //Debug.Log(name + " Enter: " + other.gameObject.name);
        Equipment other_equipment = other.gameObject.GetComponent<Equipment>();
        if (other_equipment is null || !other_equipment.enableAttachTo || !other_equipment.isGrabbed)   // ֱ���޶������Է��������ڱ�ץ��!
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
        //Vector3 toMove = selfAttach.position - otherAttach.position;   // Ҫ��otherAttach��λ�ü�����ô��.
        //Vector3 toRotate = selfAttach.rotation.eulerAngles - otherAttach.rotation.eulerAngles;
        //other_equipment.TransformEquipment(toMove, toRotate);
        //Equipment.TransformAttach(this, other_equipment, selfAttach, otherAttach);
        other_equipment.attachedTo = this;
        other_equipment.attach_from_to = (other_index, self_index);

        isAttachedNum += 1;
        // �Է�Ҳ�п�����û��grab�������Ī������.
        //if (!other_equipment.isGrabbed)
        //    other_equipment.CheckAttachment();
        //if(TryGetComponent(out Rigidbody selfRigidbody))
        //    selfRigidbody.isKinematic = true;
    }

    public void AttachExit(Collider other)
    {
        //Debug.Log(name + " trigger exit. by " + other.name);
        // �п��������ڳ��߶��ӣ�Ҳ�п��������ڳ��߸���. ����.attachTo = ���ף�������.isAttached = true;
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
        if (attachedTo == other_equipment)  // �����뿪���Ǹ����ǡ����ס�
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
        else if(other_equipment.attachedTo == this)   // �Լ��ǡ����ס�
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
