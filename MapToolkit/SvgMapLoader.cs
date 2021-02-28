using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using SFML.System;
using SFML.Graphics;
using System.Collections.Generic;

namespace MapToolkit
{
    public class SvgMapLoader
    {
        public SvgMapLoader()
        {
            
        }

        public VectorMap LoadMap(string filepath)
        {
            VectorMap map = new VectorMap();
            XDocument xDocument = XDocument.Load(filepath);

            var nodes =
                from node in xDocument.Descendants()
                where node.Name.LocalName == "g"
                select node;
            nodes = nodes.ToList();
            nodes =
                from node in nodes
                where node.Parent.Name.LocalName != "g"
                select node;
            foreach(var node in nodes)
            {
                //Console.WriteLine($"Node: {node.Name.LocalName}\n{node}");
                Vector2f startingPos = new Vector2f(0, 0);
                Transform transform = Transform.Identity;

                var childNodes =
                    from childNode in node.Descendants()
                    where childNode.Name.LocalName is "rect" or "path" 
                    select childNode;

                foreach(var childNode in childNodes)
                {
                    switch(childNode.Name.LocalName)
                    {
                        default:
                            break;
                        case "rect":
                            HandleRect(childNode, ref map);
                            break;
                        case "path":
                            HandlePath(childNode, ref map);
                            break;
                    }
                }
            }

            return map;
        }

        private Color HexStringToColor(string hex)
        {
            string rHex = hex.Substring(0, 2);
            string gHex = hex.Substring(2, 2);
            string bHex = hex.Substring(4, 2);
            return new Color(Convert.ToByte(rHex, 16), Convert.ToByte(gHex, 16), Convert.ToByte(bHex, 16));
        }

        private Transform EvaluateTransform(XElement element) 
        {
            Transform transform = Transform.Identity;

            return transform;
        }

        private void HandlePath(XElement node, ref VectorMap map)
        {
            Console.WriteLine(node);

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
                    fillColor = HexStringToColor(style.Substring(fillColorIndex + "fill:#".Length, 6));
                }
            }

            XAttribute pathAttrib = node.Attribute("d");
            if (pathAttrib == null)
            {
                return;
            }

            string path = pathAttrib.Value;
            path = path.Replace(',', ' ');
            string[] pathSegments = path.Split(' ', StringSplitOptions.TrimEntries);
            Console.WriteLine(path);

            if (pathSegments.Length <= 0)
            {
                return;
            }
            
            List<Vertex> vertices = new List<Vertex>();
            #region PathCommands
            Vector2f currentPos = new Vector2f(0, 0);
            bool hasDoneFirstMove = false;
            string lastCommand = string.Empty;

            for (int i = 0; i < pathSegments.Length; i++)
            {
                // Handle svg path commands
                HandlePathCommand(pathSegments[i], ref i, pathSegments, ref vertices, ref currentPos, ref hasDoneFirstMove, ref lastCommand, fillColor);
            }
            #endregion

            foreach(Vertex vertex in vertices)
            {
                map.DebugLines.Append(vertex);
            }

            return;
        }

        private void HandlePathCommand(string command, ref int i, string[] pathSegments, ref List<Vertex> vertices, ref Vector2f currentPos, ref bool hasDoneFirstMove, ref string lastCommand, Color fillColor)
        {
            switch (command)
            {
                // Repeat recent command if there is any
                default:
                    if(command != string.Empty)
                    {
                        i -= 1;
                        HandlePathCommand(lastCommand, ref i, pathSegments, ref vertices, ref currentPos, ref hasDoneFirstMove, ref lastCommand, fillColor);
                    }
                    break;
                // Absolute move
                case "M":
                    {
                        lastCommand = "M";
                        if (!hasDoneFirstMove)
                        {
                            // Move
                            float x = float.Parse(pathSegments[i + 1]);
                            float y = float.Parse(pathSegments[i + 2]);
                            currentPos = new Vector2f(x, y);
                            i += 2;
                            hasDoneFirstMove = true;
                        }
                        else
                        {
                            // Draw line
                            float x = float.Parse(pathSegments[i + 1]);
                            float y = float.Parse(pathSegments[i + 2]);
                            Vector2f lineEnd = new Vector2f(x, y);
                            vertices.Add(new Vertex(currentPos, fillColor));
                            vertices.Add(new Vertex(lineEnd, fillColor));
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
                            float dx = float.Parse(pathSegments[i + 1]);
                            float dy = float.Parse(pathSegments[i + 2]);
                            currentPos += new Vector2f(dx, dy);
                            i += 2;
                            hasDoneFirstMove = true;
                        }
                        else
                        {
                            // Draw line
                            float dx = float.Parse(pathSegments[i + 1]);
                            float dy = float.Parse(pathSegments[i + 2]);
                            Vector2f lineEnd = currentPos + new Vector2f(dx, dy);
                            vertices.Add(new Vertex(currentPos, fillColor));
                            vertices.Add(new Vertex(lineEnd, fillColor));
                            currentPos = lineEnd;
                            i += 2;
                        }

                    }
                    break;
                // Absolute line
                case "L":
                    {
                        lastCommand = "L";
                        float x = float.Parse(pathSegments[i + 1]);
                        float y = float.Parse(pathSegments[i + 2]);
                        Vector2f lineEnd = new Vector2f(x, y);
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(lineEnd, fillColor));
                        currentPos = lineEnd;
                        i += 2;
                    }
                    break;
                // Relative line
                case "l":
                    {
                        lastCommand = "l";
                        float dx = float.Parse(pathSegments[i + 1]);
                        float dy = float.Parse(pathSegments[i + 2]);
                        Vector2f lineEnd = currentPos + new Vector2f(dx, dy);
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(lineEnd, fillColor));
                        currentPos = lineEnd;
                        i += 2;
                    }
                    break;
                // Absolute horizontal line
                case "H":
                    {
                        lastCommand = "H";
                        float x = float.Parse(pathSegments[i + 1]);
                        Vector2f lineEnd = new Vector2f(x, currentPos.Y);
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(lineEnd, fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Relative horizontal line
                case "h":
                    {
                        lastCommand = "h";
                        float dx = float.Parse(pathSegments[i + 1]);
                        Vector2f lineEnd = new Vector2f(currentPos.X + dx, currentPos.Y);
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(lineEnd, fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Absolute vertical line
                case "V":
                    {
                        lastCommand = "V";
                        float y = float.Parse(pathSegments[i + 1]);
                        Vector2f lineEnd = new Vector2f(currentPos.X, y);
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(lineEnd, fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Relative vertical line
                case "v":
                    {
                        lastCommand = "v";
                        float dy = float.Parse(pathSegments[i + 1]);
                        Vector2f lineEnd = new Vector2f(currentPos.X, currentPos.Y + dy);
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(lineEnd, fillColor));
                        currentPos = lineEnd;
                        i += 1;
                    }
                    break;
                // Close path
                case "Z":
                case "z":
                    {
                        lastCommand = "Z";
                        vertices.Add(new Vertex(currentPos, fillColor));
                        vertices.Add(new Vertex(vertices[0].Position, fillColor));
                        currentPos = vertices[0].Position;
                    }
                    break;

            }
        }

        private void HandleRect(XElement node, ref VectorMap map)
        {
            //Console.WriteLine(node);
            float x, y, width, height;
            x = y = width = height = 0;

            string style = string.Empty;
            Color fillColor = Color.Magenta;

            if (node.Attribute("x") != null)
            {
                x = float.Parse(node.Attribute("x").Value);
            }
            if (node.Attribute("y") != null)
            {
                y = float.Parse(node.Attribute("y").Value);
            }
            if (node.Attribute("width") != null)
            {
                width = float.Parse(node.Attribute("width").Value);
            }
            if (node.Attribute("height") != null)
            {
                height = float.Parse(node.Attribute("height").Value);
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
                    fillColor = HexStringToColor(style.Substring(fillColorIndex + "fill:#".Length, 6));
                }
            }

            Vertex topLeft = new Vertex(new Vector2f(x, y), fillColor);
            Vertex topRight = new Vertex(new Vector2f(x + width, y), fillColor);
            Vertex bottomLeft = new Vertex(new Vector2f(x, y + height), fillColor);
            Vertex bottomRight = new Vertex(new Vector2f(x + width, y + height), fillColor);
            map.DrawLayer.Append(topLeft);
            map.DrawLayer.Append(topRight);
            map.DrawLayer.Append(bottomLeft);
            map.DrawLayer.Append(bottomLeft);
            map.DrawLayer.Append(bottomRight);
            map.DrawLayer.Append(topRight);
        }
    }
}
