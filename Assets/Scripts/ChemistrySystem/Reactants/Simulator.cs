using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ��ReactionConfig�Ļ����Ͻ��л�ѧ��Ӧģ��. ÿһ��ģ�ⶼӦ����������һ����Ȼ��һ��һ��������ι��ȥ
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
    // ��bool[reactions_num]��0-1�����ʾ���ܵķ�Ӧ��״̬.
    State curState;    // ��ǰ��״̬.
    List<State> stack = new List<State>();
    List<Reactant> waiting = new List<Reactant>();
    List<Reactant> buffer = new List<Reactant>();
    Equipment.EqEnv eqenv;
    bool[] occurred;    // ��ʾ��Ӧ�Ѿ���������.
    float timeDelta;

    public Simulator(Equipment.EqEnv eqenv)
    {
        curState = new State(ReactionConfig.reactions_count);
        occurred = new bool[ReactionConfig.reactions_count];
        //timeDelta = Time.deltaTime;
        timeDelta = globalInterval;
        this.eqenv = eqenv;
    }

    /// <summary>��ģ�����ж�Ͷ��һ�ַ�Ӧ��.</summary>
    public void Feed(Reactant reactant)
    {
        // ���buffer���Ѿ����ˣ��ͺϲ���buffer��!
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
            // �Ƚ���һ��״̬ת��.
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

            // Ȼ����з�Ӧ(reduce).
            //// �ȼ����û�дﵽ��ӦҪ��ķ�Ӧ. �������Ҫ�󣬾ͽ��з�Ӧ�����һ���stack�����waiting.
            for(int i = 0; i < ReactionConfig.reactions_count; ++i)
            {
                if(curState.satisfied_reactants_num[i] == ReactionConfig.simu_reactions[i].reactants_count)
                {
                    ReactionConfig.SimuReaction reaction = ReactionConfig.simu_reactions[i];
                    // ��buffer���ҵ���Ӧ��. ֮����Կ�����ǰ��(��State��)��ǰ������..
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
                    if (reaction.checkCondition(reactants, eqenv))  // ���㷴Ӧ���������з�Ӧ.
                    {
                        // �����û������������ⷴӦ(����β��)���͵�����ֱ�Ӱ����巴Ӧ�������꣡�������Ǵ���β����
                        if (reaction.products_name_proportion is null)
                        {
                            foreach (Reactant r in reactants)
                                if(r.state == Reactant.StateOfMatter.Gas)
                                    r.AddAmountMol(-r.amount_mol);
                            buffer = buffer.Where((x) => { return x.amount_mol > Constant.Negligible; }).ToList();
                        }
                        // ������Ӧ.
                        else
                        {
                            float speed = reaction.speed[ReactionConfig.SimuReaction.SPEED_LOW];
                            foreach (Reactant r in reactants)
                            {
                                // ��ʱ�����ǣ���̬��ʱ��ĩ��ʱ����high����.
                                if (r.state == Reactant.StateOfMatter.Solidity && r.contactArea > 0)
                                {
                                    speed = reaction.speed[ReactionConfig.SimuReaction.SPEED_HIGH];
                                    break;
                                }
                            }
                            // �ȼ��Ӧ�÷�Ӧ���٣��п����е����ʷ�Ӧ���ˡ�����Ȼ�趨�����ʺ�С�������Ժ�С.��.
                            float reaction_multiple = speed * timeDelta;
                            // ���㻯ѧʽ�ı���.(С��ֱ�Ӽ�Ϊ0�������·�Ӧû����ȫ���������������.)
                            foreach (Reactant r in reactants)
                            {
                                float max_multiple = r.amount_mol / reaction.reactants_name_proportion[r.name];  // ����ܷ�Ӧ��ô���.
                                if (max_multiple < reaction_multiple)
                                    reaction_multiple = max_multiple;
                            }
                            // ���ٷ�Ӧ��.
                            foreach (Reactant r in reactants)
                            {
                                float reduction = reaction_multiple * reaction.reactants_name_proportion[r.name];
                                r.AddAmountMol(-reduction);
                            }
                            // ��һ��buffer����������ʵ���Ϊ0�ģ���ɾȥ.
                            buffer = buffer.Where((x) => { return x.amount_mol > Constant.Negligible; }).ToList();
                            // ����������. ���buffer���У��ͺϲ���ȥ�����buffer��û�У��������ɲ���insert��waiting�Ŀ�ͷ.
                            List<Reactant> products = new List<Reactant>(reaction.products_count);
                            foreach ((string name, int proportion) in reaction.products_name_proportion)
                            {
                                int id = ReactionConfig.reagents_name_to_id[name];
                                ReactionConfig.ReagentProperty property = ReactionConfig.reagent_property_list[id];
                                float mol = proportion * reaction_multiple;
                                bool reactant_occurred = false;
                                foreach (Reactant r in buffer)
                                {
                                    if (r.reactant_id == id)   // ���֮ǰ��buffer����У��ϲ���ȥ
                                    {
                                        reactant_occurred = true;
                                        r.AddAmountMol(mol);
                                        break;
                                    }
                                }
                                if (!reactant_occurred)   // �����½�һ��������waiting�е���ǰ��.
                                {
                                    Reactant product;
                                    switch (property.state)
                                    {
                                        case Reactant.StateOfMatter.Liquidity:
                                            product = Reactant.Create_Liquidity(name, mol, 0);   // ��Ӧ������Һ��������Բ���.
                                            break;
                                        case Reactant.StateOfMatter.Solidity:
                                            product = Reactant.Create_Solidity(name, mol, 0);   // ���ɹ��壬������״.
                                            break;
                                        default:
                                            product = new Reactant(name, property.state, mol);
                                            break;
                                    }
                                    waiting.Insert(0, product);
                                }
                            }
                        }
                        // ����stack״̬���Ѻ����ѹ��waiting.������start_index���index��ʼ����. Ҳ���Ǵ�start_index-1��ʼ.
                        curState = start_stack_index - 1 >= 0 ? stack[start_stack_index - 1] : new State(ReactionConfig.reactions_count);  // �п��ܴ�ͷ��ʼ.
                        stack.RemoveRange(start_stack_index, stack.Count - start_stack_index);
                        List<Reactant> removed = buffer.GetRange(start_stack_index, buffer.Count - start_stack_index);
                        waiting.InsertRange(0, removed);  // ����waiting����������״̬ת��.
                        buffer.RemoveRange(start_stack_index, buffer.Count - start_stack_index);
                        occurred[i] = true;   // ��Ƿ�Ӧi�Ѿ�������.
                        break;  // ���ټ��������Ӧ�����¿�ʼ״̬ת��.
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