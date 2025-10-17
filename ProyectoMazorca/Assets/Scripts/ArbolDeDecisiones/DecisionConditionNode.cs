public class DecisionConditionNode : DecisionTreeNode
{
    public System.Func<MovmientoNPC, bool> condition;
    public DecisionTreeNode trueNode;
    public DecisionTreeNode falseNode;

    public DecisionConditionNode(System.Func<MovmientoNPC, bool> condition, DecisionTreeNode trueNode, DecisionTreeNode falseNode)
    {
        this.condition = condition;
        this.trueNode = trueNode;
        this.falseNode = falseNode;
    }

    public override void Evaluate(MovmientoNPC npc)
    {
        if (condition(npc))
            trueNode.Evaluate(npc);
        else
            falseNode.Evaluate(npc);
    }
}
