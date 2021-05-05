using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.CBS
{
    public static class LevelDelegationHelper
    {
        public static List<AgentDelegationHelper> AgentDelegationHelpers { get; set; }
        public static List<BoxDelegationHelper> BoxDelegationHelpers { get; set; }
        public static List<BoxGoalDelegationHelper> BoxGoalDelegationHelpers { get; set; }

        public static LevelDelegation LevelDelegation { get; set; }

        public static void InitializeDelegationData()
        {
            AgentDelegationHelpers = new List<AgentDelegationHelper>();
            BoxDelegationHelpers = new List<BoxDelegationHelper>();
            BoxGoalDelegationHelpers = new List<BoxGoalDelegationHelper>();

            foreach (var boxGoal in Level.BoxGoals)
            {
                var boxGoalDelegation = new BoxGoalDelegationHelper(boxGoal);
                BoxGoalDelegationHelpers.Add(boxGoalDelegation);

                var boxesMatchingGoal = Level.Boxes.Where(b => b.Letter == boxGoal.Letter).ToList();
                foreach (var box in boxesMatchingGoal)
                {
                    BoxDelegationHelper boxDelegation = BoxDelegationHelpers.FirstOrDefault(b => b.BoxReference == box);

                    if (boxDelegation == null)
                    {
                        boxDelegation = new BoxDelegationHelper(box);
                        BoxDelegationHelpers.Add(boxDelegation);
                    }

                    var startNode = Level.Graph.NodeGrid[box.GetInitialLocation().Row, box.GetInitialLocation().Column];
                    var endNode = Level.Graph.NodeGrid[boxGoal.GetInitialLocation().Row,
                        boxGoal.GetInitialLocation().Column];

                    int cost = Level.Graph.BFS(startNode, endNode);
                    if (cost < int.MaxValue)
                    {
                        boxGoalDelegation.ReachableBoxes.Add((boxDelegation, cost));
                        boxDelegation.ReachableBoxGoals.Add((boxGoalDelegation, cost));
                    }

                    var agentsMatchingBox = Level.Agents.Where(a => a.Color == box.Color).ToList();
                    foreach (var agent in agentsMatchingBox)
                    {
                        AgentDelegationHelper agentDelegation =
                            AgentDelegationHelpers.FirstOrDefault(a => a.AgentReference == agent);

                        if (agentDelegation == null)
                        {
                            agentDelegation = new AgentDelegationHelper(agent);
                            AgentDelegationHelpers.Add(agentDelegation);
                        }

                        startNode = Level.Graph.NodeGrid[box.GetInitialLocation().Row, box.GetInitialLocation().Column];
                        endNode = Level.Graph.NodeGrid[agent.GetInitialLocation().Row,
                            agent.GetInitialLocation().Column];

                        cost = Level.Graph.BFS(startNode, endNode);
                        if (cost < int.MaxValue)
                        {
                            if (!agentDelegation.ReachableBoxes.Exists(b => b.boxDelegationHelper.BoxReference == box))
                            {
                                agentDelegation.ReachableBoxes.Add((boxDelegation, cost));
                            }

                            if (!boxDelegation.ReachableAgents.Exists(a =>
                                a.agentDelegationHelper.AgentReference == agent))
                            {
                                boxDelegation.ReachableAgents.Add((agentDelegation, cost));
                            }
                        }
                    }
                }
            }
        }

        public static void DelegateLevel()
        {
            var levelDelegation = new LevelDelegation();

            var usedBoxes = new List<Box>();

            foreach (var boxGoalHelper in BoxGoalDelegationHelpers)
            {
                // If no agent exist to move this box - skip this
                if (!Level.Agents.Exists(a => a.Color == boxGoalHelper.BoxGoalReference.Color))
                {
                    continue;
                }

                var closestBoxHelper =
                    boxGoalHelper.ReachableBoxes.Where(b => !usedBoxes.Contains(b.boxDelegationHelper.BoxReference))
                        .ToList()
                        .Aggregate((currentBox, box) =>
                            currentBox.boxDelegationHelper == null || currentBox.cost > box.cost ? box : currentBox);

                usedBoxes.Add(closestBoxHelper.boxDelegationHelper.BoxReference);

                var closestAgent =
                    closestBoxHelper.boxDelegationHelper.ReachableAgents.Aggregate((currentAgent, agent) =>
                        currentAgent.agentDelegationHelper == null || currentAgent.cost > agent.cost
                            ? agent
                            : currentAgent);

                levelDelegation.AgentToBoxes[closestAgent.agentDelegationHelper.AgentReference]
                    .Add(closestBoxHelper.boxDelegationHelper.BoxReference);
                levelDelegation.AgentToBoxGoals[closestAgent.agentDelegationHelper.AgentReference]
                    .Add(boxGoalHelper.BoxGoalReference);
            }

            var unusedBoxes = Level.Boxes.Except(usedBoxes).ToList();

            unusedBoxes.ForEach(b =>
            {
                var agentMatchingBox = Level.Agents.FirstOrDefault(a => a.Color == b.Color);

                if (agentMatchingBox != null)
                {
                    levelDelegation.AgentToBoxes[agentMatchingBox].Add(b);
                }
            });

            LevelDelegation = levelDelegation;
        }
    }

    public class LevelDelegation
    {
        public Dictionary<Agent, List<Box>> AgentToBoxes { get; set; }
        public Dictionary<Agent, List<Box>> AgentToBoxGoals { get; set; }
        
        public LevelDelegation()
        {
            AgentToBoxes = new();
            AgentToBoxGoals = new();

            Level.Agents.ForEach(a =>
            {
                AgentToBoxes.Add(a, new List<Box>());
                AgentToBoxGoals.Add(a, new List<Box>());
            });
        }
    }

    public class AgentDelegationHelper
    {
        public Agent AgentReference { get; set; }
        public List<(BoxDelegationHelper boxDelegationHelper, int cost)> ReachableBoxes { get; set; }

        public AgentDelegationHelper(Agent agentReference)
        {
            AgentReference = agentReference;
            ReachableBoxes = new List<(BoxDelegationHelper boxDelegationHelper, int cost)>();
        }
    }

    public class BoxDelegationHelper
    {
        public Box BoxReference { get; set; }
        public List<(BoxGoalDelegationHelper boxGoalDelegationHelper, int cost)> ReachableBoxGoals { get; set; }
        public List<(AgentDelegationHelper agentDelegationHelper, int cost)> ReachableAgents { get; set; }

        public BoxDelegationHelper(Box boxReference)
        {
            BoxReference = boxReference;
            ReachableBoxGoals = new List<(BoxGoalDelegationHelper boxGoalDelegationHelper, int cost)>();
            ReachableAgents = new List<(AgentDelegationHelper agentDelegationHelper, int cost)>();
        }
    }

    public class BoxGoalDelegationHelper
    {
        public Box BoxGoalReference { get; set; }
        public List<(BoxDelegationHelper boxDelegationHelper, int cost)> ReachableBoxes { get; set; }

        public BoxGoalDelegationHelper(Box boxGoalReference)
        {
            BoxGoalReference = boxGoalReference;
            ReachableBoxes = new List<(BoxDelegationHelper boxDelegationHelper, int cost)>();
        }
    }
}