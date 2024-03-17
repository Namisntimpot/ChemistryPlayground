using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class Constant
{
    public static readonly float MolarVolumeOfGas = 22.4f;  // L/mol
    public static readonly float AtmosphericPressure = 101.32f;  // in kPa
    public static readonly float RoomTemperature = 290;    // in K.
    public static readonly float Negligible = 1e-6f;       // 忽略不计
}

public static class ReactionConfig
{
    public struct ReagentProperty
    {
        public string name;
        public int id;
        public Reactant.StateOfMatter state;
        public Color color;
        public bool harm;
        public float threshold;   // 变为无色的临界浓度—— for solution.
        public float concentration;
        public bool provided;
        public Reactant.Form form;   // 仅给固体用.

        /// <summary>
        /// 从名字(单纯的名字)、物态、形式 到识别名：名字_物态_形式.
        /// </summary>
        public static string GetIdentificationName(string name, Reactant.StateOfMatter state, Reactant.Form form)
        {
            return name + "_" + state.ToString() + "_" + form.ToString();
        }
    }

    // simu前缀表示在仿真计算的时候用
    public struct SimuReagent{   // 仿真时候用的simureagents.
        public string name;
        public int id;
        public List<int> as_reactant;
    }

    public struct SimuReaction  // 仿真时候用的化学反应的性质.
    {
        public static readonly int SPEED_LOW = 0, SPEED_HIGH = 1;
        public int id;
        public Dictionary<string, int> reactants_name_proportion;   // 改用字典!!
        public Dictionary<string, int> products_name_proportion;
        public float[] speed;  // [low, high]
        public JObject conditions;
        public int reactants_count { get { return reactants_name_proportion.Count; } }
        public int products_count { get { return products_name_proportion == null ? 0 : products_name_proportion.Count; } }

        public bool checkCondition(List<Reactant> reactants, Equipment.EqEnv eqenv)
        {
            if (conditions is null)
                return true;
            // 这样有点低效...
            if (conditions.Property("heat") != null)
            {
                float required = conditions.Property("heat").ToObject<float>();
                if (eqenv.temperature < required)
                    return false;
            }
            if (conditions.Property("pressure") != null)
            {
                float required = conditions.Property("pressure").ToObject<float>();
                if (eqenv.pressure < required)
                    return false;
            }
            foreach(Reactant reactant in reactants)
            {
                if (conditions.Property(reactant.name) != null)
                {
                    JObject cond = conditions[reactant.name].ToObject<JObject>();
                    // 单一反应物的限定，暂时只考虑浓度 
                    if (cond.Property("concentration") != null)
                    {
                        //Debug.Log("Concentration Check: " + reactant.name + " (" + reactant.Concentration(eqenv.waterVol) + "), with water "+eqenv.waterVol + " L");
                        float required = cond["concentration"].ToObject<float>();
                        if (reactant.Concentration(eqenv.waterVol) < required)
                            return false;
                    }
                    // ... 其他可能的对反应物的限定... 目前暂不考虑.
                }
            }
            return true;
        }
    }


    static string config_path = "Assets/Scripts/ChemistrySystem/reaction.config.json";
    static string number_path = "Assets/Scripts/ChemistrySystem/reagent.number.json";
    public static int reagents_count { get { return simu_reagents.Count; } }
    public static int reactions_count { get { return simu_reactions.Count; } }
    public static List<SimuReagent> simu_reagents;
    public static List<ReagentProperty> reagent_property_list;
    public static Dictionary<string, int> reagents_name_to_id;
    public static Dictionary<string, ReagentProperty> reagents_identification_name_to_property;   // 从识别名到它固定的property.

    public static List<SimuReaction> simu_reactions;

    static bool initialized = false;
    public static void init()
    {
        if (initialized)
            return;
        // 应该把对json的解析分离开来!!

        // 读取json内容形成JObject
        string json_number = File.ReadAllText(number_path);
        string json_config = File.ReadAllText(config_path);
        JArray reagent_number = JArray.Parse(json_number);
        JObject config = JObject.Parse(json_config);
        JArray reactions_config = JArray.Parse(config["reactions"].ToString());
        JObject reagents_config = JObject.Parse(config["reagents"].ToString());

        // 新建成员变量
        simu_reagents = new List<SimuReagent>(reagent_number.Count);
        reagent_property_list = new List<ReagentProperty>(reagent_number.Count);
        reagents_name_to_id = new Dictionary<string, int>(reagent_number.Count);
        simu_reactions = new List<SimuReaction>(reactions_config.Count);
        reagents_identification_name_to_property = new Dictionary<string, ReagentProperty>(reagents_config.Count);

        // 加载模拟时所需.
        for(int i=0; i<reagent_number.Count; ++i)
        {
            var item = reagent_number[i];
            string name = item["name"].ToObject<string>();
            List<int> as_reactant = item["as_reactant"].ToObject<List<int>>();
            SimuReagent simuReagent = new SimuReagent() { name = name, as_reactant = as_reactant, id = i };
            simu_reagents.Add(simuReagent);
            reagents_name_to_id.Add(name, i);
            //Debug.Log("Add: " + name + ", " + i.ToString());
        }
        for(int i=0; i<reactions_config.Count; ++i)
        {
            var item = reactions_config[i].ToObject<JObject>();
            JArray reactants = JArray.Parse(item["reactants"].ToString());
            Dictionary<string, int> reactants_name_proportion = new Dictionary<string, int>(reactants.Count);
            foreach(var j in reactants)
            {
                reactants_name_proportion.Add(j["name"].ToObject<string>(), j["proportion"].ToObject<int>());
            }
            JObject conditions = item.Property("conditions") is null ? null : JObject.Parse(item["conditions"].ToString());
            JArray products = item.Property("products")!=null ? JArray.Parse(item["products"].ToString()) : null;
            Dictionary<string,int> products_name_proportion = products != null ? new Dictionary<string, int>() : null; 
            if(products != null)
            {
                foreach(var j in products)
                {
                    products_name_proportion.Add(j["name"].ToObject<string>(), j["proportion"].ToObject<int>());
                }
            }
            List<float> speed = item.Property("speed") is null ? null : item["speed"].ToObject<List<float>>();
            SimuReaction simuReaction = new SimuReaction()
            {
                id = i,
                reactants_name_proportion = reactants_name_proportion,
                products_name_proportion = products_name_proportion, conditions=conditions, speed = speed is not null ? speed.ToArray() : null
            };
            simu_reactions.Add(simuReaction);
        }

        // 加载每种试剂的属性所需...
        for(int i=0; i<simu_reagents.Count; ++i)
        {
            string name = simu_reagents[i].name;
            JArray jarr = reagents_config[name].ToObject<JArray>();
            bool first = true;
            for (int j = 0; j < jarr.Count; ++j)
            {
                JObject property = reagents_config[name][j].ToObject<JObject>();   // reagents_confit[name]之后是个数组.
                Reactant.StateOfMatter state = (Reactant.StateOfMatter)System.Enum.Parse(typeof(Reactant.StateOfMatter), property["state"].ToString());
                List<float> color = property["color"].ToObject<List<float>>();
                bool harm = property["harm"].ToObject<bool>();
                float threshold = state == Reactant.StateOfMatter.Solution ? property["threshold"].ToObject<float>() : 0;
                float concentration = property.Property("concentration") is null ? 0.0f : property["concentration"].ToObject<float>();
                bool provided = property["provided"].ToObject<bool>();
                Reactant.Form form = property.Property("form") is null ? Reactant.Form.none : property["form"].ToObject<Reactant.Form>();
                ReagentProperty reagentProperty = new ReagentProperty()
                {
                    name = name,
                    id = i,
                    harm = harm,
                    state = state,
                    threshold = threshold,
                    color = color.Count == 4 ? new Color(color[0] / 255, color[1] / 255, color[2] / 255, color[3] / 255) : Color.clear,
                    concentration = concentration,
                    provided = provided,
                    form = form
                };
                if (first)
                {
                    reagent_property_list.Add(reagentProperty);
                    first = false;
                }
                string identification = ReagentProperty.GetIdentificationName(name, state, form);
                //Debug.Log(identification);
                reagents_identification_name_to_property.Add(identification, reagentProperty);
            }
        }

        initialized = true;
    }
}
