using Unity.Mathematics;
using Unity.Collections;
using Unity.Mathematics.Experimental;

public static class RVOUtility
{
    public struct Line
    {
        public float2 point;
        public float2 direction;
    }

    static float sqr(float f) { return f * f; }

    static float det(float2 vector1, float2 vector2)
    {
        return vector1.x * vector2.y - vector1.y * vector2.x;
    }

    static bool linearProgram1(NativeLocalArray<Line> lines, int lineNo, float radius, float2 optVelocity, bool directionOpt, ref float2 result)
    {
        var line = lines[lineNo];
        float dotProduct = math.dot(line.point, line.direction);
        float discriminant = sqr(dotProduct) + sqr(radius) - math.lengthSquared(line.point);

        if (discriminant < 0.0f)
        {
            /* Max speed circle fully invalidates line lineNo. */
            return false;
        }

        float sqrtDiscriminant = math.sqrt(discriminant);
        float tLeft = -dotProduct - sqrtDiscriminant;
        float tRight = -dotProduct + sqrtDiscriminant;

        for (int i = 0; i < lineNo; ++i)
        {
            float denominator = det(line.direction, lines[i].direction);
            float numerator = det(lines[i].direction, line.point - lines[i].point);

            if (math.abs(denominator) <= math_experimental.epsilon)
            {
                /* Lines lineNo and i are (almost) parallel. */
                if (numerator < 0.0f)
                {
                    return false;
                }
                else
                {
                    continue;
                }
            }

            float t = numerator / denominator;
            
            if (denominator >= 0.0f)
            {
                /* Line i bounds line lineNo on the right. */
                tRight = math.min(tRight, t);
            }
            else
            {
                /* Line i bounds line lineNo on the left. */
                tLeft = math.max(tLeft, t);
            }

            if (tLeft > tRight)
            {
                return false;
            }
        }

        if (directionOpt)
        {
            /* Optimize direction. */
            result = math.select(
                line.point + tLeft * line.direction, /* Take left extreme. */
                line.point + tRight * line.direction, /* Take right extreme. */
                math.dot(optVelocity, line.direction) > 0.0f
                );
        }
        else
        {
            /* Optimize closest point. */
            float t = math.dot(line.direction, (optVelocity - line.point));

            if (t < tLeft)
            {
                result = line.point + tLeft * line.direction;
            }
            else if (t > tRight)
            {
                result = line.point + tRight * line.direction;
            }
            else
            {
                result = line.point + t * line.direction;
            }
        }

        return true;
    }

    public static int linearProgram2(NativeLocalArray<Line> lines, int lineSize, float radius, float2 optVelocity, bool directionOpt, out float2 result)
    {
        if (directionOpt)
        {
            /*
			 * Optimize direction. Note that the optimization velocity is of unit
			 * length in this case.
			 */
            result = optVelocity * radius;
        }
        else if (math.lengthSquared(optVelocity) > sqr(radius))
        {
            /* Optimize closest point and outside circle. */
            result = math.normalize(optVelocity) * radius;
        }
        else
        {
            /* Optimize closest point and inside circle. */
            result = optVelocity;
        }

        for (int i = 0; i < lineSize; ++i)
        {
            if (det(lines[i].direction, lines[i].point - result) > 0.0f)
            {
                /* Result does not satisfy constraint i. Compute new optimal result. */
                float2 tempResult = result;

                if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tempResult;
                    return i;
                }
            }
        }

        return lineSize;
    }

    public static void linearProgram3(NativeLocalArray<Line> lines, int lineSize, NativeLocalArray<Line> projLines, int beginLine, float radius, ref float2 result)
    {
        float distance = 0f;

        for (int i = beginLine; i < lineSize; ++i)
        {
            if (det(lines[i].direction, lines[i].point - result) > distance)
            {
                /* Result does not satisfy constraint of line i. */
                int k = 0;
                for (int j = 0; j < i; ++j)
                {
                    Line line;

                    float determinant = det(lines[i].direction, lines[j].direction);

                    if (math.abs(determinant) <= math_experimental.epsilon)
                    {
                        /* Line i and line j are parallel. */
                        if (math.dot(lines[i].direction, lines[j].direction) > 0.0f)
                        {
                            /* Line i and line j point in the same direction. */
                            continue;
                        }
                        else
                        {
                            /* Line i and line j point in opposite direction. */
                            line.point = 0.5f * (lines[i].point + lines[j].point);
                        }
                    }
                    else
                    {
                        line.point = lines[i].point + (det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                    }

                    line.direction = math.normalize(lines[j].direction - lines[i].direction);
                    projLines[k++] = line;
                }

                float2 tempResult = result;
                var optVelocity = new float2(-lines[i].direction.y, lines[i].direction.x);

                if (linearProgram2(projLines, k, radius, optVelocity, true, out result) < k)
                {
                    /* This should in principle not happen.  The result is by definition
					 * already in the feasible region of this linear program. If it fails,
					 * it is due to small floating point error, and the current result is
					 * kept.
					 */
                    result = tempResult;
                }

                distance = det(lines[i].direction, lines[i].point - result);
            }
        }
    }


}