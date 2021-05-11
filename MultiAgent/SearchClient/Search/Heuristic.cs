using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public static class Heuristic
    {
        public static int CalculateHeuristicSA(SAState state)
        {
            int agentDistance = 0;
            int boxDistance = 0;
            int solvedGoalsToBoxDistance = 0;
            int constraintsScore = 0;

            var constraints = state.Constraints.Where(c => c.MaxTime > state.Time).ToList();

            foreach (var constraint in constraints)
            {
                foreach (var constrainedPosition in constraint.Positions)
                {
                    if (state.PositionsOfBoxes.ContainsKey(constrainedPosition))
                    {
                        constraintsScore +=
                            10 + Level.GetDistanceBetweenPosition(state.AgentPosition, constrainedPosition);
                    }
                }
            }

            var boxGoalsPreviouslySolved = state.BoxGoals.Except(new[] {state.CurrentBoxGoal}).ToList();
            if (boxGoalsPreviouslySolved.Any()) // if any box-goals except the one we are currently solving
            {
                var usedBoxes = new List<Box>(state.Boxes.Count);

                foreach (var boxGoal in boxGoalsPreviouslySolved)
                {
                    if (state.PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var boxOnTopOfGoal))
                    {
                        if (boxGoal.Letter == boxOnTopOfGoal.Letter)
                        {
                            continue;
                        }
                    }

                    Box closestBox = null;
                    Position? closestBoxPosition = null;
                    foreach (var (boxPosition, box) in state.PositionsOfBoxes
                        .Where(b => b.Value.Letter == boxGoal.Letter && !usedBoxes.Contains(b.Value)))
                    {
                        if (closestBox == null || !closestBoxPosition.HasValue)
                        {
                            closestBox = box;
                            closestBoxPosition = boxPosition;
                            continue;
                        }

                        if (Level.GetDistanceBetweenPosition(closestBoxPosition.Value,
                                boxGoal.GetInitialLocation()) >
                            Level.GetDistanceBetweenPosition(boxPosition, boxGoal.GetInitialLocation()))
                        {
                            closestBox = box;
                            closestBoxPosition = boxPosition;
                        }
                    }

                    usedBoxes.Add(closestBox);

                    if (closestBoxPosition != null)
                    {
                        solvedGoalsToBoxDistance += Level.GetDistanceBetweenPosition(boxGoal.GetInitialLocation(),
                            closestBoxPosition.Value);

                        solvedGoalsToBoxDistance +=
                            Level.GetDistanceBetweenPosition(closestBoxPosition.Value, state.AgentPosition);
                    }
                }
            }

            if (state.CurrentBoxGoal != null && state.RelevantBoxToSolveGoal != null)
            {
                boxDistance += Level.GetDistanceBetweenPosition(state.CurrentBoxGoal.GetInitialLocation(),
                    state.GetPositionOfBox(state.RelevantBoxToSolveGoal));
                boxDistance +=
                    Level.GetDistanceBetweenPosition(state.GetPositionOfBox(state.RelevantBoxToSolveGoal),
                        state.AgentPosition);
            }

            if (state.AgentGoal != null)
            {
                agentDistance +=
                    Level.GetDistanceBetweenPosition(state.AgentGoal.GetInitialLocation(), state.AgentPosition);
            }

            var h = 20 * constraintsScore + 20 * solvedGoalsToBoxDistance + 10 * boxDistance + 5 * agentDistance +
                    state.Time;
            return h;
        }


        public static int CalculateHeuristicMA(MAState state)
        {
            int agentDistance = 0;
            int boxDistance = 0;
            int solvedGoalsToBoxDistance = 0;
            int constraintsScore = 0;
            
            foreach (var agent in state.Agents)
            {
                var boxGoalsPreviouslySolved = state.BoxGoals.Except(new[] {state.AgentToCurrentGoal[agent]}).ToList();
                if (boxGoalsPreviouslySolved.Any()) // if any box-goals except the one we are currently solving
                {
                    var usedBoxes = new List<Box>(state.Boxes.Count);

                    foreach (var boxGoal in boxGoalsPreviouslySolved)
                    {
                        if (state.PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var boxOnTopOfGoal))
                        {
                            if (boxGoal.Letter == boxOnTopOfGoal.Letter)
                            {
                                continue;
                            }
                        }

                        Box closestBox = null;
                        Position? closestBoxPosition = null;
                        foreach (var (boxPosition, box) in state.PositionsOfBoxes
                            .Where(b => b.Value.Letter == boxGoal.Letter && !usedBoxes.Contains(b.Value)))
                        {
                            if (closestBox == null || !closestBoxPosition.HasValue)
                            {
                                closestBox = box;
                                closestBoxPosition = boxPosition;
                                continue;
                            }

                            if (Level.GetDistanceBetweenPosition(closestBoxPosition.Value,
                                    boxGoal.GetInitialLocation()) >
                                Level.GetDistanceBetweenPosition(boxPosition, boxGoal.GetInitialLocation()))
                            {
                                closestBox = box;
                                closestBoxPosition = boxPosition;
                            }
                        }

                        usedBoxes.Add(closestBox);

                        if (closestBoxPosition != null)
                        {
                            solvedGoalsToBoxDistance += Level.GetDistanceBetweenPosition(boxGoal.GetInitialLocation(),
                                closestBoxPosition.Value);

                            solvedGoalsToBoxDistance +=
                                Level.GetDistanceBetweenPosition(closestBoxPosition.Value, state.AgentPositions[agent]);
                        }
                    }
                }

                if (state.AgentToCurrentGoal[agent] != null && state.AgentToRelevantBox[agent] != null)
                {
                    boxDistance += Level.GetDistanceBetweenPosition(
                        state.AgentToCurrentGoal[agent].GetInitialLocation(),
                        state.GetPositionOfBox(state.AgentToRelevantBox[agent]));
                    boxDistance +=
                        Level.GetDistanceBetweenPosition(state.GetPositionOfBox(state.AgentToRelevantBox[agent]),
                            state.AgentPositions[agent]);
                }

                if (state.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number) != null)
                {
                    agentDistance +=
                        Level.GetDistanceBetweenPosition(
                            state.AgentGoals.First(ag => ag.Number == agent.Number).GetInitialLocation(),
                            state.AgentPositions[agent]);
                }
            }

            return 20 * solvedGoalsToBoxDistance + 10 * boxDistance + 5 * agentDistance + state.Time;
        }
    }
}
