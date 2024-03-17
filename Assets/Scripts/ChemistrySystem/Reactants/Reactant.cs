using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reactant
{
    // **�޸�**��ֻ��Һ��(ˮ)�ſ����������Һֻ��¼���ʵ�mol��֮��̬����concentration!!

    static string NIL = "NULL";  // һ���յģ�ֻ��ռλ.
    /// <summary>��Һ��Ϊ�˷�������ģ�һ����Һ����ֻ��һ�����ʡ��ܼ���reactant��ֻ��¼���ʵ�mol����Ũ��Ҫ����env��Һ�����������.</summary>
    public enum StateOfMatter
    {
        Solidity, Liquidity, Gas, Solution,
    }

    public enum Form       // ��ʵӦ���ǿ�״����ĩ��������Ϊֻ��ͭ˿��������wire..
    {
        none, wire, powder,
    }

    public StateOfMatter state;
    public Form form = Form.none;    // ��ʱֻ�Թ�������.
    public string name;
    public int reactant_id = -1;        // ���������ڻ�ѧ��Ӧͼ�еĽڵ�id, -1��ʾNULL��û��.
    public float amount_mol = 0;   // ��Ħ����ʾ����. ������Һ��ʱ�����ֵ��ʾΨһ���ʵ����ʵ���
    // public float concentration = 0;    // ����Һ�õ�, mol/L. Ũ�ȶ�̬���㣬��Һֻ������.
    public float amount_vol = 0;   // ����Һ�õģ�L.
    public float contactArea = 0;  // ֻ�Թ�����Ч��Ӱ�췴Ӧ���ʣ�ֻ�и�(��ĩ)��(��)����. ����0��ʾ��ĩ.

    public Reactant()
    {
        name = NIL;
        state = StateOfMatter.Solidity;
    }
    public Reactant(string name, StateOfMatter state, float amount)
    {
        //Debug.Log("Try to create: " + name);
        this.name = name; this.state = state; this.amount_mol = amount;
        reactant_id = ReactionConfig.reagents_name_to_id[name];
    }
    /// <summary>����ʼ�������õģ�contactArea����0��ʾ��ĩ</summary>
    public static Reactant Create_Solidity(string name, float amount, float contaceArea)
    {
        Reactant reactant = new Reactant(name, StateOfMatter.Solidity, amount);
        reactant.contactArea = contaceArea;
        reactant.form = contaceArea > 0.0 ? Form.powder : Form.wire;
        return reactant;
    }
    /// <summary>����ʼ����Һ�õ�<\summary>
    public static Reactant Create_Liquidity(string name, float amount_mol, float volume_L)
    {
        Reactant reactant = new Reactant(name, StateOfMatter.Liquidity, amount_mol);
        reactant.amount_vol = volume_L;
        return reactant;
    }

    public float Concentration(float volume)
    {
        return amount_mol / volume;
    }

    /// <summary>���ҩƷ���ʵ���(in mol)�������ɸ�</summary>
    public void AddAmountMol(float incre_mol)
    {
        amount_mol += incre_mol;
        // ��Ħ�����仯���µĹ���Һ������仯���ơ������������Ż�����������.
    }

    /// <summary>�����Һ�壬���ϲ������</summary>
    public void Merge(Reactant reactant)
    {
        if (reactant.reactant_id != reactant_id)
            return;
        amount_mol += reactant.amount_mol;
        switch (state)
        {
            case StateOfMatter.Liquidity:
                amount_vol += reactant.amount_vol;
                break;
            case StateOfMatter.Solidity:
                contactArea = Mathf.Max(contactArea, reactant.contactArea);   // �ø�����Ǹ�
                break;
        }
    }
}
