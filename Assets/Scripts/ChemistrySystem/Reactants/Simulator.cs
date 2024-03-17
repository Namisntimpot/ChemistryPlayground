using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 在ReactionConfig的基础上进行化学反应模拟. 每一步模拟都应该重新声明一个它然后一个一个化合物喂进去
/// </summary>
public class Simulator
{
    public static float globalInterval;
    struct State
    {
        public bool[] prob_reactions;
        public int[] satisfied_reactants_num;
        public State(int reaction_num)
        {
            prob_reactions = new bool[reaction_num];
            satisfied_reactants_num = new int[reaction_num];
        }
    }
    // 以bool[reactions_num]的0-1数组表示可能的反应的状态.
    State curState;    // 当前的状态.
    List<State> stack = new List<State>();
    List<Reactant> waiting = new List<Reactant>();
    List<Reactant> buffer = new List<Reactant>();
    Equipment.EqEnv eqenv;
    bool[] occurred;    // 表示反应已经发生过了.
    float timeDelta;

    public Simulator(Equipment.EqEnv eqenv)
    {
        curState = new State(ReactionConfig.reactions_count);
        occurred = new bool[ReactionConfig.reactions_count];
        //timeDelta = Time.deltaTime;
        timeDelta = globalInterval;
        this.eqenv = eqenv;
    }

    /// <summary>向模拟器中多投入一种反应物.</summary>
    public void Feed(Reactant reactant)
    {
        // 如果buffer中已经有了，就合并到buffer中!
        foreach(Reactant r in buffer)
        {
            if (r.reactant_id == reactant.reactant_id)
            {
                r.Merge(reactant);
                return;
            }
        }
        waiting.Add(reactant);
        Update();
    }

    void Update()
    {
        while(waiting.Count > 0)
        {
            Reactant reactant = waiting[0];
            waiting.RemoveAt(0);
            // 先进行一步状态转移.
            foreach(int reaction_id in ReactionConfig.simu_reagents[reactant.reactant_id].as_reactant)
            {
                if (!occurred[reaction_id])
                {
                    if (!curState.prob_reactions[reaction_id])
                    {
                        curState.prob_reactions[reaction_id] = true;
                    }
                    curState.satisfied_reactants_num[reaction_id] += 1;
                }
            }
            State new_state = new State(ReactionConfig.reactions_count);
            curState.prob_reactions.CopyTo(new_state.prob_reactions, 0);
            curState.satisfied_reactants_num.CopyTo(new_state.satisfied_reactants_num, 0);
            stack.Add(new_state);
            buffer.Add(reactant);

            // 然后进行反应(reduce).
            //// 先检查有没有达到反应要求的反应. 如果满足要求，就进行反应，并且回退stack，填充waiting.
            for(int i = 0; i < ReactionConfig.reactions_count; ++i)
            {
                if(curState.satisfied_reactants_num[i] == ReactionConfig.simu_reactions[i].reactants_count)
                {
                    ReactionConfig.SimuReaction reaction = ReactionConfig.simu_reactions[i];
                    // 从buffer中找到反应物. 之后可以考虑在前面(在State里)提前存起来..
                    int start_stack_index = -1;
                    List<Reactant> reactants = new List<Reactant>(curState.satisfied_reactants_num[i]);
                    for(int j=0; reactants.Count < curState.satisfied_reactants_num[i] ; ++j)
                    {
                        if (reaction.reactants_name_proportion.ContainsKey(buffer[j].name))
                        {
                            if (start_stack_index < 0)
                                start_stack_index = j;
                            reactants.Add(buffer[j]);
                        }
                    }
                    if (reaction.checkCondition(reactants, eqenv))  // 满足反应条件，进行反应.
                    {
                        // 如果是没有生成物的特殊反应(处理尾气)，就当作是直接把气体反应物消耗完！（当作是处理尾气）
                        if (reaction.products_name_proportion is null)
                        {
                            foreach (Reactant r in reactants)
                                if(r.state == Reactant.StateOfMatter.Gas)
                                    r.AddAmountMol(-r.amount_mol);
                            buffer = buffer.Where((x) => { return x.amount_mol > Constant.Negligible; }).ToList();
                        }
                        // 正常反应.
                        else
                        {
                            float speed = reaction.speed[ReactionConfig.SimuReaction.SPEED_LOW];
                            foreach (Reactant r in reactants)
                            {
                                // 暂时仅考虑，固态且时粉末的时候，用high速率.
                                if (r.state == Reactant.StateOfMatter.Solidity && r.contactArea > 0)
                                {
                                    speed = reaction.speed[ReactionConfig.SimuReaction.SPEED_HIGH];
                                    break;
                                }
                            }
                            // 先检查应该反应多少（有可能有的物质反应完了——虽然设定的速率很小，可能性很小.）.
                            float reaction_multiple = speed * timeDelta;
                            // 计算化学式的倍数.(小心直接减为0，而导致反应没有完全按照速率来的情况.)
                            foreach (Reactant r in reactants)
                            {
                                float max_multiple = r.amount_mol / reaction.reactants_name_proportion[r.name];  // 最多能反应这么多份.
                                if (max_multiple < reaction_multiple)
                                    reaction_multiple = max_multiple;
                            }
                            // 减少反应物.
                            foreach (Reactant r in reactants)
                            {
                                float reduction = reaction_multiple * reaction.reactants_name_proportion[r.name];
                                r.AddAmountMol(-reduction);
                            }
                            // 过一遍buffer，如果有物质的量为0的，就删去.
                            buffer = buffer.Where((x) => { return x.amount_mol > Constant.Negligible; }).ToList();
                            // 产生生成物. 如果buffer中有，就合并进去；如果buffer中没有，就新生成并且insert到waiting的开头.
                            List<Reactant> products = new List<Reactant>(reaction.products_count);
                            foreach ((string name, int proportion) in reaction.products_name_proportion)
                            {
                                int id = ReactionConfig.reagents_name_to_id[name];
                                ReactionConfig.ReagentProperty property = ReactionConfig.reagent_property_list[id];
                                float mol = proportion * reaction_multiple;
                                bool reactant_occurred = false;
                                foreach (Reactant r in buffer)
                                {
                                    if (r.reactant_id == id)   // 如果之前的buffer里就有，合并进去
                                    {
                                        reactant_occurred = true;
                                        r.AddAmountMol(mol);
                                        break;
                                    }
                                }
                                if (!reactant_occurred)   // 否则新建一个，插入waiting中的最前端.
                                {
                                    Reactant product;
                                    switch (property.state)
                                    {
                                        case Reactant.StateOfMatter.Liquidity:
                                            product = Reactant.Create_Liquidity(name, mol, 0);   // 反应产生的液体体积忽略不计.
                                            break;
                                        case Reactant.StateOfMatter.Solidity:
                                            product = Reactant.Create_Solidity(name, mol, 0);   // 生成固体，当作块状.
                                            break;
                                        default:
                                            product = new Reactant(name, property.state, mol);
                                            break;
                                    }
                                    waiting.Insert(0, product);
                                }
                            }
                        }
                        // 回退stack状态，把后面的压入waiting.——从start_index这个index开始回退. 也就是从start_index-1开始.
                        curState = start_stack_index - 1 >= 0 ? stack[start_stack_index - 1] : new State(ReactionConfig.reactions_count);  // 有可能从头开始.
                        stack.RemoveRange(start_stack_index, stack.Count - start_stack_index);
                        List<Reactant> removed = buffer.GetRange(start_stack_index, buffer.Count - start_stack_index);
                        waiting.InsertRange(0, removed);  // 插入waiting队列中重新状态转移.
                        buffer.RemoveRange(start_stack_index, buffer.Count - start_stack_index);
                        occurred[i] = true;   // 标记反应i已经发生过.
                        break;  // 不再检查其他反应，重新开始状态转移.
                    }
                }
            }
        }
    }

    public List<Reactant> Finish()
    {
        buffer.Sort((x, y) =>
        {
            return x.reactant_id - y.reactant_id;
        });
        return buffer;
    }
}