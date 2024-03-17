using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��֯������pathway, �������еĶ�Ӧ��������!! ע�⻹���ܶ�һ�����������ǵ�����������...
/// </summary>
public class Pathway : MonoBehaviour
{
    public RoomEnvironment roomEnvironment;
    static readonly int REMOVED = -1;  // ��ʾ�����Ѿ���ɾ����״̬.
    bool simulationStarted = false;
    [SerializeField]
    float simu_interval = 3f;
    private void Awake()
    {
        Simulator.globalInterval = simu_interval;
    }
    /// <summary>
    /// ��pathway�еķ�������ǰ�滹���ں���.
    /// </summary>
    public enum Direction
    {
        before, after,
    }
    public class PathwayNode
    {
        static int count = 0;
        public int equipment_id, node_id, par, next;   // par��ָ�򸸽ڵ㣬nextָ���ӽڵ��key(next, parΪ�Լ���key��ʾû��ǰһ������û�к�һ��.)��root��ʾ����key.
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
    public Dictionary<int, PathwayEquipment> EqIdToEquipment = new Dictionary<int, PathwayEquipment>();   // ������id������������������ʱ���ã�
    public Dictionary<int, int> EqIdToNodeId = new Dictionary<int, int>();   // ������id������������Ľڵ㣨����ͼ��ʱ����)

    /// <summary> ��ȡ����ڵ�ĸ���Ҳ�������pathway����� </summary>
    int GetRoot(int nodeid)
    {
        int id = nodeid;
        while (NodeList[id].par != id)
            id = NodeList[id].par;
        return id;
    }
    /// <summary>����������������Ľڵ���������.ע��ֻ����β����������ᶪ��epid_from�ڵ�������ӵĶ����Լ�epid_toǰ�����ӵĶ���(���ǻ��γ��µ���.)
    /// eqid_from��Ӧ���ף�eqid_to��Ӧ����</summary>
    public void Connect(int eqid_from, int eqid_to)
    {
        int nodeid_from = EqIdToNodeId[eqid_from];
        int nodeid_to = EqIdToNodeId[eqid_to];
        //Debug.Log(string.Format("Connect eqid: {0} -> {1}, nodeid: {2} -> {3}", eqid_from, eqid_to, nodeid_from, nodeid_to));
        if (NodeList[nodeid_from].next != nodeid_from)  // from����β�ͣ�����֮����������γ��µ���.
        {
            NodeList[NodeList[nodeid_from].next].par = NodeList[nodeid_from].next;   // ��ԭ������һ��������Ϊ�µ��������µ�ͷ.
        }
        if(NodeList[nodeid_to].par != nodeid_to)  // to����ͷ������֮ǰ���������γ��µ���
        {
            NodeList[NodeList[nodeid_to].par].next = NodeList[nodeid_to].par;
        }
        NodeList[nodeid_from].next = nodeid_to;
        NodeList[nodeid_to].par = nodeid_from;
        //Debug.Log(string.Format("NodeList[nodeid_from].next = {0}", NodeList[nodeid_from].next));
    }

    /// <summary>��������������ڵ��������Ͱ����Ƿֿ����������������ʲôҲ����</summary>
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


    /// <summary>�ѽڵ㵥���ó��������һ������ǰ����������</summary>
    void MakeSeparate(int eqid)
    {
        int nodeid = EqIdToNodeId[eqid];
        int last = NodeList[nodeid].par;
        int next = NodeList[nodeid].next;
        NodeList[nodeid].par = NodeList[nodeid].next = nodeid;  // ���Լ������ó���
        NodeList[next].par = next;   // ����һ���ڵ��Ϊ��
        NodeList[last].next = last;  // ����һ���ڵ��Ϊ���һ��
    }
    /// <summary>��һ��**������**�����ڵ���뵽һ��eqid_before�ĺ���,ԭ��eqid_before������������eq_separate�����
    /// ���eqid_separate���ǵ����ģ���ǿ�а�����ԭ����������ȡ������ԭ����������ǰ�����˵��������γ���������������</summary>
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
    /// <summary>��һ��**������**�����ڵ���뵽һ��eqid_after��ǰ��,ԭ��eqid_afterǰ����������eq_separateǰ���
    /// ���eqid_separate���ǵ����ģ���ǿ�а�����ԭ����������ȡ������ԭ����������ǰ�����˵��������γ���������������</summary>
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
        PathwayNode pathwayNode = new PathwayNode(equipment.uuid);  // Ŀǰ��˵�ǵ�����.
        NodeList.Add(pathwayNode);
        EqIdToEquipment.Add(equipment.uuid, equipment);
        EqIdToNodeId.Add(equipment.uuid, pathwayNode.node_id);
    }
    /// <summary>
    /// ɾ��һ���������������ֵ��н���ɾ������NodeList�н�����������������ڵ�������ڵ㣬�����ͷţ����ǵ����Ľڵ�.
    /// </summary>
    public void DeregisterNode(PathwayEquipment equipment)
    {
        MakeSeparate(equipment.uuid);
        int node_id = EqIdToNodeId[equipment.uuid];
        EqIdToEquipment.Remove(equipment.uuid);
        EqIdToNodeId.Remove(equipment.uuid);
        NodeList[node_id].equipment_id = REMOVED;
    }

    /// <summary>�޸�pathway�е����ӹ�ϵ.������ԣ����ǰ�toplace����������Ȼ��ŵ�place_at(����)��ǰ������</summary>
    public void UpdateNode(PathwayEquipment equipment_to_place, PathwayEquipment place_at, Direction direction)
    {
        MakeSeparate(equipment_to_place.uuid);
        if (place_at is null)  // Ҫ������equipment_to_place�ó���.
        {
            return;
        }
        else if(direction == Direction.after)  // Ҫ��eq_to_place�ŵ�place_at����
        {
            InsertAfter(equipment_to_place.uuid, place_at.uuid);
        }
        else
        {
            InsertBefore(equipment_to_place.uuid, place_at.uuid);
        }
    }

    /// <summary>��ʼequipment���ڵ�pathway��ģ�⡣��״̬�����ı��ʱ�򣬻ᴥ������</summary>
    public void StartPathwaySimulation(PathwayEquipment equipment)
    {
        int eqid = equipment.uuid;
        int nodeid = EqIdToNodeId[eqid];
        equipment.onSimulation = true;
        StartPathwaySimulation(nodeid);
    }
    /// <summary>��ʼnodeid���ڵ�������ģ�⣬ע����ʱ��nodeid������������Ѿ�����ģ���ˣ�</summary>
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
    /// <summary> ����һ��ģ��...</summary>
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

                List<Reactant> reactant = new List<Reactant>();   // ע����ֻ����ʱռλ�õģ�
                int i = node.node_id;
                PathwayEquipment.PathwayEquipmentType from = PathwayEquipment.PathwayEquipmentType.Reactor;    // ��ǰ���reactant�Ǵ����ֳɷ�������.
                for(; NodeList[i].next != i; i = NodeList[i].next)   // ���ѭ����һ���С���һ����
                {
                    PathwayEquipment thisequipment = EqIdToEquipment[NodeList[i].equipment_id];
                    reactant = thisequipment.Step(reactant);

                    PathwayEquipment nextequipment = EqIdToEquipment[NodeList[NodeList[i].next].equipment_id];
                    switch (thisequipment.eqtype)
                    {
                        case PathwayEquipment.PathwayEquipmentType.Reactor:
                            from = PathwayEquipment.PathwayEquipmentType.Reactor;
                            if(nextequipment.eqtype != PathwayEquipment.PathwayEquipmentType.Plug)   // �����������ŷŵ���������
                            {
                                roomEnvironment.Emit(reactant);
                                reactant.Clear();
                            }
                            else
                            {
                                i = NodeList[i].next;    // ������һ���������. ����������Ǹ��ܵ�.
                            }
                            break;
                        case PathwayEquipment.PathwayEquipmentType.Connector:
                            from = PathwayEquipment.PathwayEquipmentType.Connector;
                            if(nextequipment.eqtype == PathwayEquipment.PathwayEquipmentType.Plug)   // ���������������...
                            {
                                i = NodeList[i].next;
                            }
                            break;
                    }
                }
                // �������һ��...
                PathwayEquipment lastequipment = EqIdToEquipment[NodeList[i].equipment_id];
                if (lastequipment.eqtype != PathwayEquipment.PathwayEquipmentType.Plug)
                    reactant = lastequipment.Step(reactant);
                else if(from == PathwayEquipment.PathwayEquipmentType.Reactor)   // ��ǰ���reactant�Ǵ�reactor���ģ�˵��reactor��һ����plug������ֱ�Ӻ������������´���.
                    reactant.Clear();

                // ���reactant���ж������ͷŵ�������.
                roomEnvironment.Emit(reactant);
            }
        }
    }
}
