using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tube : PathwayEquipment
{
    [HideInInspector]
    public bool traversability = true;  // 可自己设置.
    // 如果导管堵住了.
    List<Reactant> inner_reactants = new List<Reactant>();
    bool gasTightness = true;    // 先不管气密性，都当作ok.
    void Start()
    {
        Init();
        traversability = eqtype == PathwayEquipmentType.Connector;
        reagentContainable = true;
        isReagentCollector = false;
        env = new EqEnv() { pressure = Constant.AtmosphericPressure, temperature = Constant.RoomTemperature, waterVol = 0 };
        // 计算R.
        R = env.pressure / env.temperature * Constant.MolarVolumeOfGas;   // R=PV/(NT), V/N=22.4  ——刚初始化的时候显然是没有加热的.
        // 剩下的应该在inspector列表中自定义.

        // 添加空气类别. 初始就有空气.
        Reactant air = new Reactant("air", Reactant.StateOfMatter.Gas, volume / Constant.MolarVolumeOfGas);
        inner_reactants.Add(air);
    }

    public override List<Reactant> Step(List<Reactant> inflow)
    {
        // 如果气密性不好，就排放一些到空气中.
        // ...
        if (traversability)
        {
            // 相应的动画. 先不用动画..
            return inflow;
        }
        // 合并到inner中.
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
        return new List<Reactant>();   // 返回一个空的.
    }

    public override bool ValidateConnectionInPathway(PathwayEquipment equipment)
    {
        return true;  // 导管能和任何连接.
    }
}
