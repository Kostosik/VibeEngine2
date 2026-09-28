using Engine.Navigation.Maps;

namespace Engine.Navigation.Pathfinding.Algorithms;

public sealed class AStarPathfinder : INavigationPathfinder
{
    private static readonly (
        int X,
        int Y)[] Directions =
    {
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1)
    };

    public bool TryFindPath(
        INavigationMap map,
        NavigationCoordinate start,
        NavigationCoordinate goal,
        out NavigationPath? path)
    {
        ArgumentNullException.ThrowIfNull(
            map);

        path =
            null;

        if (!map.Contains(
                start) ||
            !map.Contains(
                goal))
        {
            return false;
        }

        if (!map.IsTraversable(
                start) ||
            !map.IsTraversable(
                goal))
        {
            return false;
        }

        if (start ==
            goal)
        {
            path =
                new NavigationPath(
                    new[]
                    {
                        start
                    },
                    0.0f);

            return true;
        }

        var frontier =
            new PriorityQueue<
                NavigationCoordinate,
                (float FScore, int Y, int X)>();

        var costs =
            new Dictionary<
                NavigationCoordinate,
                float>();

        var parents =
            new Dictionary<
                NavigationCoordinate,
                NavigationCoordinate>();

        costs[start] =
            0.0f;

        frontier.Enqueue(
            start,
            (
                Heuristic(
                    start,
                    goal),
                start.Y,
                start.X));

        while (frontier.Count > 0)
        {
            var current =
                frontier.Dequeue();

            if (current ==
                goal)
            {
                path =
                    BuildPath(
                        parents,
                        costs,
                        start,
                        goal);

                return true;
            }

            var currentCost =
                costs[current];

            foreach (var direction in
                     Directions)
            {
                var neighbor =
                    new NavigationCoordinate(
                        current.X + direction.X,
                        current.Y + direction.Y);

                if (!map.Contains(
                        neighbor) ||
                    !map.IsTraversable(
                        neighbor))
                {
                    continue;
                }

                var stepCost =
                    map.GetTraversalCost(
                        neighbor);

                ValidateTraversalCost(
                    stepCost);

                var newCost =
                    currentCost +
                    stepCost;

                if (costs.TryGetValue(
                        neighbor,
                        out var existingCost) &&
                    newCost >= existingCost)
                {
                    continue;
                }

                costs[neighbor] =
                    newCost;

                parents[neighbor] =
                    current;

                var fScore =
                    newCost +
                    Heuristic(
                        neighbor,
                        goal);

                frontier.Enqueue(
                    neighbor,
                    (
                        fScore,
                        neighbor.Y,
                        neighbor.X));
            }
        }

        return false;
    }

    private static NavigationPath BuildPath(
        IReadOnlyDictionary<
            NavigationCoordinate,
            NavigationCoordinate> parents,
        IReadOnlyDictionary<
            NavigationCoordinate,
            float> costs,
        NavigationCoordinate start,
        NavigationCoordinate goal)
    {
        var coordinates =
            new List<NavigationCoordinate>();

        var current =
            goal;

        coordinates.Add(
            current);

        while (current !=
               start)
        {
            current =
                parents[current];

            coordinates.Add(
                current);
        }

        coordinates.Reverse();

        return new NavigationPath(
            coordinates,
            costs[goal]);
    }

    private static int Heuristic(
        NavigationCoordinate from,
        NavigationCoordinate to)
    {
        return Math.Abs(
                   from.X - to.X) +
               Math.Abs(
                   from.Y - to.Y);
    }

    private static void ValidateTraversalCost(
        float cost)
    {
        if (float.IsNaN(cost) ||
            float.IsInfinity(cost) ||
            cost <= 0.0f)
        {
            throw new InvalidOperationException(
                "Navigation traversal cost must be finite and greater than zero.");
        }
    }
}