# ChemistryPlayground
试图在一定程度上模拟简单的高中化学实验，并且提供尽可能高的自由度。这里选择的是浓硝酸和铜反应。  

实现了一个简单(并且不完整)的化学反应计算器，实现了一系列Equipment脚本，能够组装简单的化学反应通路(单线、单向、不逆向)，然后进行化学反应计算。  

# 化学反应计算
受启发于 LR parser 实现的化学反应计算器。所有涉及的试剂、化学反应在 Reaction.config.json 中定义，这些化学反应根据反应物和生成物的关系形成图，拓扑排序后（未处理成环情况）以此顺序对试剂进行排序，形成 Reactants.number.json。  

模拟过程中将容器内的化学物质一个一个按照编号顺序输入化学反应计算器中，化学反应计算器根据config里的定义进行类似于LR parser的状态转移和reduce。  

# 化学反应通路和仪器
发生反应的场所被抽象为通路(Pathway)，Pathway中暂时有三种成分: 反应器(Reactor), 连接件(Connector) 和 塞子(Plug)，大体是 Reactor - Plug - Connector - (Connector*) - Reactor - Plug ... 的顺序，只考虑化学反应单向进行，并且只考虑前面的反应产生气体或液体输入到后面，不考虑前面的反应吸收气体要从后面的容器中吸收气体和液体进来。——尽管我的实现确实是基于 压强、温度 等进行气体、液体转移计算的，但并没有考虑将通路反过来的情况。  

化学仪器脚本层次为：  
+ Equipment  
  仪器脚本基类，单独它自己也有用。主要规定了一些基本性质，和用于仪器组装。  
  + PathwayEquipment(abstract)  
    反应通路仪器的父类，主要封装了和模拟器 Pathway 打交道的部分，暴露Step接口，供Pathway进行一步模拟。  
    + Container(Reactor)
      反应容器也就是反应器。它在Step函数中实例化一个Simulator计算器进行一步模拟，然后将向外输出的气体或者液体返回给Pathway。  
    + Tube(Connector or Plug)
  + ReagentBottle  
    试剂瓶。能够像Container中倒反应物.  
  + Burner
    可以加热别的物体。  
  + SolidReagentEquipment
    可以向Container中添加固体反应物。  

液体模拟用 Zibra Liquids 实现（Zibra Liquids的效果在液体的Collider随着父物体旋转的时候会出问题，应该让容器竖直着放置），气体、烟雾、火焰用粒子简单模拟其效果。  

# 交互
组装主要用碰撞箱和吸附完成。吸附的旋转对的不是很好(因为没太搞懂四元数什么的)，而碰撞箱在多了之后其触发行为就变得很怪异，加了很多判断来避免意外的触发，但组装这方面做的确实不好。  

其他有功能的仪器，除了ReagentBottle要拿起来之后按(一次，注意不是按住)Primary Button来开始倾倒之外，其他都是手柄射线指着仪器(hover)后按下Primary Button。将铜丝(SolidReagentEquipment)放在Container上后，hover它按下Primary Button才向Container中加入这个固体反应物。  

# 其他
原意是提供尽可能高的可自定义性，也就是希望所有化学反应模拟也游戏本身都基于 Reaction.config.json 生成（也就是，只用修改Reaction.config.json的内容，就能修改成不同的实验和不同的现象），实际上化学反应计算已经完全基于这个文件了，但场景并不完全根据它，只是部分实现自定义。  
而且化学反应现象毕竟很多，像析出固体什么的，本项目只涉及寥寥溶液变色、生成气体什么的，想要真正自定义需要更多的抽象和更强大通用的框架。  