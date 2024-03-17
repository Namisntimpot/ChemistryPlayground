using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>会被纳入反应通路的仪器，包括反应器(容器)、连接件(导管等)、输入器具(漏斗等)</summary>
public abstract class PathwayEquipment : Equipment
{
    public bool onSimulation = false;   // 是否已经开始了模拟！
    /// <summary>
    /// 三种会出现在反应通路中的试剂的类型.
    /// </summary>
    public enum PathwayEquipmentType
    {
        Reactor,   // 反应器
        Connector, // 连接件
        Plug,      // 塞子
        //ReagentProvider,  // 试剂提供者（并且是和整个实验装置固连在一起的那种），分液漏斗，之后应该把它放到unpathway中去！
    }

    public Pathway pathway;   // 全局的pathway.
    public PathwayEquipmentType eqtype;

    /// <summary>在pathway进行模拟的时候，传入流入的反应物，返回这个反应器产出的、**能进入下一个阶段的产物**(气体或者液体)反应物.() </summary>
    public abstract List<Reactant> Step(List<Reactant> inflow);

    /// <summary>检查上下游是否能够连接，比如输入器具不能连接到导管上.</summary>
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

    public override void OnEquipmentTriggerEnter(Collider other)   // 这里一定是"我"是父亲.
    {
        base.OnEquipmentTriggerEnter(other);
        
        // 注意一下，应该要保证Reactor的儿子是Plug, 而Plug的儿子是Connector, 不能 Connector - Plug - Reactor. 应该Reactor - Plug - Connector - (Connector...) - Reactor - Plug - Connector - ..
        // 这里所形成的，一定是自己为father, other为son. 需要注意逆转 Plug和Reactor的父子关系.
        if(other.TryGetComponent(out PathwayEquipment pequipment) && !other.isTrigger && other.gameObject != gameObject && pequipment.attachedTo == this)
        {
            Debug.Log(string.Format("{0} enter into {1}", other.name, name));
            if (eqtype == PathwayEquipmentType.Plug && pequipment.eqtype == PathwayEquipmentType.Reactor)
            {
                //Debug.Log(string.Format("inverse between {0} and {1}", name, other.name));
                // 逆转父子关系.
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
            if(isAttachedNum < origin_isAttachedNum)   // 自己是父亲. 这个条件是充要的.
            {
                pathway.SplitAt(uuid, pequipment.uuid);
            }
            else if(attachedTo is null)    // 对方是父亲并且对方确实与自己分离了.(会有一些很奇怪的触发...)
            {
                pathway.SplitAt(pequipment.uuid, uuid);
            }
        }
    }

    /// <summary>在放置的时候，会调用它作为回调函数，试图把这个仪器注册到一个pathway里，或者新建一个只有自己的pathway.</summary>
    public void RegisterToPathway()
    {
        pathway.RegisterNode(this);
    }

    /// <summary>放置完又试图移动时，会调用它从当前的pathway(可能是无)注销.</summary>
    public void DeregisterFromPathway()
    {
        pathway.DeregisterNode(this);
    }

    public void UpdatePathway()
    {
        // 利用碰撞箱判断是否接近某个其他物品.
    }

    /// <summary>这个仪器的状态发生改变，需要开始模拟(这会导致当前pathway中所有仪器都开始模拟!)</summary>
    public void StartSimulation()
    {
        pathway.StartPathwaySimulation(this);
    }
}
