using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube : PathwayEquipment
{
    [HideInInspector]
    public bool traversability = true;  // ���Լ�����.
    // ������ܶ�ס��.
    List<Reactant> inner_reactants = new List<Reactant>();
    bool gasTightness = true;    // �Ȳ��������ԣ�������ok.
    void Start()
    {
        Init();
        traversability = eqtype == PathwayEquipmentType.Connector;
        reagentContainable = true;
        isReagentCollector = false;
        env = new EqEnv() { pressure = Constant.AtmosphericPressure, temperature = Constant.RoomTemperature, waterVol = 0 };
        // ����R.
        R = env.pressure / env.temperature * Constant.MolarVolumeOfGas;   // R=PV/(NT), V/N=22.4  �����ճ�ʼ����ʱ����Ȼ��û�м��ȵ�.
        // ʣ�µ�Ӧ����inspector�б����Զ���.

        // ��ӿ������. ��ʼ���п���.
        Reactant air = new Reactant("air", Reactant.StateOfMatter.Gas, volume / Constant.MolarVolumeOfGas);
        inner_reactants.Add(air);
    }

    public override List<Reactant> Step(List<Reactant> inflow)
    {
        // ��������Բ��ã����ŷ�һЩ��������.
        // ...
        if (traversability)
        {
            // ��Ӧ�Ķ���. �Ȳ��ö���..
            return inflow;
        }
        // �ϲ���inner��.
        int i, j;
        for (i = j = 0; i < inner_reactants.Count && j < inflow.Count; ++i)
        {
            if (inner_reactants[i].reactant_id == inflow[j].reactant_id)
            {
                inner_reactants[i].Merge(inflow[j++]);
            }
            if (inner_reactants[i].reactant_id > inflow[j].reactant_id)
            {
                inner_reactants.Insert(i, inflow[j++]);
                i += 1;
            }
        }
        for (; j < inflow.Count; ++j)
        {
            inner_reactants.Add(inflow[j]);
        }
        return new List<Reactant>();   // ����һ���յ�.
    }

    public override bool ValidateConnectionInPathway(PathwayEquipment equipment)
    {
        return true;  // �����ܺ��κ�����.
    }
}
