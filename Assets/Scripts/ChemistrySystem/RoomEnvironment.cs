using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 暂时不考虑保存逸散到room中的物体，故也不考虑这些气体的消散。只是根据Emit来的气体打印警告.
/// </summary>
public class RoomEnvironment : MonoBehaviour
{
    public static int ventilation = 0;
    public float gasDisappearSpeed = 0.01f;  // mol/s.

    [SerializeField]
    TextMeshProUGUI blackboard;

    List<Reactant> gasEnvironment = new List<Reactant>(2);
    int air_id;

    private void Start()
    {
        Reactant air = new Reactant("air", Reactant.StateOfMatter.Gas, 100000000f);
        air_id = air.reactant_id;
        gasEnvironment.Add(air);
    }

    // 因为暂时不考虑保留气体，所以也不考虑气体衰减.
    //private void Update()
    //{
        
    //}

    public void Emit(List<Reactant> reactants)
    {
        // 暂时只考虑气体. 暂时只看有害性进行警告.
        foreach(var r in reactants)
        {
            // 报警：xxx泄露！
            // 。。。。。。
            // ------------

            if(r.state != Reactant.StateOfMatter.Solidity && r.reactant_id != air_id)
            {
                string identification = r.name + '_' + r.state.ToString() + "_none";
                string log = "\nLeaking: <color=\"purple\"><b>" + r.name + "</b></color>";
                if (ReactionConfig.reagents_identification_name_to_property[identification].harm)
                {
                    // 报警：有害气体泄露...
                    log += " It's <b>Toxic, <color=\"red\">DANGEROUS</color></b>!";
                    if(ventilation == 0)
                    {
                        log += "\nOpen Windows right now.";
                    }
                }
                else
                {
                    log += " Luckily it's <b>nontoxic. <color=\"yellow\">WARNING</color></b>!";
                }
                blackboard.text += log;
            }
        }
        //int i, j;
        //for (i = j = 0; i < gasEnvironment.Count && j < reactants.Count; ++i)
        //{
        //    if (gasEnvironment[i].reactant_id == reactants[j].reactant_id)
        //    {
        //        gasEnvironment[i].Merge(reactants[j++]);
        //    }
        //    else if (gasEnvironment[i].reactant_id > reactants[j].reactant_id)
        //    {
        //        gasEnvironment.Insert(i, reactants[j++]);
        //        i += 1;
        //    }
        //}
        //for (; j < reactants.Count; ++j)
        //{
        //    gasEnvironment.Add(reactants[j]);
        //}
    }
}
