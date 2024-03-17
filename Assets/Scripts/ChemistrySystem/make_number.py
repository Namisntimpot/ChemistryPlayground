import json
import argparse

parser = argparse.ArgumentParser(description="convert reaction configuration to state definition and state transition matrix")
parser.add_argument("--input", type=str, default="reaction.config.json")
parser.add_argument("--output", type=str, default="reagent.number.json")

class ReagentItem:
    name_to_token_id = {}
    def __init__(self, name, token_id) -> None:
        self.name = name
        self.token_id = token_id
        ReagentItem.name_to_token_id[name] = token_id

class ReactionItem:
    def __init__(self, reactants:list, products:list, id:int) -> None:
        self.reactants = reactants
        self.products = products
        self.id = id
        self.in_degree = 0
        self.adjacency = []

def main():
    args = parser.parse_args()
    with open(args.input) as f:
        config = json.load(f)
    reagents_config = config["reagents"]
    reactions_config = config["reactions"]

    # 形成reactions的邻接表
    reactions_list : list[ReactionItem] = []
    reactants_to_id = {}  # 反应物的名字->它所在的反应的id列表
    i = 0
    ## 读所有反应的反应物生成物.
    for reaction in reactions_config:
        reactants = reaction["reactants"]  # list.
        reactants_name = list(map(lambda x: x["name"], reactants))
        products = reaction.get("products", [])   # 可能当作没有生成物！
        products_name = list(map(lambda x: x["name"], products))
        reaction_item = ReactionItem(reactants_name, products_name, i)
        reactions_list.append(reaction_item)
        # 形成 反应物名字->它所在的反应的id列表, 方便之后成图.
        for rname in reactants_name:
            try:
                reactants_to_id[rname].append(i)
            except:
                reactants_to_id[rname] = [i]
        i += 1
    ## 形成反应之间的邻接表；如果反应a的生成物中有一个在反应b的反应物中，则 a->b
    for reaction in reactions_list:
        for pname in reaction.products:
            incre = reactants_to_id.get(pname, [])
            reaction.adjacency += incre
            for to_id in incre:
                reactions_list[to_id].in_degree += 1

    
    # 对反应拓扑排序，用于之后对化合物编号! **先不考虑反应成环！**
    topology_reactions = []   # 存放的是id
    queue = []
    tmp_indegree = []
    # isSorted = [False for i in range(len(reactions_list))]  先不考虑成环，所以用不上
    for reaction in reactions_list:
        if reaction.in_degree == 0:
            queue.append(reaction.id)
        tmp_indegree.append(reaction.in_degree)
    while len(queue) > 0:
        this_id = queue.pop(0)
        topology_reactions.append(this_id)
        # 减小 indegree.
        for id in reactions_list[this_id].adjacency:
            tmp_indegree[id] -= 1
            if tmp_indegree[id] == 0:
                queue.append(id)

    # 基于拓扑排序后反应的顺序，进行试剂的排序，依次按照生成物、反应物顺序;
    reagents_list:list[ReagentItem] = []
    i = 0
    for reaction in reactions_list:
        for rname in reaction.reactants:
            t = ReagentItem.name_to_token_id.get(rname)
            if t is None:
                ReagentItem.name_to_token_id[rname] = i
                reagent = ReagentItem(rname, i)
                reagents_list.append(reagent)
                i += 1
        for pname in reaction.products:
            t = ReagentItem.name_to_token_id.get(pname)
            if t is None:
                ReagentItem.name_to_token_id[pname] = i
                reagent = ReagentItem(pname, i)
                reagents_list.append(reagent)
                i += 1
    
    # 读取config中所有的试剂，对剩下的试剂也编号.
    for reagent_name in reagents_config.keys():
        t = ReagentItem.name_to_token_id.get(reagent_name)
        if t is None:
            ReagentItem.name_to_token_id[reagent_name] = i
            reagent = ReagentItem(reagent_name, i)
            reagents_list.append(reagent)
            i += 1

    # 输出：试剂编号、(反应编号就是config中的顺序)、
    ret = []
    for reagent in reagents_list:
        item = {
            "name":reagent.name,
            "as_reactant":reactants_to_id.get(reagent.name, []),
        }
        ret.append(item)
    with open(args.output, 'w') as f:
        json.dump(ret, f, indent=4)


if __name__=='__main__':
    main()