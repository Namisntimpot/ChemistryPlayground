using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reactant
{
    // **修改**：只有液体(水)才考虑体积，溶液只记录溶质的mol，之后动态计算concentration!!

    static string NIL = "NULL";  // 一个空的，只是占位.
    /// <summary>溶液是为了方便而来的，一个溶液当成只有一种溶质。溶剂在reactant里只记录溶质的mol量，浓度要输入env中液体体积来给出.</summary>
    public enum StateOfMatter
    {
        Solidity, Liquidity, Gas, Solution,
    }

    public enum Form       // 其实应该是块状、粉末，但是因为只有铜丝，所以用wire..
    {
        none, wire, powder,
    }

    public StateOfMatter state;
    public Form form = Form.none;    // 暂时只对固体有用.
    public string name;
    public int reactant_id = -1;        // 这种物质在化学反应图中的节点id, -1表示NULL，没有.
    public float amount_mol = 0;   // 以摩尔表示的量. 当是溶液的时候，这个值表示唯一溶质的物质的量
    // public float concentration = 0;    // 给溶液用的, mol/L. 浓度动态计算，溶液只存溶质.
    public float amount_vol = 0;   // 给溶液用的，L.
    public float contactArea = 0;  // 只对固体有效，影响反应速率，只有高(粉末)低(块)两类. 大于0表示粉末.

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
    /// <summary>给初始化固体用的，contactArea大于0表示粉末</summary>
    public static Reactant Create_Solidity(string name, float amount, float contaceArea)
    {
        Reactant reactant = new Reactant(name, StateOfMatter.Solidity, amount);
        reactant.contactArea = contaceArea;
        reactant.form = contaceArea > 0.0 ? Form.powder : Form.wire;
        return reactant;
    }
    /// <summary>给初始化溶液用的<\summary>
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

    /// <summary>添加药品物质的量(in mol)，可正可负</summary>
    public void AddAmountMol(float incre_mol)
    {
        amount_mol += incre_mol;
        // 因摩尔数变化导致的固体液体体积变化不计。气体的体积跟着环境来，不管.
    }

    /// <summary>如果是液体，还合并体积！</summary>
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
                contactArea = Mathf.Max(contactArea, reactant.contactArea);   // 用更大的那个
                break;
        }
    }
}
