using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>�ᱻ���뷴Ӧͨ·��������������Ӧ��(����)�����Ӽ�(���ܵ�)����������(©����)</summary>
public abstract class PathwayEquipment : Equipment
{
    public bool onSimulation = false;   // �Ƿ��Ѿ���ʼ��ģ�⣡
    /// <summary>
    /// ���ֻ�����ڷ�Ӧͨ·�е��Լ�������.
    /// </summary>
    public enum PathwayEquipmentType
    {
        Reactor,   // ��Ӧ��
        Connector, // ���Ӽ�
        Plug,      // ����
        //ReagentProvider,  // �Լ��ṩ�ߣ������Ǻ�����ʵ��װ�ù�����һ������֣�����Һ©����֮��Ӧ�ð����ŵ�unpathway��ȥ��
    }

    public Pathway pathway;   // ȫ�ֵ�pathway.
    public PathwayEquipmentType eqtype;

    /// <summary>��pathway����ģ���ʱ�򣬴�������ķ�Ӧ����������Ӧ�������ġ�**�ܽ�����һ���׶εĲ���**(�������Һ��)��Ӧ��.() </summary>
    public abstract List<Reactant> Step(List<Reactant> inflow);

    /// <summary>����������Ƿ��ܹ����ӣ������������߲������ӵ�������.</summary>
    public abstract bool ValidateConnectionInPathway(PathwayEquipment equipment);

    protected override void Init()
    {
        base.Init();
        if (pathway is null)
            pathway = GameObject.Find("Simulator").GetComponent<Pathway>();
        RegisterToPathway();
    }

    private void OnDestroy()
    {
        DeregisterFromPathway();
    }

    public override void OnEquipmentTriggerEnter(Collider other)   // ����һ����"��"�Ǹ���.
    {
        base.OnEquipmentTriggerEnter(other);
        
        // ע��һ�£�Ӧ��Ҫ��֤Reactor�Ķ�����Plug, ��Plug�Ķ�����Connector, ���� Connector - Plug - Reactor. Ӧ��Reactor - Plug - Connector - (Connector...) - Reactor - Plug - Connector - ..
        // �������γɵģ�һ�����Լ�Ϊfather, otherΪson. ��Ҫע����ת Plug��Reactor�ĸ��ӹ�ϵ.
        if(other.TryGetComponent(out PathwayEquipment pequipment) && !other.isTrigger && other.gameObject != gameObject && pequipment.attachedTo == this)
        {
            Debug.Log(string.Format("{0} enter into {1}", other.name, name));
            if (eqtype == PathwayEquipmentType.Plug && pequipment.eqtype == PathwayEquipmentType.Reactor)
            {
                //Debug.Log(string.Format("inverse between {0} and {1}", name, other.name));
                // ��ת���ӹ�ϵ.
                Equipment par_eq = attachedTo;
                int par_attachpoint_id = attach_from_to.Item2;

                isAttachedNum -= 1;
                attachedTo = pequipment;
                attach_from_to = (pequipment.attach_from_to.Item2, pequipment.attach_from_to.Item1);

                pequipment.attachedTo = par_eq;
                int other_from_id = 0;
                if (par_eq is not null)
                {
                    float min_distance = Vector3.Distance(par_eq.attachPoints[par_attachpoint_id].position, pequipment.attachPoints[0].position);
                    for (int i = 1; i < pequipment.attachPoints.Length; ++i)
                    {
                        float distance = Vector3.Distance(par_eq.attachPoints[par_attachpoint_id].position, pequipment.attachPoints[i].position);
                        if(distance < min_distance)
                        {
                            min_distance = distance;
                            other_from_id = i;
                        }
                    }
                }
                pequipment.attach_from_to = (other_from_id, par_attachpoint_id);
                pequipment.isAttachedNum += 1;

                //Debug.Log(string.Format("Connect in pathway: {0} -> {1}", pequipment.name, name));
                if (par_eq is not null)
                {
                    pathway.SplitAt(par_eq.uuid, uuid);
                    pathway.Connect(par_eq.uuid, pequipment.uuid);
                }
                pathway.Connect(pequipment.uuid, uuid);
            }
            else if (ValidateConnectionInPathway(pequipment))
            {
                //Debug.Log(string.Format("Connect in pathway: {0} -> {1}", name, pequipment.name));
                pathway.Connect(uuid, pequipment.uuid);
            }
        }
    }

    public override void OnEquipmentTriggerExit(Collider other)
    {
        int origin_isAttachedNum = isAttachedNum;
        base.OnEquipmentTriggerExit(other);

        if(other.TryGetComponent(out PathwayEquipment pequipment) && !other.isTrigger && other.gameObject != gameObject)
        {
            //Debug.Log(string.Format("{0} exit from {1}", other.name, name));
            if(isAttachedNum < origin_isAttachedNum)   // �Լ��Ǹ���. ��������ǳ�Ҫ��.
            {
                pathway.SplitAt(uuid, pequipment.uuid);
            }
            else if(attachedTo is null)    // �Է��Ǹ��ײ��ҶԷ�ȷʵ���Լ�������.(����һЩ����ֵĴ���...)
            {
                pathway.SplitAt(pequipment.uuid, uuid);
            }
        }
    }

    /// <summary>�ڷ��õ�ʱ�򣬻��������Ϊ�ص���������ͼ���������ע�ᵽһ��pathway������½�һ��ֻ���Լ���pathway.</summary>
    public void RegisterToPathway()
    {
        pathway.RegisterNode(this);
    }

    /// <summary>����������ͼ�ƶ�ʱ����������ӵ�ǰ��pathway(��������)ע��.</summary>
    public void DeregisterFromPathway()
    {
        pathway.DeregisterNode(this);
    }

    public void UpdatePathway()
    {
        // ������ײ���ж��Ƿ�ӽ�ĳ��������Ʒ.
    }

    /// <summary>���������״̬�����ı䣬��Ҫ��ʼģ��(��ᵼ�µ�ǰpathway��������������ʼģ��!)</summary>
    public void StartSimulation()
    {
        pathway.StartPathwaySimulation(this);
    }
}
