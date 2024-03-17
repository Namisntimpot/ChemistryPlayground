using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 组织仪器的pathway, 里面所有的都应该是引用!! 注意还可能对一串链条而不是单个仪器操作...
/// </summary>
public class Pathway : MonoBehaviour
{
    public RoomEnvironment roomEnvironment;
    static readonly int REMOVED = -1;  // 表示仪器已经被删除的状态.
    bool simulationStarted = false;
    [SerializeField]
    float simu_interval = 3f;
    private void Awake()
    {
        Simulator.globalInterval = simu_interval;
    }
    /// <summary>
    /// 在pathway中的方向，是在前面还是在后面.
    /// </summary>
    public enum Direction
    {
        before, after,
    }
    public class PathwayNode
    {
        static int count = 0;
        public int equipment_id, node_id, par, next;   // par是指向父节点，next指向子节点的key(next, par为自己的key表示没有前一个或者没有后一个.)，root表示根的key.
        public PathwayNode(int equipment_id)
        {
            this.equipment_id = equipment_id;
            par = next = node_id = count;
            count += 1;
        }
        public bool IsRoot()
        {
            return node_id == par;
        }
        public bool IsTail()
        {
            return node_id == next;
        }
    }
    public List<PathwayNode> NodeList = new List<PathwayNode>();
    public Dictionary<int, PathwayEquipment> EqIdToEquipment = new Dictionary<int, PathwayEquipment>();   // 仪器的id访问这个仪器（仿真的时候用）
    public Dictionary<int, int> EqIdToNodeId = new Dictionary<int, int>();   // 仪器的id访问这个仪器的节点（更新图的时候用)

    /// <summary> 获取这个节点的根，也就是这个pathway的起点 </summary>
    int GetRoot(int nodeid)
    {
        int id = nodeid;
        while (NodeList[id].par != id)
            id = NodeList[id].par;
        return id;
    }
    /// <summary>把两个仪器所代表的节点连接起来.注意只能首尾相连，否则会丢掉epid_from节点后面连接的东西以及epid_to前面连接的东西(他们会形成新的链.)
    /// eqid_from对应父亲，eqid_to对应儿子</summary>
    public void Connect(int eqid_from, int eqid_to)
    {
        int nodeid_from = EqIdToNodeId[eqid_from];
        int nodeid_to = EqIdToNodeId[eqid_to];
        //Debug.Log(string.Format("Connect eqid: {0} -> {1}, nodeid: {2} -> {3}", eqid_from, eqid_to, nodeid_from, nodeid_to));
        if (NodeList[nodeid_from].next != nodeid_from)  // from不是尾巴，则它之后的链条会形成新的链.
        {
            NodeList[NodeList[nodeid_from].next].par = NodeList[nodeid_from].next;   // 让原本的下一个链条成为新的链条，新的头.
        }
        if(NodeList[nodeid_to].par != nodeid_to)  // to不是头，则它之前的链条会形成新的链
        {
            NodeList[NodeList[nodeid_to].par].next = NodeList[nodeid_to].par;
        }
        NodeList[nodeid_from].next = nodeid_to;
        NodeList[nodeid_to].par = nodeid_from;
        //Debug.Log(string.Format("NodeList[nodeid_from].next = {0}", NodeList[nodeid_from].next));
    }

    /// <summary>如果给定的两个节点相连，就把他们分开；如果不想连，就什么也不做</summary>
    public void SplitAt(int eqid_par, int eqid_son)
    {
        int nodeid_par = EqIdToNodeId[eqid_par];
        int nodeid_son = EqIdToNodeId[eqid_son];
        //Debug.Log(string.Format("Pathway Split at nodeid: {0} -> {1};", nodeid_par, nodeid_son));
        if (NodeList[nodeid_son].par == nodeid_par)
        {
            NodeList[nodeid_par].next = nodeid_par;
            NodeList[nodeid_son].par = nodeid_son;
        }
    }


    /// <summary>把节点单独拿出来，变成一个，它前后变成两条链</summary>
    void MakeSeparate(int eqid)
    {
        int nodeid = EqIdToNodeId[eqid];
        int last = NodeList[nodeid].par;
        int next = NodeList[nodeid].next;
        NodeList[nodeid].par = NodeList[nodeid].next = nodeid;  // 把自己单独拿出来
        NodeList[next].par = next;   // 让下一个节点成为根
        NodeList[last].next = last;  // 让上一个节点成为最后一个
    }
    /// <summary>把一个**单独的**仪器节点插入到一个eqid_before的后面,原本eqid_before后面的链条变成eq_separate后面的
    /// 如果eqid_separate不是单独的，会强行把它从原本的链条中取出来，原本链条中其前后两端的链条会形成两个单独的链条</summary>
    void InsertAfter(int eqid_separate, int eqid_before)
    {
        int separate = EqIdToNodeId[eqid_separate], before = EqIdToNodeId[eqid_before];
        if (!(NodeList[separate].IsRoot() && NodeList[separate].IsTail()))
        {
            MakeSeparate(eqid_separate);
        }
        if (NodeList[before].IsTail())
        {
            NodeList[before].next = separate;
            NodeList[separate].par = before;
        }
        else
        {
            int after = NodeList[before].next;
            NodeList[before].next = separate;
            NodeList[separate].par = before;
            NodeList[separate].next = after;
            NodeList[after].par = separate;
        }
    }
    /// <summary>把一个**单独的**仪器节点插入到一个eqid_after的前面,原本eqid_after前面的链条变成eq_separate前面的
    /// 如果eqid_separate不是单独的，会强行把它从原本的链条中取出来，原本链条中其前后两端的链条会形成两个单独的链条</summary>
    void InsertBefore(int eqid_separate, int eqid_after)
    {
        int separate = EqIdToNodeId[eqid_separate], after = EqIdToNodeId[eqid_after];
        if (!(NodeList[separate].IsRoot() && NodeList[separate].IsTail()))
        {
            MakeSeparate(eqid_separate);
        }
        if (NodeList[after].IsRoot())
        {
            NodeList[after].par = separate;
            NodeList[separate].next = after;
        }
        else
        {
            int before = NodeList[after].par;
            NodeList[after].par = separate;
            NodeList[separate].next = after;
            NodeList[separate].par = before;
            NodeList[before].next = separate;
        }
    }
    public void RegisterNode(PathwayEquipment equipment)
    {
        PathwayNode pathwayNode = new PathwayNode(equipment.uuid);  // 目前来说是单独的.
        NodeList.Add(pathwayNode);
        EqIdToEquipment.Add(equipment.uuid, equipment);
        EqIdToNodeId.Add(equipment.uuid, pathwayNode.node_id);
    }
    /// <summary>
    /// 删除一个仪器。在两个字典中将它删除，在NodeList中将它保留，但是这个节点会变成死节点，不会释放，而是单独的节点.
    /// </summary>
    public void DeregisterNode(PathwayEquipment equipment)
    {
        MakeSeparate(equipment.uuid);
        int node_id = EqIdToNodeId[equipment.uuid];
        EqIdToEquipment.Remove(equipment.uuid);
        EqIdToNodeId.Remove(equipment.uuid);
        NodeList[node_id].equipment_id = REMOVED;
    }

    /// <summary>修改pathway中的连接关系.具体而言，就是把toplace独立出来，然后放到place_at(如有)的前面或后面</summary>
    public void UpdateNode(PathwayEquipment equipment_to_place, PathwayEquipment place_at, Direction direction)
    {
        MakeSeparate(equipment_to_place.uuid);
        if (place_at is null)  // 要单独把equipment_to_place拿出来.
        {
            return;
        }
        else if(direction == Direction.after)  // 要把eq_to_place放到place_at后面
        {
            InsertAfter(equipment_to_place.uuid, place_at.uuid);
        }
        else
        {
            InsertBefore(equipment_to_place.uuid, place_at.uuid);
        }
    }

    /// <summary>开始equipment所在的pathway的模拟。当状态发生改变的时候，会触发它。</summary>
    public void StartPathwaySimulation(PathwayEquipment equipment)
    {
        int eqid = equipment.uuid;
        int nodeid = EqIdToNodeId[eqid];
        equipment.onSimulation = true;
        StartPathwaySimulation(nodeid);
    }
    /// <summary>开始nodeid所在的链条的模拟，注意这时候nodeid所代表的仪器已经开启模拟了！</summary>
    public void StartPathwaySimulation(int nodeid)
    {
        for (int i = NodeList[nodeid].next; NodeList[i].next != i; i = NodeList[i].next)
        {
            EqIdToEquipment[NodeList[i].equipment_id].onSimulation = true;
        }
        for (int i = NodeList[nodeid].par; NodeList[i].par != i; i = NodeList[i].par)
        {
            EqIdToEquipment[NodeList[i].equipment_id].onSimulation = true;
        }

        if (!simulationStarted)
        {
            simulationStarted = true;
            InvokeRepeating("Simulate", 0, simu_interval);
        }
    }
    /// <summary> 进行一次模拟...</summary>
    public void Simulate()
    {
        foreach(var node in NodeList)
        {
            if(node.equipment_id != REMOVED && node.IsRoot() && EqIdToEquipment[node.equipment_id].onSimulation)
            {
                string log = "Pathway: ";
                int j;
                //Debug.Log(string.Format("node_id: {0}, next: {1}", node.node_id, node.next));
                for(j = node.node_id; NodeList[j].next!=j; j = NodeList[j].next)
                {
                    log += EqIdToEquipment[NodeList[j].equipment_id].name + " - ";
                }
                log += EqIdToEquipment[NodeList[j].equipment_id].name;
                Debug.Log(log);

                List<Reactant> reactant = new List<Reactant>();   // 注意这只是暂时占位置的！
                int i = node.node_id;
                PathwayEquipment.PathwayEquipmentType from = PathwayEquipment.PathwayEquipmentType.Reactor;    // 当前这个reactant是从哪种成分里来的.
                for(; NodeList[i].next != i; i = NodeList[i].next)   // 这个循环中一定有“下一个”
                {
                    PathwayEquipment thisequipment = EqIdToEquipment[NodeList[i].equipment_id];
                    reactant = thisequipment.Step(reactant);

                    PathwayEquipment nextequipment = EqIdToEquipment[NodeList[NodeList[i].next].equipment_id];
                    switch (thisequipment.eqtype)
                    {
                        case PathwayEquipment.PathwayEquipmentType.Reactor:
                            from = PathwayEquipment.PathwayEquipmentType.Reactor;
                            if(nextequipment.eqtype != PathwayEquipment.PathwayEquipmentType.Plug)   // 敞口容器，排放到空气。。
                            {
                                roomEnvironment.Emit(reactant);
                                reactant.Clear();
                            }
                            else
                            {
                                i = NodeList[i].next;    // 跳过下一个这个塞子. 传入接下来那个管道.
                            }
                            break;
                        case PathwayEquipment.PathwayEquipmentType.Connector:
                            from = PathwayEquipment.PathwayEquipmentType.Connector;
                            if(nextequipment.eqtype == PathwayEquipment.PathwayEquipmentType.Plug)   // 跳过下面这个塞子...
                            {
                                i = NodeList[i].next;
                            }
                            break;
                    }
                }
                // 还差最后一个...
                PathwayEquipment lastequipment = EqIdToEquipment[NodeList[i].equipment_id];
                if (lastequipment.eqtype != PathwayEquipment.PathwayEquipmentType.Plug)
                    reactant = lastequipment.Step(reactant);
                else if(from == PathwayEquipment.PathwayEquipmentType.Reactor)   // 当前这个reactant是从reactor来的，说明reactor下一个是plug，可以直接忽略它，不往下传播.
                    reactant.Clear();

                // 如果reactant中有东西，释放到环境中.
                roomEnvironment.Emit(reactant);
            }
        }
    }
}
