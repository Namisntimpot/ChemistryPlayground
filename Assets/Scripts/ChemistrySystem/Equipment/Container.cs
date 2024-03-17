using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Container : PathwayEquipment
{
    List<Reactant> inner_reactant = new List<Reactant>();
    bool outflowGas { get; set; } = true;   // ����������ĵ��ܲ���ˮ�����߸ɴ�û�з�ڣ���������壬�����������ҵ�����ˮ�������Һ��.

    public ParticleSystem smokeSystem;

    Color defaultColor;
    Material liquidMaterial;

    bool smokeAtTop = false;
    Vector3 smokeOriginLocalPosition;

    float fullGasColorAmount;   // ��ȫ��ɫ��ʱ����Ҫ��gas��

    // Start is called before the first frame update
    void Start()
    {
        Init();
        eqtype = PathwayEquipmentType.Reactor;
        reagentContainable = true;
        isReagentCollector = false;
        env = new EqEnv() { pressure = Constant.AtmosphericPressure, temperature = Constant.RoomTemperature, waterVol = 0 };
        // ����R.
        R = env.pressure / env.temperature * Constant.MolarVolumeOfGas;   // R=PV/(NT), V/N=22.4  �����ճ�ʼ����ʱ����Ȼ��û�м��ȵ�.
        // ʣ�µ�Ӧ����inspector�б����Զ���.

        // ��ӿ������. ��ʼ���п���.
        fullGasColorAmount = volume / Constant.MolarVolumeOfGas;
        Reactant air = new Reactant("air", Reactant.StateOfMatter.Gas, fullGasColorAmount);
        inner_reactant.Add(air);

        // ��ȡҺ�����ɫ.
        liquidMaterial = liquidRenderer.material;
        defaultColor = liquidMaterial.GetColor("_BaseColor");

        smokeOriginLocalPosition = smokeSystem.transform.localPosition;
    }

    public override List<Reactant> Step(List<Reactant> inflow)
    {
        /// ��ʱ��������������Ϊ���������Ҳ����˵����ʱ���ܡ����װ�������ԡ��������
        /// û�п��ǿ������٣�����û��Ӧ�Ͳ�����������ɢ��ȥ...

        // ��������Ǵ�inflow�������ģ��Ͱ�����ϵͳ��������
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
        // �ϲ������.  ��һ�������ܳ�������ʵӦ�÷���...
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

        // ������û��Ҫˢ��ˮ�������...
        foreach(Reactant r in inflow)
        {
            if(r.state == Reactant.StateOfMatter.Liquidity)
            {
                env.waterVol += r.amount_vol;    // ��Ϊ����water��ʱֻ����ˮ...
            }
        }

        // ���inflow�������壬����������ǰ����
        // �޸Ķ���(��������)...
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


        // ����һ��ģ��.
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

        // ��֯���������壬��������ѧ��Ӧ������Һ������仯�����Բ�������Һ�����.
        List<Reactant> outflow = new List<Reactant>();
        /// �ж��Ƿ�Ҫ����ʲô��������Ϊ��ʱ����������Ϊ��������ֻ�ж�R�����. PӦ�á����䡱
        float mol_gas = 0;  // ���㳡��������mol����.
        foreach(Reactant r in inner_reactant)
        {
            if (r.state == Reactant.StateOfMatter.Gas)
                mol_gas += r.amount_mol;
        }
        float new_R = env.pressure * (volume - env.waterVol) / mol_gas / env.temperature;

        //Debug.Log("new_R: " + new_R + ", R: " + R);
        if(new_R < R - Constant.Negligible)    // �������Һ�塰���ˡ�. ��ʱ�����ǡ����ˡ������
        {
            if (outflowGas)   // �������壬���Լ���inner_reactant�еȱ�������������.
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
            else   // ����Һ��.
            {
                float delta = env.waterVol - (volume - mol_gas * R * env.temperature / env.pressure);
                foreach(Reactant r in inner_reactant)
                {
                    if(r.state == Reactant.StateOfMatter.Liquidity)
                    {
                        float reduction = delta * (r.amount_vol / env.waterVol);
                        r.AddAmountMol(-reduction);
                        Reactant outliquid = new Reactant(r.name, Reactant.StateOfMatter.Liquidity, reduction);  // �ǵ���Reactant���޸�Һ�����������Mol����vol!
                        outflow.Add(outliquid);
                    }
                    if(r.state == Reactant.StateOfMatter.Solution)
                    {
                        // Ũ�Ȳ���.
                        float reduction = delta * r.Concentration(env.waterVol);
                        r.AddAmountMol(-reduction);
                        Reactant outsolution = new Reactant(r.name, Reactant.StateOfMatter.Solution, reduction);
                        outflow.Add(outsolution);
                    }
                }
                env.waterVol -= delta;
            }

            // �޸Ķ���(��������)...�������inflow��û�����壬�����������.
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
        else   // û�б仯��û�в�������ķ�Ӧ. ���岻������Ϊ��ʱ�����Ƿ�������������ֱ�Ӳ������.
        {
            float delta = env.pressure * (volume - env.waterVol) / R / env.temperature - mol_gas;   // Ҫ�����������.
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

            if(!upperSmoke)      //���inflow��û�����壬�����������.
                smokeSystem.Stop();  
        }

        BlendLiquidColor();


        return outflow;
    }

    public override bool ValidateConnectionInPathway(PathwayEquipment equipment)
    {
        // Reactorֻ�ܺ�Connector����, ����һ�������container: �޻�.
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
    /// ����Һ��/��Һ����ɫ��ӻ��Һ����ɫ. ����Һ�������ɫΪ���ף��������ʰ������ʵ�����Ȩ��ͣ�Ȼ����ˣ��Ƿ�ı�͸���ȣ�����ֱ���������޸�Һ����ɫ.
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
                // ����Ũ����scolor��defaultColor֮���ֵ.
                float concentration = r.Concentration(env.waterVol);
                float fullColor = property.concentration == 0 ? Constant.Negligible : property.concentration;   // ��һ�������ɫ.
                float coef = Mathf.Clamp01((concentration - property.threshold) / (fullColor - property.threshold));
                // ����һ��ϡ��(����Ũ�Ȳ�ֵ)...
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
    /// ����������ɫ����ֱ�����������޸�smokeSystem����ɫ. ����������ɫ(��Ϊ�������Ҫ�����������ӿ���) \
    /// ��ϵ�ʱ��aȡ���Բ�ֵ��rgbȡ��mol��Ȩ���.
    /// </summary>
    Color BlendGasColor()
    {
        Color gasColor = Color.clear;
        List<Reactant> inner_gas = inner_reactant.Where((x) => { return x.state == Reactant.StateOfMatter.Gas; }).ToList();
        float sumGasMol = inner_gas.Select((r) => r.amount_mol).Sum();
        foreach(var r in inner_gas)
        {
            Color thisGasColor = ReactionConfig.reagent_property_list[r.reactant_id].color;
            // ���Լ���Ũ�Ⱥ���С�Ŀɺ���Ũ��֮���ֵ.
            float coef = Mathf.Clamp01((r.amount_mol - Constant.Negligible) / (fullGasColorAmount - Constant.Negligible));
            thisGasColor.a *= coef;
            float tmp_a = gasColor.a;
            gasColor += (r.amount_mol / sumGasMol) * thisGasColor;
            gasColor.a = Mathf.Max(tmp_a, thisGasColor.a);
            //gasColor += Color.Lerp(Color.clear, thisGasColor, coef);   // coefΪ0ʱ����Color.clear
        }
        return gasColor;
    }

    /// <summary>
    /// ֱ��ɾ����������е�һ����Ӧ��. ֻ�����ǹ��巴Ӧ�
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
