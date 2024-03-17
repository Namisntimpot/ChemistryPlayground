using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// ��ʱ�����Ǳ�����ɢ��room�е����壬��Ҳ��������Щ�������ɢ��ֻ�Ǹ���Emit���������ӡ����.
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

    // ��Ϊ��ʱ�����Ǳ������壬����Ҳ����������˥��.
    //private void Update()
    //{
        
    //}

    public void Emit(List<Reactant> reactants)
    {
        // ��ʱֻ��������. ��ʱֻ���к��Խ��о���.
        foreach(var r in reactants)
        {
            // ������xxxй¶��
            // ������������
            // ------------

            if(r.state != Reactant.StateOfMatter.Solidity && r.reactant_id != air_id)
            {
                string identification = r.name + '_' + r.state.ToString() + "_none";
                string log = "\nLeaking: <color=\"purple\"><b>" + r.name + "</b></color>";
                if (ReactionConfig.reagents_identification_name_to_property[identification].harm)
                {
                    // �������к�����й¶...
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
