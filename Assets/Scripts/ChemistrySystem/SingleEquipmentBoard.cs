using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SingleEquipmentBoard : MonoBehaviour
{
    static readonly string prefabResourcesRoot = "Prefab/";
    static readonly string textureResourcesRoot = "Texture/";

    public string identification_name;
    public bool isEquipment = true;
    [SerializeField]
    Color hoverColor = Color.blue;
    Color originColor;
    int baseColorId;
    EquipmentBoard parentBoard;
    
    Material mat;
    private void Start()
    {
        mat = GetComponent<Renderer>().material;   // �Ҽǵ���ô��ȡ֮��Ͳ��Ṳ�������.
        baseColorId = Shader.PropertyToID("_BaseColor");
        originColor = mat.GetColor(baseColorId);
        // ��ʼ���޸�����.
        string textureResourcePath = textureResourcesRoot + name;
        Texture tex = Resources.Load<Texture>(textureResourcePath);
        int tex_id = Shader.PropertyToID("_BaseMap");
        mat.SetTexture(tex_id, tex);

        parentBoard = transform.parent.GetComponent<EquipmentBoard>();
    }

    public void onControllerEnter()
    {
        mat.SetColor(baseColorId, hoverColor);
        parentBoard.hoveredBoardNum += 1;
    }
    public void onControllerExit()
    {
        mat.SetColor(baseColorId, originColor);
        parentBoard.hoveredBoardNum -= 1;
    }

    public void onControllerSelect()
    {
        CreatePrefab();
    }

    void CreatePrefab()
    {
        if (parentBoard.hoveredBoardNum != 1)   // ֻ����ֻѡ����1����ʱ������.
            return;
        GameObject instance;
        Vector3 pos = parentBoard.generationPoint.position;
        pos.y += 0.2f;
        if (identification_name.Contains('_'))  // �ǻ����Ҫ��Resources����ReagentBottle��������.
        {
            if (identification_name.Contains(Reactant.StateOfMatter.Liquidity.ToString()) || identification_name.Contains(Reactant.StateOfMatter.Solution.ToString()))
            {
                string prefabPath = prefabResourcesRoot + "ReagentBottleSketch";
                GameObject sketch = Resources.Load<GameObject>(prefabPath);
                instance = Instantiate(sketch);
                instance.transform.position = pos;
                // �޸���ɫ!
                Material mat = instance.transform.GetChild(0).GetComponent<Renderer>().material; // ReagentBottle/bottle/fakeliquid
                SetFakeLiquidColor(mat);

                ReagentBottle reagentBottle = instance.GetComponent<ReagentBottle>();
                reagentBottle.identification_name = identification_name;
                reagentBottle.reactant_name = identification_name.Split('_')[0];
                reagentBottle.state = identification_name.Contains(Reactant.StateOfMatter.Liquidity.ToString()) ? Reactant.StateOfMatter.Liquidity : Reactant.StateOfMatter.Solution;
            }
            // ͭ˿��ͭ��., û��Gas.
            else if (identification_name.Contains(Reactant.StateOfMatter.Solidity.ToString()))
            {
                string prefabPath = prefabResourcesRoot + identification_name;
                instance = Instantiate(Resources.Load<GameObject>(prefabPath));
                instance.transform.position = pos;
                SolidReagentEquipment solid = instance.GetComponent<SolidReagentEquipment>();
                solid.reagent_name = identification_name.Split('_')[0];
                solid.eqname = identification_name;
            }
        }
        else   // ������.
        {
            string prefabPath = prefabResourcesRoot + identification_name;
            GameObject equipment = Resources.Load<GameObject>(prefabPath);
            instance = Instantiate(equipment);  // Ӧ��������ѡ�ϲŶ�...
            instance.transform.position = pos;
        }
        // �����instance��ѡ��.
    }

    void SetFakeLiquidColor(Material mat)
    {
        Color color = ReactionConfig.reagents_identification_name_to_property[identification_name].color;
        color = color == Color.clear ? Color.white : color;
        string[] color_names = new string[]
        {
            "_FoamColor", "_BottomColor", "_TopColor", "_Rim_Color"
        };
        foreach (string colorname in color_names)
        {
            int id = Shader.PropertyToID(colorname);
            mat.SetColor(id, color);
        }
    }
}
