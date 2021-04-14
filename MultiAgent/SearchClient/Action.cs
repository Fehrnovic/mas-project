using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public class Action
    {
        public readonly string Name;
        public readonly ActionType Type;
        public readonly int AgentRowDelta;
        public readonly int AgentColDelta;
        public readonly int BoxRowDelta;
        public readonly int BoxColDelta;
        public static List<Action> AllActions = new List<Action>()
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

        public Action(string name, ActionType type, int agentRowDelta, int agentColDelta, int boxRowDelta, int boxColDelta)
        {
            Name = name;
            Type = type;
            AgentRowDelta = agentRowDelta;
            AgentColDelta = agentColDelta;
            BoxRowDelta = boxRowDelta;
            BoxColDelta = boxColDelta;
        }
    }

    public enum ActionType
    {
        NoOp,
        Move,
        Push,
        Pull
    }
}
