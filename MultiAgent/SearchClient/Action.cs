using System.Collections.Generic;

namespace MultiAgent.SearchClient
{
    public class Action
    {
        public readonly string Name;
        public readonly ActionType Type;
        public readonly int AgentRowDelta;
        public readonly int AgentColumnDelta;
        public readonly int BoxRowDelta;
        public readonly int BoxColumnDelta;

        public static Action NoOp { get; } = new Action("NoOp", ActionType.NoOp, 0, 0, 0, 0);

        public static readonly List<Action> AllActions = new()
        {
            //NoOp
            new Action("NoOp", ActionType.NoOp, 0, 0, 0, 0),

            //Move
            new Action("Move(N)", ActionType.Move, -1, 0, 0, 0),
            new Action("Move(S)", ActionType.Move, 1, 0, 0, 0),
            new Action("Move(E)", ActionType.Move, 0, 1, 0, 0),
            new Action("Move(W)", ActionType.Move, 0, -1, 0, 0),

            //Push
            new Action("Push(N,N)", ActionType.Push, -1, 0, -1, 0),
            new Action("Push(N,W)", ActionType.Push, -1, 0, 0, -1),
            new Action("Push(N,E)", ActionType.Push, -1, 0, 0, 1),

            new Action("Push(S,S)", ActionType.Push, 1, 0, 1, 0),
            new Action("Push(S,W)", ActionType.Push, 1, 0, 0, -1),
            new Action("Push(S,E)", ActionType.Push, 1, 0, 0, 1),

            new Action("Push(E,E)", ActionType.Push, 0, 1, 0, 1),
            new Action("Push(E,N)", ActionType.Push, 0, 1, -1, 0),
            new Action("Push(E,S)", ActionType.Push, 0, 1, 1, 0),

            new Action("Push(W,W)", ActionType.Push, 0, -1, 0, -1),
            new Action("Push(W,N)", ActionType.Push, 0, -1, -1, 0),
            new Action("Push(W,S)", ActionType.Push, 0, -1, 1, 0),

            //Pull
            new Action("Pull(N,N)", ActionType.Pull, -1, 0, -1, 0),
            new Action("Pull(N,W)", ActionType.Pull, -1, 0, 0, -1),
            new Action("Pull(N,E)", ActionType.Pull, -1, 0, 0, 1),

            new Action("Pull(S,S)", ActionType.Pull, 1, 0, 1, 0),
            new Action("Pull(S,W)", ActionType.Pull, 1, 0, 0, -1),
            new Action("Pull(S,E)", ActionType.Pull, 1, 0, 0, 1),

            new Action("Pull(E,E)", ActionType.Pull, 0, 1, 0, 1),
            new Action("Pull(E,N)", ActionType.Pull, 0, 1, -1, 0),
            new Action("Pull(E,S)", ActionType.Pull, 0, 1, 1, 0),

            new Action("Pull(W,W)", ActionType.Pull, 0, -1, 0, -1),
            new Action("Pull(W,N)", ActionType.Pull, 0, -1, -1, 0),
            new Action("Pull(W,S)", ActionType.Pull, 0, -1, 1, 0),
        };

        public Action(string name, ActionType type, int agentRowDelta, int agentColumnDelta, int boxRowDelta,
            int boxColumnDelta)
        {
            Name = name;
            Type = type;
            AgentRowDelta = agentRowDelta;
            AgentColumnDelta = agentColumnDelta;
            BoxRowDelta = boxRowDelta;
            BoxColumnDelta = boxColumnDelta;
        }

        public Action(Action action)
        {
            Name = action.Name;
            Type = action.Type;
            AgentRowDelta = action.AgentRowDelta;
            AgentColumnDelta = action.AgentColumnDelta;
            BoxRowDelta = action.BoxRowDelta;
            BoxColumnDelta = action.BoxColumnDelta;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum ActionType
    {
        NoOp,
        Move,
        Push,
        Pull,
    }
}
