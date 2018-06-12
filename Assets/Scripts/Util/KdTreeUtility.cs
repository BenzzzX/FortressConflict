using Unity.Collections;
using Unity.Mathematics;

public static class KdTreeUtility
{
    public const int MAX_LEAF_SIZE = 10;
    
    public struct TreeNode
    {
        public int begin;
        public int end;
        public int left;
        public int right;
        public float maxX;
        public float maxY;
        public float minX;
        public float minY;
    }


    static public void BuildTree(NativeArray<TreeNode> tree, NativeArray<float2> agents, NativeArray<int> agentIndices)
    {
        BuildTree(tree, agents, agentIndices, 0, agents.Length, 0);
    }

    static void BuildTree(NativeArray<TreeNode> tree, NativeArray<float2> agents, NativeArray<int> agentIndices, int begin, int end, int index)
    {
        var node = tree[index];
        node.begin = begin;
        node.end = end;
        node.minX = node.maxX = agents[begin].x;
        node.minY = node.maxY = agents[begin].y;

        for(var i = begin+1;i<end;++i)
        {
            node.maxX = math.max(node.maxX, agents[i].x);
            node.minX = math.min(node.minX, agents[i].x);
            node.maxY = math.max(node.maxY, agents[i].y);
            node.minY = math.min(node.minY, agents[i].y);
        }

        tree[index] = node;
        if (end - begin > MAX_LEAF_SIZE)
        {
            /* No leaf node. */
            bool isVertical = (node.maxX - node.minX > node.maxY - node.minY);
            float splitValue = (isVertical ? 0.5f * (node.maxX + node.minX) : 0.5f * (node.maxY + node.minY));

            int left = begin;
            int right = end;

            while (left < right)
            {
                while (left < right && (isVertical ? agents[left].x : agents[left].y) < splitValue)
                {
                    ++left;
                }

                while (right > left && (isVertical ? agents[right - 1].x : agents[right - 1].y) >= splitValue)
                {
                    --right;
                }

                if (left < right)
                {
                    Swap(agents, left, right - 1);
                    Swap(agentIndices, left, right - 1);
                    ++left;
                    --right;
                }
            }

            if (left == begin)
            {
                ++left;
                ++right;
            }

            node.left = index + 1;
            node.right = index + 2 * (left - begin);

            tree[index] = node;
            BuildTree(tree, agents, agentIndices, begin, left, node.left);
            BuildTree(tree, agents, agentIndices, left, end, node.right);
        }
    }

    static void Swap<T>(NativeArray<T> array, int l, int r)
        where T: struct
    {
        T t = array[l];
        array[l] = array[r];
        array[r] = t;
    }

    static public void QueryNeighbors(NativeArray<TreeNode> tree, NativeArray<float2> agents, int agentID, float rangeSq, NativeSlice<int> neighbors, NativeSlice<float> distance, ref int neighborSize)
    {
        QueryNeighbors(tree, agents, agentID, ref rangeSq, 0, neighbors, distance, ref neighborSize);
    }

    static void QueryNeighbors(NativeArray<TreeNode> tree, NativeArray<float2> agents, int agentID, ref float rangeSq, int index, NativeSlice<int> neighbors, NativeSlice<float> distances, ref int neighborSize)
    {
        var agent = agents[agentID];
        if (tree[index].end - tree[index].begin <= MAX_LEAF_SIZE)
        {
            for (int i = tree[index].begin; i < tree[index].end; ++i)
            {
                if (i != agentID)
                {
                    float distSq = math.lengthSquared(agent - agents[i]);

                    if (distSq < rangeSq)
                    {
                        if (neighborSize < neighbors.Length)
                        {
                            neighbors[neighborSize] = i;
                            distances[neighborSize++] = distSq;
                        }

                        int k = neighborSize - 1;

                        while (k != 0 && distSq < distances[k - 1])
                        {
                            neighbors[k] = neighbors[k - 1];
                            distances[k] = distances[k - 1];
                            --k;
                        }

                        neighbors[k] = i;
                        distances[k] = distSq;

                        if (neighborSize == neighbors.Length)
                        {
                            rangeSq = distances[neighborSize - 1];
                        }
                    }
                }
            }
        }
        else
        {
            float distSqLeft = 
                sqr(math.max(0.0f, tree[tree[index].left].minX - agent.x)) + 
                sqr(math.max(0.0f, agent.x - tree[tree[index].left].maxX)) + 
                sqr(math.max(0.0f, tree[tree[index].left].minY - agent.y)) + 
                sqr(math.max(0.0f, agent.y - tree[tree[index].left].maxY));

            float distSqRight = 
                sqr(math.max(0.0f, tree[tree[index].right].minX - agent.x)) + 
                sqr(math.max(0.0f, agent.x - tree[tree[index].right].maxX)) + 
                sqr(math.max(0.0f, tree[tree[index].right].minY - agent.y)) + 
                sqr(math.max(0.0f, agent.y - tree[tree[index].right].maxY));

            if (distSqLeft < distSqRight)
            {
                if (distSqLeft < rangeSq)
                {
                    QueryNeighbors(tree, agents, agentID, ref rangeSq, tree[index].left, neighbors, distances, ref neighborSize);

                    if (distSqRight < rangeSq)
                    {
                        QueryNeighbors(tree, agents, agentID, ref rangeSq, tree[index].right, neighbors, distances, ref neighborSize);
                    }
                }
            }
            else
            {
                if (distSqRight < rangeSq)
                {
                    QueryNeighbors(tree, agents, agentID, ref rangeSq, tree[index].right, neighbors, distances, ref neighborSize);

                    if (distSqLeft < rangeSq)
                    {
                        QueryNeighbors(tree, agents, agentID, ref rangeSq, tree[index].left, neighbors, distances, ref neighborSize);
                    }
                }
            }
        }
    }

    static float sqr(float f) { return f * f; }
}