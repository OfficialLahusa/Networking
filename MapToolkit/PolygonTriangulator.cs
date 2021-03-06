using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapToolkit
{
    public class PolygonTriangulator
    {
        public List<Vertex> TriangulatePolygon(List<Vertex> vertices)
        {
            List<Vertex> inputVertices = new List<Vertex>(vertices);
            List<Vertex> triangles = new List<Vertex>();

            while(inputVertices.Count > 2)
            {
                int index = FindEar(inputVertices);
                if(index == -1)
                {
                    break;
                }

                #region Vertices for calculation
                Vertex previous, current, next;
                current = inputVertices[index];

                if (index == 0)
                {
                    previous = inputVertices[^1];
                }
                else
                {
                    previous = inputVertices[index - 1];
                }

                if (index == inputVertices.Count - 1)
                {
                    next = inputVertices[0];
                }
                else
                {
                    next = inputVertices[index + 1];
                }
                #endregion
                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                inputVertices.RemoveAt(index);
            }

            return triangles;
        }

        private int FindEar(List<Vertex> inputVertices)
        {
            int earIndex = -1;

            for (int i = 0; i < inputVertices.Count; i++)
            {
                if (IsConvex(inputVertices, i))
                {
                    int previous = i - 1;
                    int next = i + 1;

                    if (previous < 0) previous = inputVertices.Count - 1;
                    if (next > inputVertices.Count - 1) next = 0;

                    bool containsConcave = false;
                    for(int j = 0; j < inputVertices.Count; j++)
                    {
                        if(j != i && j != previous && j != next)
                        {
                            if(!IsConvex(inputVertices, j))
                            {
                                if(TriangleContains(inputVertices[previous].Position, inputVertices[i].Position, inputVertices[next].Position, inputVertices[j].Position))
                                {
                                    containsConcave = true;
                                    break;
                                }
                            }
                        }
                    }

                    if(!containsConcave)
                    {
                        return i;
                    }
                }
            }

            return earIndex;
        }

        private bool IsConvex(List<Vertex> vertices, int i)
        {
            #region Vertices for calculation
            Vertex previous, current, next;
            current = vertices[i];

            if (i == 0)
            {
                previous = vertices[^1];
            }
            else
            {
                previous = vertices[i - 1];
            }

            if (i == vertices.Count - 1)
            {
                next = vertices[0];
            }
            else
            {
                next = vertices[i + 1];
            }
            #endregion

            Vector2f first = current.Position - previous.Position;
            first /= MathF.Sqrt(first.X * first.X + first.Y * first.Y);
            Vector2f second = next.Position - previous.Position;
            second /= MathF.Sqrt(second.X * second.X + second.Y * second.Y);

            float angle = MathF.Atan2(first.X * second.Y - first.Y * second.X, first.X * second.X + first.Y * second.Y) / MathF.PI * 180.0f;

            return angle >= 0;
        }

        // Both functions from https://www.geeksforgeeks.org/check-whether-a-given-point-lies-inside-a-triangle-or-not/
        private static double TriangleArea(Vector2f p1, Vector2f p2, Vector2f p3)
        {
            return Math.Abs((p1.X * (p2.Y - p3.Y) +
                             p2.X * (p3.Y - p1.Y) +
                             p3.X * (p1.Y - p2.Y)) / 2.0);
        }

        private static bool TriangleContains(Vector2f p1, Vector2f p2, Vector2f p3, Vector2f p)
        {
            /* Calculate area of triangle ABC */
            double A = TriangleArea(p1, p2, p3);

            /* Calculate area of triangle PBC */
            double A1 = TriangleArea(p, p2, p3);

            /* Calculate area of triangle PAC */
            double A2 = TriangleArea(p1, p, p3);

            /* Calculate area of triangle PAB */
            double A3 = TriangleArea(p1, p2, p);

            /* Check if sum of A1, A2 and A3 is same as A */
            return (A == A1 + A2 + A3);
        }
    }
}
