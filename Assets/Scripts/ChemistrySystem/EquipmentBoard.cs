using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentBoard : MonoBehaviour
{
    public bool isActive = true;
    public Transform generationPoint;

    [HideInInspector]
    public int hoveredBoardNum = 0;
    // Start is called before the first frame update

    private void Awake()
    {
        ReactionConfig.init();
    }

    void Start()
    {
        //ReactionConfig.init();    // 全局初始化.

        // 修改其儿子的脚本. 其实本来应该根据ReactionConfig的加载结果动态创建的，但简化起见，直接创建好了所有儿子. 所以直接在儿子的脚本中完成了. 所以这里只用收集gameobject.
    }

    public void SwitchActivation()
    {
        isActive = !isActive;
        int cnt = transform.childCount;
        // 我发现提前保存这个数组的尝试都失败了，报错nullexception.
        // 即，如果试图在start中提前遍历并保存儿子数组(gameobject or SingleEquipmentBoard[])，都会保存NullException
        for (int i = 0; i < cnt; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }
}
