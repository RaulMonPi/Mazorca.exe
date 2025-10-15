public class DecisionActionNode : DecisionTreeNode
{
    public System.Action<MovmientoNPC> action;

    public DecisionActionNode(System.Action<MovmientoNPC> action)
    {
        this.action = action;
    }

    public override void Evaluate(MovmientoNPC npc)
    {
        action(npc);
    }
}
