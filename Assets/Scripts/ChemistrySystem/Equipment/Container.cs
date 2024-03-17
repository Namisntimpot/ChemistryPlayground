using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Container : PathwayEquipment
{
    List<Reactant> inner_reactant = new List<Reactant>();
    bool outflowGas { get; set; } = true;   // 如果接下来的导管不入水，或者干脆没有封口，就输出气体，否则如果封口且导管入水，就输出液体.

    public ParticleSystem smokeSystem;

    Color defaultColor;
    Material liquidMaterial;

    bool smokeAtTop = false;
    Vector3 smokeOriginLocalPosition;

    float fullGasColorAmount;   // 完全颜色的时候需要的gas量

    // Start is called before the first frame update
    void Start()
    {
        Init();
        eqtype = PathwayEquipmentType.Reactor;
        reagentContainable = true;
        isReagentCollector = false;
        env = new EqEnv() { pressure = Constant.AtmosphericPressure, temperature = Constant.RoomTemperature, waterVol = 0 };
        // 计算R.
        R = env.pressure / env.temperature * Constant.MolarVolumeOfGas;   // R=PV/(NT), V/N=22.4  ——刚初始化的时候显然是没有加热的.
        // 剩下的应该在inspector列表中自定义.

        // 添加空气类别. 初始就有空气.
        fullGasColorAmount = volume / Constant.MolarVolumeOfGas;
        Reactant air = new Reactant("air", Reactant.StateOfMatter.Gas, fullGasColorAmount);
        inner_reactant.Add(air);

        // 获取液体的颜色.
        liquidMaterial = liquidRenderer.material;
        defaultColor = liquidMaterial.GetColor("_BaseColor");

        smokeOriginLocalPosition = smokeSystem.transform.localPosition;
    }

    public override List<Reactant> Step(List<Reactant> inflow)
    {
        /// 暂时不考虑流入流出为负的情况，也就是说，暂时不管“检查装置气密性”这个设想
        /// 没有考虑空气流速，所以没反应就不会有气体逸散出去...

        // 如果气体是从inflow里面来的，就把粒子系统反过来！
        bool upperSmoke = false;
        foreach(var r in inflow)
        {
            if(r.state == Reactant.StateOfMatter.Gas)
            {
                upperSmoke = true;
                break;
            }
        }
        if(upperSmoke && !smokeAtTop)
        {
            smokeSystem.transform.position = attachedTo.attachPoints[attach_from_to.Item2].position;
            smokeSystem.transform.Rotate(Vector3.right, 180);
            smokeAtTop = true;
        }
        else if(!upperSmoke && smokeAtTop)
        {
            smokeSystem.transform.localPosition = smokeOriginLocalPosition;
            smokeSystem.transform.Rotate(Vector3.right, 180);
            smokeAtTop = false;
        }

        //......
        // 合并或添加.  这一串操作很常见，其实应该封类...
        int i, j;
        for(i=j=0; i<inner_reactant.Count && j<inflow.Count; ++i)
        {
            if(inner_reactant[i].reactant_id == inflow[j].reactant_id)
            {
                inner_reactant[i].Merge(inflow[j++]);
            }
            else if(inner_reactant[i].reactant_id > inflow[j].reactant_id)
            {
                inner_reactant.Insert(i, inflow[j++]);
                i += 1;
            }
        }
        for(; j<inflow.Count; ++j)
        {
            inner_reactant.Add(inflow[j]);
        }

        // 看看有没有要刷新水的体积的...
        foreach(Reactant r in inflow)
        {
            if(r.state == Reactant.StateOfMatter.Liquidity)
            {
                env.waterVol += r.amount_vol;    // 因为现在water暂时只考虑水...
            }
        }

        // 如果inflow中有气体，就在这里提前放烟
        // 修改动画(启动粒子)...
        if (upperSmoke)
        {
            Color gasColor = BlendGasColor();
            if (gasColor.a > 0.01f)
            {
                //gasColor.a = 150f / 255;
                var main = smokeSystem.main;
                main.startColor = gasColor;
                smokeSystem.Play();
            }
            else
            {
                smokeSystem.Stop();
            }
        }


        // 进行一步模拟.
        Simulator simulator = new Simulator(env);
        foreach(Reactant r in inner_reactant)
        {
            simulator.Feed(r);
        }
        inner_reactant = simulator.Finish();

        string log = "";
        foreach(var r in inner_reactant)
        {
            log = log + r.name + "(" + r.amount_mol + "), ";
        }
        Debug.Log("Step " + name + ": " + log);

        // 组织流出的物体，不考虑因化学反应带来的液体体积变化，所以不用重算液体体积.
        List<Reactant> outflow = new List<Reactant>();
        /// 判断是否要流出什么东西，因为暂时不考虑流出为负，所以只判断R变高了. P应该“不变”
        float mol_gas = 0;  // 计算场景中气体mol数量.
        foreach(Reactant r in inner_reactant)
        {
            if (r.state == Reactant.StateOfMatter.Gas)
                mol_gas += r.amount_mol;
        }
        float new_R = env.pressure * (volume - env.waterVol) / mol_gas / env.temperature;

        //Debug.Log("new_R: " + new_R + ", R: " + R);
        if(new_R < R - Constant.Negligible)    // 气体或者液体“多了”. 暂时不考虑“少了”的情况
        {
            if (outflowGas)   // 流出气体，从自己的inner_reactant中等比例地流出气体.
            {
                float delta = mol_gas - env.pressure * (volume - env.waterVol) / R / env.temperature;
                foreach(Reactant r in inner_reactant)
                {
                    if (r.state == Reactant.StateOfMatter.Gas)
                    {
                        float reduction = delta * (r.amount_mol / mol_gas);
                        r.AddAmountMol(-reduction);
                        Reactant outgas = new Reactant(r.name, Reactant.StateOfMatter.Gas, reduction);
                        outflow.Add(outgas);
                    }
                }
            }
            else   // 流出液体.
            {
                float delta = env.waterVol - (volume - mol_gas * R * env.temperature / env.pressure);
                foreach(Reactant r in inner_reactant)
                {
                    if(r.state == Reactant.StateOfMatter.Liquidity)
                    {
                        float reduction = delta * (r.amount_vol / env.waterVol);
                        r.AddAmountMol(-reduction);
                        Reactant outliquid = new Reactant(r.name, Reactant.StateOfMatter.Liquidity, reduction);  // 记得在Reactant中修改液体操作，不是Mol而是vol!
                        outflow.Add(outliquid);
                    }
                    if(r.state == Reactant.StateOfMatter.Solution)
                    {
                        // 浓度不变.
                        float reduction = delta * r.Concentration(env.waterVol);
                        r.AddAmountMol(-reduction);
                        Reactant outsolution = new Reactant(r.name, Reactant.StateOfMatter.Solution, reduction);
                        outflow.Add(outsolution);
                    }
                }
                env.waterVol -= delta;
            }

            // 修改动画(启动粒子)...否则，如果inflow中没有气体，就在这里放烟.
            //Vector4 gasColor = Vector4.zero;
            if (!upperSmoke)
            {
                Color gasColor = BlendGasColor();
                if (gasColor != Color.clear)
                {
                    //gasColor.a = 150f / 255;
                    var main = smokeSystem.main;
                    main.startColor = gasColor;
                    smokeSystem.Play();
                }
                else
                {
                    smokeSystem.Stop();
                }
            }
        }
        else   // 没有变化，没有产生气体的反应. 气体不够，因为暂时不考虑反向的情况，所以直接补充空气.
        {
            float delta = env.pressure * (volume - env.waterVol) / R / env.temperature - mol_gas;   // 要补充的气体量.
            int air_id = ReactionConfig.reagents_name_to_id["air"];
            int k;
            for(k=0; k<inner_reactant.Count; ++k)
            {
                if(inner_reactant[k].reactant_id == air_id)
                {
                    inner_reactant[k].AddAmountMol(delta);
                    break;
                }
                else if(inner_reactant[k].reactant_id > air_id)
                {
                    Reactant air = new Reactant("air", Reactant.StateOfMatter.Gas, delta);
                    inner_reactant.Insert(k, air);
                    break;
                }
            }
            if(k == inner_reactant.Count)
            {
                inner_reactant.Add(new Reactant("air", Reactant.StateOfMatter.Gas, delta));
            }

            if(!upperSmoke)      //如果inflow中没有气体，就在这里结束.
                smokeSystem.Stop();  
        }

        BlendLiquidColor();


        return outflow;
    }

    public override bool ValidateConnectionInPathway(PathwayEquipment equipment)
    {
        // Reactor只能和Connector相连, 除了一个特殊的container: 棉花.
        return (equipment.eqtype == PathwayEquipmentType.Connector) || (equipment.eqtype == PathwayEquipmentType.Plug) || (equipment.eqname == "Cotton");
    }

    public int AddReactant(Reactant reactant, int start_index)
    {
        Debug.Log(name + " (" + eqname + ") " + "received " + reactant.name);
        if (reactant.state == Reactant.StateOfMatter.Liquidity)
        {
            env.waterVol += reactant.amount_vol;
        }
        int i;
        for(i = start_index; i<inner_reactant.Count; ++i)
        {
            if(inner_reactant[i].reactant_id == reactant.reactant_id)
            {
                inner_reactant[i].Merge(reactant);
                return i + 1;
            }
            else if(inner_reactant[i].reactant_id > reactant.reactant_id)
            {
                inner_reactant.Insert(i, reactant);
                return i + 1;
            }
        }
        inner_reactant.Add(reactant);

        return i;
    }
    public void AddReactant(Reactant reactant)
    {
        AddReactant(reactant, 0);
        if (!onSimulation)
        {
            pathway.StartPathwaySimulation(this);
        }
        BlendLiquidColor();
    }

    public void AddReactant(Reactant[] reactants)
    {
        int start = 0;
        foreach (var toAdd in reactants)
            start = AddReactant(toAdd, start);
        if (!onSimulation)
        {
            pathway.StartPathwaySimulation(this);
        }
        BlendLiquidColor();
    }

    /// <summary>
    /// 根据液体/溶液的颜色添加混合液体颜色. 以溶液本身的颜色为基底，所有溶质按照物质的量加权求和，然后相乘，是否改变透明度？并且直接在这里修改液体颜色.
    /// </summary>
    /// <param name="vol_before"></param>
    /// <param name="vol_after"></param>
    /// <param name="newColor"></param>
    void BlendLiquidColor()
    {
        bool hasLiquid = false;
        Color baseColor = Color.white;
        List<Color> solution_color = new List<Color>(3);
        List<float> solution_mol = new List<float>(3);
        float sum_mol = 0;
        foreach(Reactant r in inner_reactant)
        {
            if(r.state == Reactant.StateOfMatter.Liquidity)
            {
                string identification = r.name + '_' + r.state.ToString() + "_none";
                Color lcolor = ReactionConfig.reagents_identification_name_to_property[identification].color;
                lcolor = lcolor == Color.clear ? defaultColor : lcolor;
                baseColor *= lcolor;
                hasLiquid = true;
            }
            else if(r.state == Reactant.StateOfMatter.Solution)
            {
                sum_mol += r.amount_mol;
                solution_mol.Add(r.amount_mol);
                string identification = r.name + '_' + r.state.ToString() + "_none";
                ReactionConfig.ReagentProperty property = ReactionConfig.reagents_identification_name_to_property[identification];
                Color scolor = property.color;
                scolor = scolor == Color.clear ? defaultColor : scolor;
                // 根据浓度在scolor和defaultColor之间插值.
                float concentration = r.Concentration(env.waterVol);
                float fullColor = property.concentration == 0 ? Constant.Negligible : property.concentration;   // 有一点就有颜色.
                float coef = Mathf.Clamp01((concentration - property.threshold) / (fullColor - property.threshold));
                // 考虑一下稀释(根据浓度插值)...
                scolor = Color.Lerp(defaultColor, scolor, coef);
                solution_color.Add(scolor);
                hasLiquid = true;
            }
        }
        if (hasLiquid)
        {
            Color solutionColor = Color.clear;
            for (int i = 0; i < solution_color.Count; ++i)
            {
                solutionColor += solution_color[i] * (solution_mol[i] / sum_mol);
            }
            Color retColor = baseColor * (solutionColor == Color.clear ? Color.white : solutionColor);
            retColor.a = Mathf.Max(baseColor.a, solutionColor.a);
            //Debug.Log("Blend liquid color: " + retColor);
            liquidMaterial.SetColor("_BaseColor", retColor);
        }
    }

    /// <summary>
    /// 计算气体颜色但不直接在这里面修改smokeSystem的颜色. 返回气体颜色(因为外面可能要控制气体粒子开关) \
    /// 混合的时候，a取线性插值，rgb取按mol加权求和.
    /// </summary>
    Color BlendGasColor()
    {
        Color gasColor = Color.clear;
        List<Reactant> inner_gas = inner_reactant.Where((x) => { return x.state == Reactant.StateOfMatter.Gas; }).ToList();
        float sumGasMol = inner_gas.Select((r) => r.amount_mol).Sum();
        foreach(var r in inner_gas)
        {
            Color thisGasColor = ReactionConfig.reagent_property_list[r.reactant_id].color;
            // 在自己的浓度和最小的可忽略浓度之间插值.
            float coef = Mathf.Clamp01((r.amount_mol - Constant.Negligible) / (fullGasColorAmount - Constant.Negligible));
            thisGasColor.a *= coef;
            float tmp_a = gasColor.a;
            gasColor += (r.amount_mol / sumGasMol) * thisGasColor;
            gasColor.a = Mathf.Max(tmp_a, thisGasColor.a);
            //gasColor += Color.Lerp(Color.clear, thisGasColor, coef);   // coef为0时返回Color.clear
        }
        return gasColor;
    }

    /// <summary>
    /// 直接删掉这个容器中的一个反应物. 只可能是固体反应物。
    /// </summary>
    public void RemoveReactant(string reactant_name)
    {
        int id = ReactionConfig.reagents_name_to_id[reactant_name];
        for (int i = 0; i < inner_reactant.Count; ++i)
        {
            if (inner_reactant[i].reactant_id == id)
            {
                inner_reactant.RemoveAt(i);
                break;
            }
            else if (inner_reactant[i].reactant_id > id)
                break;
        }
    }
}
