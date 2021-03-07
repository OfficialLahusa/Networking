using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using SFML.System;
using SFML.Graphics;
using System.Collections.Generic;
using System.Globalization;

namespace MapToolkit
{
    public class SvgMapLoader
    {
        public SvgMapLoader()
        {
            
        }

        public VectorMap LoadMap(string filepath, Font font)
        {
            VectorMap map = new VectorMap();
            XDocument document = XDocument.Load(filepath);

            LoadBackgroundColor(document, ref map);

            Transform transform = Transform.Identity;

            var nodes =
                from node in document.Descendants()
                where node.Name.LocalName == "g"
                select node;
            nodes = nodes.ToList();
            nodes =
                from node in nodes
                where node.Parent.Name.LocalName != "g"
                select node;
            foreach(var node in nodes)
            {
                HandleGroup(node, ref map, transform, font);
            }

            return map;
        }

        // Formats a string of format "#RRGGBB" or "RRGGBB" into the corresponding RGB color
        private static Color? HexStringToColor(string hex)
        {
            if (hex.Length < 6) return null;
            if (hex[0] == '#') hex = hex.Remove(0, 1);
            string rHex = hex.Substring(0, 2);
            string gHex = hex.Substring(2, 2);
            string bHex = hex.Substring(4, 2);
            return new Color(Convert.ToByte(rHex, 16), Convert.ToByte(gHex, 16), Convert.ToByte(bHex, 16));
        }

        private static Transform EvaluateTransform(XElement element) 
        {
            Transform transform = Transform.Identity;

            XAttribute transformAttrib = element.Attribute("transform");
            if(transformAttrib == null)
            {
                return transform;
            }

            string transformString = transformAttrib.Value;

            if (transformString.Length == 0)
            {
                return transform;
            }

            transformString = transformString.Replace(") ", ")|");
            string[] transformSteps = transformString.Split('|');

            foreach(string transformStep in transformSteps) {
                string[] transformStepParts = transformStep.Split('(');

                if(transformStepParts.Length != 2)
                {
                    return transform;
                }

                string transformVerb = transformStepParts[0];
                string transformValuesString = transformStepParts[1].Substring(0, transformStepParts[1].Length - 1);

                string[] valuesString;
                if (transformValuesString.IndexOf(',') != -1)
                {
                    valuesString = transformValuesString.Split(',');
                } else
                {
                    valuesString = transformValuesString.Split(' ');
                }
                
                float[] values = new float[valuesString.Length];

                CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
                for(int i = 0; i < valuesString.Length; i++)
                {
                    values[i] = float.Parse(valuesString[i], System.Globalization.NumberStyles.Any, cultureInfo);
                }

                switch(transformVerb)
                {
                    default: 
                        break;
                    case "translate":
                        transform.Translate(values[0], values[1]);
                        break;
                    case "scale":
                        transform.Scale(values[0], values[1]);
                        break;
                    case "rotate":
                        if(values.Length == 1)
                        {
                            transform.Rotate(values[0]);
                        }
                        else if(values.Length == 3)
                        {
                            transform.Rotate(values[0], values[1], values[2]);
                        }
                        break;
                    case "matrix":
                        transform.Combine(new Transform(values[0], values[1], 0, values[2], values[3], 0, values[4], values[5], 1));
                        break;
                }
            }

            return transform;
        }

        private void HandleGroup(XElement node, ref VectorMap map, Transform parentTransform, Font font)
        {
            //Console.WriteLine($"Node: {node.Name.LocalName}\n{node}");
            Vector2f startingPos = new Vector2f(0, 0);
            Transform transform = parentTransform;
            transform.Combine(EvaluateTransform(node));

            var childNodes =
                from childNode in node.Elements()
                where childNode.Name.LocalName is "rect" or "path" or "g"
                select childNode;

            foreach (var childNode in childNodes)
            {
                switch (childNode.Name.LocalName)
                {
                    default:
                        break;
                    case "g":
                        HandleGroup(childNode, ref map, transform, font);
                        break;
                    case "rect":
                        HandleRect(childNode, ref map, transform);
                        break;
                    case "path":
                        HandlePath(childNode, ref map, transform, font);
                        break;
                }
            }
        }

        private void HandlePath(XElement node, ref VectorMap map, Transform parentTransform, Font font)
        {
            //Console.WriteLine(node);
            // Read transform
            Transform transform = parentTransform * EvaluateTransform(node);

            CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

            #region Style data
            // Read fill color from style attribute, if existing
            string style = string.Empty;
            Color fillColor = Color.Magenta;

            if (node.Attribute("style") != null)
            {
                style = node.Attribute("style").Value;
            }
            if (style != string.Empty)
            {
                int fillColorIndex = style.IndexOf("fill:#");
                if (fillColorIndex >= 0)
                {
                    fillColor = HexStringToColor(style.Substring(fillColorIndex + "fill:#".Length, 6)) ?? Color.Magenta;
                }
            }
            #endregion

            #region Path segmentation
            XAttribute pathAttrib = node.Attribute("d");

            // Return if no path attribute was found
            if (pathAttrib == null)
            {
                return;
            }

            string path = pathAttrib.Value;
            path = path.Replace(',', ' ');
            string[] pathSegments = path.Split(' ', StringSplitOptions.TrimEntries);

            // Return if the attribute contains no segments
            if (pathSegments.Length == 0)
            {
                return;
            }
            #endregion

            List<Vertex> vertices = new List<Vertex>();

            #region PathCommands
            Vector2f currentPos = new Vector2f(0, 0);
            bool hasDoneFirstMove = false;
            string lastCommand = string.Empty;

            for (int i = 0; i < pathSegments.Length; i++)
            {
                // Handle svg path commands
                HandlePathCommand(pathSegments[i], transform, ref i, pathSegments, ref vertices, ref currentPos, ref hasDoneFirstMove, ref lastCommand, fillColor, cultureInfo);
            }
            #endregion

            if(vertices.Last().Position == vertices.First().Position)
            {
                vertices.RemoveAt(vertices.Count - 1);
            }

            if(vertices.Count == 3)
            {
                foreach(Vertex vertex in vertices)
                {
                    map.DrawLayer.Append(vertex);
                }
            } else
            {
                // Calculate path center position
                Vector2f center = new Vector2f(0, 0);
                foreach (Vertex vertex in vertices)
                {
                    center += vertex.Position;
                }
                center /= vertices.Count;

                // Determine winding order
                int windingBias = 0;
                for (int i = 0; i < vertices.Count; i++)
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

                    Vector2f firstWinding = current.Position - center;
                    firstWinding /= MathF.Sqrt(firstWinding.X * firstWinding.X + firstWinding.Y * firstWinding.Y);
                    Vector2f secondWinding = next.Position - center;
                    secondWinding /= MathF.Sqrt(secondWinding.X * secondWinding.X + secondWinding.Y * secondWinding.Y);

                    float windingAngle = MathF.Atan2(firstWinding.X * secondWinding.Y - firstWinding.Y * secondWinding.X, firstWinding.X * secondWinding.X + firstWinding.Y * secondWinding.Y) / MathF.PI * 180.0f;

                    windingBias += (windingAngle > 0) ? 1 : -1;
                }

                // Convert counter-clockwise polygon to clockwise polygon
                if (windingBias <= 0)
                {
                    vertices.Reverse();
                }


                bool containsConcaveVertices = false;
                for (int i = 0; i < vertices.Count; i++)
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

                    #region VertexIDText
#if DEBUG
                    /*Text debugText = new Text(i.ToString(), font, 12)
                    {
                        Position = current.Position + new Vector2f(8, 0)
                    };
                    map.DebugText.Add(debugText);*/
#endif
                    #endregion

                    Vector2f first = current.Position - previous.Position;
                    first /= MathF.Sqrt(first.X * first.X + first.Y * first.Y);
                    Vector2f second = next.Position - previous.Position;
                    second /= MathF.Sqrt(second.X * second.X + second.Y * second.Y);

                    float angle = MathF.Atan2(first.X * second.Y - first.Y * second.X, first.X * second.X + first.Y * second.Y) / MathF.PI * 180.0f;

                    bool convex = angle >= 0;

                    if (!convex) containsConcaveVertices = true;

                    #region Vertex marker
                    /*           
#if DEBUG
                    // Draw vertex marker in angle sign color
                    Color markerColor = (convex) ? Color.Blue : Color.Red;
                    const float markerDist = 4;
                    map.DrawLayer.Append(new Vertex(current.Position + new Vector2f(-markerDist, -markerDist), markerColor));
                    map.DrawLayer.Append(new Vertex(current.Position + new Vector2f(markerDist, -markerDist), markerColor));
                    map.DrawLayer.Append(new Vertex(current.Position + new Vector2f(-markerDist, markerDist), markerColor));
                    map.DrawLayer.Append(new Vertex(current.Position + new Vector2f(markerDist, -markerDist), markerColor));
                    map.DrawLayer.Append(new Vertex(current.Position + new Vector2f(-markerDist, markerDist), markerColor));
                    map.DrawLayer.Append(new Vertex(current.Position + new Vector2f(markerDist, markerDist), markerColor));
#endif
                    */
                    #endregion
                }

                #region Center marker
                /*
#if DEBUG
                // Draw center marker in winding order color
                Color centerColor = (!containsConcaveVertices) ? Color.Blue : Color.Red;
                const float centerDist = 6;
                map.DrawLayer.Append(new Vertex(center + new Vector2f(-centerDist, -centerDist), centerColor));
                map.DrawLayer.Append(new Vertex(center + new Vector2f(centerDist, -centerDist), centerColor));
                map.DrawLayer.Append(new Vertex(center + new Vector2f(-centerDist, centerDist), centerColor));
                map.DrawLayer.Append(new Vertex(center + new Vector2f(centerDist, -centerDist), centerColor));
                map.DrawLayer.Append(new Vertex(center + new Vector2f(-centerDist, centerDist), centerColor));
                map.DrawLayer.Append(new Vertex(center + new Vector2f(centerDist, centerDist), centerColor));
#endif
                */
                #endregion

                // Concave
                if(containsConcaveVertices)
                {
                    PolygonTriangulator triangulator = new PolygonTriangulator();
                    foreach(Vertex vertex in triangulator.TriangulatePolygon(vertices))
                    {
                        map.DrawLayer.Append(vertex);
                    }
                }
                // Convex
                else
                {
                    for (int i = 1; i < vertices.Count - 1; i++)
                    {
                        map.DrawLayer.Append(vertices[0]);
                        map.DrawLayer.Append(vertices[i]);
                        map.DrawLayer.Append(vertices[i + 1]);
                    }
                }
            }
            return;
        }

        private void HandlePathCommand(string command, Transform transform, ref int i, string[] pathSegments, ref List<Vertex> vertices, ref Vector2f currentPos, ref bool hasDoneFirstMove, ref string lastCommand, Color fillColor, CultureInfo cultureInfo)
        {
            switch (command)
            {
                // Repeat recent command if there is any
                default:
                    if(command != string.Empty)
                    {
                        i -= 1;
                        HandlePathCommand(lastCommand, transform, ref i, pathSegments, ref vertices, ref currentPos, ref hasDoneFirstMove, ref lastCommand, fillColor, cultureInfo);
                    }
                    break;
                // Absolute move
                case "M":
                    {
                        lastCommand = "M";
                        if (!hasDoneFirstMove)
                        {
                            // Move
                            float x = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                            float y = float.Parse(pathSegments[i + 2], System.Globalization.NumberStyles.Any, cultureInfo);
                            currentPos = new Vector2f(x, y);
                            i += 2;
                            hasDoneFirstMove = true;
                        }
                        else
                        {
                            // Draw line
                            float x = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                            float y = float.Parse(pathSegments[i + 2], System.Globalization.NumberStyles.Any, cultureInfo);
                            Vector2f lineEnd = new Vector2f(x, y);
                            if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                            vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                            currentPos = lineEnd;
                            i += 2;
                        }
                    }
                    break;
                // Relative move
                case "m":
                    {
                        lastCommand = "m";
                        if (!hasDoneFirstMove)
                        {
                            // Move
                            float dx = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                            float dy = float.Parse(pathSegments[i + 2], System.Globalization.NumberStyles.Any, cultureInfo);
                            currentPos += new Vector2f(dx, dy);
                            i += 2;
                            hasDoneFirstMove = true;
                        }
                        else
                        {
                            // Draw line
                            float dx = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                            float dy = float.Parse(pathSegments[i + 2], System.Globalization.NumberStyles.Any, cultureInfo);
                            Vector2f lineEnd = currentPos + new Vector2f(dx, dy);
                            if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                            vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                            currentPos = lineEnd;
                            i += 2;
                        }

                    }
                    break;
                // Absolute line
                case "L":
                    {
                        lastCommand = "L";
                        float x = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                        float y = float.Parse(pathSegments[i + 2], System.Globalization.NumberStyles.Any, cultureInfo);
                        Vector2f lineEnd = new Vector2f(x, y);
                        if(vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                        currentPos = lineEnd;
                        i += 2;
                    }
                    break;
                // Relative line
                case "l":
                    {
                        lastCommand = "l";
                        float dx = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                        float dy = float.Parse(pathSegments[i + 2], System.Globalization.NumberStyles.Any, cultureInfo);
                        Vector2f lineEnd = currentPos + new Vector2f(dx, dy);
                        if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                        currentPos = lineEnd;
                        i += 2;
                    }
                    break;
                // Absolute horizontal line
                case "H":
                    {
                        lastCommand = "H";
                        float x = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                        Vector2f lineEnd = new Vector2f(x, currentPos.Y);
                        if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Relative horizontal line
                case "h":
                    {
                        lastCommand = "h";
                        float dx = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                        Vector2f lineEnd = new Vector2f(currentPos.X + dx, currentPos.Y);
                        if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Absolute vertical line
                case "V":
                    {
                        lastCommand = "V";
                        float y = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                        Vector2f lineEnd = new Vector2f(currentPos.X, y);
                        if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Relative vertical line
                case "v":
                    {
                        lastCommand = "v";
                        float dy = float.Parse(pathSegments[i + 1], System.Globalization.NumberStyles.Any, cultureInfo);
                        Vector2f lineEnd = new Vector2f(currentPos.X, currentPos.Y + dy);
                        if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(transform.TransformPoint(lineEnd), fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Close path
                case "Z":
                case "z":
                    {
                        lastCommand = "Z";
                        if (vertices.Count == 0) vertices.Add(new Vertex(transform.TransformPoint(currentPos), fillColor));
                        vertices.Add(new Vertex(vertices[0].Position, fillColor));
                        currentPos = vertices[0].Position;
                    }
                    break;

            }
        }

        private void HandleRect(XElement node, ref VectorMap map, Transform parentTransform)
        {
            //Console.WriteLine(node);
            // Read transform
            Transform transform = parentTransform;
            transform.Combine(EvaluateTransform(node));

            float x, y, width, height;
            x = y = width = height = 0;

            string style = string.Empty;
            Color fillColor = Color.Magenta;

            CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

            if (node.Attribute("x") != null)
            {
                x = float.Parse(node.Attribute("x").Value, System.Globalization.NumberStyles.Any, cultureInfo);
            }
            if (node.Attribute("y") != null)
            {
                y = float.Parse(node.Attribute("y").Value, System.Globalization.NumberStyles.Any, cultureInfo);
            }
            if (node.Attribute("width") != null)
            {
                width = float.Parse(node.Attribute("width").Value, System.Globalization.NumberStyles.Any, cultureInfo);
            }
            if (node.Attribute("height") != null)
            {
                height = float.Parse(node.Attribute("height").Value, System.Globalization.NumberStyles.Any, cultureInfo);
            }
            if (node.Attribute("style") != null)
            {
                style = node.Attribute("style").Value;
            }
            if (style != string.Empty)
            {
                int fillColorIndex = style.IndexOf("fill:#");
                if (fillColorIndex >= 0)
                {
                    fillColor = HexStringToColor(style.Substring(fillColorIndex + "fill:#".Length, 6)) ?? Color.Magenta;
                }
            }
            Vertex topLeft      = new Vertex(transform.TransformPoint(new Vector2f(x, y)),                      fillColor);
            Vertex topRight     = new Vertex(transform.TransformPoint(new Vector2f(x + width, y)),              fillColor);
            Vertex bottomLeft   = new Vertex(transform.TransformPoint(new Vector2f(x, y + height)),             fillColor);
            Vertex bottomRight  = new Vertex(transform.TransformPoint(new Vector2f(x + width, y + height)),     fillColor);
            map.DrawLayer.Append(topLeft);
            map.DrawLayer.Append(topRight);
            map.DrawLayer.Append(bottomLeft);
            map.DrawLayer.Append(bottomLeft);
            map.DrawLayer.Append(bottomRight);
            map.DrawLayer.Append(topRight);
        }

        private void LoadBackgroundColor(XDocument document, ref VectorMap map)
        {
            List<XElement> elements = (from element in document.Descendants()
                                       where element.Name.LocalName == "namedview"
                                       select element).ToList();

            // Check if a namedview exists
            if(elements.Count != 0)
            {
                XAttribute pageColorAttribute = elements[0].Attribute("pagecolor");

                if(pageColorAttribute != null)
                {
                    map.BackgroundColor = HexStringToColor(pageColorAttribute.Value);
                }
            }


        }
    }
}
