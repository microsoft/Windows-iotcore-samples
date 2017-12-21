using RogueSharp.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel;

namespace BuildingNavigationRobot.Pathfinding
{
    public class Navigation
    {
        private List<Vertex> vertices = new List<Vertex>();
        private List<DirectedEdge> edges = new List<DirectedEdge>();
        private EdgeWeightedDigraph graph;

        private int currentDirection; // keep track of current absolute direction roomba is facing (start at "north")
        private double _distance = 0;
        private List<Instruction> _directions = new List<Instruction>();

        private Vertex startVertex;
        private Vertex endVertex;
        private int maxId = -1;

        /// <summary>
        /// Initalize map (_Locations & _connections) from an XML file.
        /// To be called once over lifetime of app (when app is started).
        /// Format of file path example: InitializeMap(@"Map\map.xml");
        /// </summary>
        /// <param name="file"></param>
        public void InitializeMap(string file)
        {
            var doc = new XmlDocument();

            string XMLFilePath = Path.Combine(Package.Current.InstalledLocation.Path, file);

            //Try to read in XML file
            try
            {
                using (XmlReader reader = XmlReader.Create(XMLFilePath))
                {
                    doc.Load(reader);
                    ParseFile(doc);
                }
            }
            catch (Exception ex)
            {
                //Failed to load file
                Debug.WriteLine(ex.ToString());
            }
        }// End of IntializeMap

        /// <summary>
        /// Set the starting room
        /// </summary>
        /// <param name="room">Room number</param>
        public void SetStartingPosition(int room)
        {
            startVertex = FindRoom(room);
        }

        /// <summary>
        /// Check if room exists
        /// </summary>
        /// <param name="room">Room number</param>
        /// <returns></returns>
        public bool DoesRoomExist(int room)
        {
            return (FindRoom(room) != null);
        }
        
        /// <summary>
        /// Parse XML file
        /// </summary>
        private void ParseFile(XmlDocument doc)
        {
            vertices.Clear();
            edges.Clear();

            //Go through each node in XML file
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                var vertex = new Vertex();
                //One node (room)
                if (node.Name.Equals("Node"))
                {
                    //Go through properties of one node (room)
                    foreach (XmlNode n in node.ChildNodes)
                    {
                        if (n.Name.Equals("Identifier"))
                        {
                            vertex.Id = Int32.Parse(n.InnerText);
                            if (vertex.Id > maxId) maxId = vertex.Id;
                        }
                        else if (n.Name.Equals("X"))
                        {
                            vertex.X = int.Parse(n.InnerText);
                        }
                        else if (n.Name.Equals("Y"))
                        {
                            vertex.Y = int.Parse(n.InnerText);
                        }
                        else if (n.Name.Equals("Rooms"))
                        {
                            foreach (XmlNode nc in n.ChildNodes)
                            {
                                if (nc.Name.Equals("Rm"))
                                {
                                    vertex.Rooms.Add(int.Parse(nc.InnerText));
                                }
                            }
                        }
                        else if (n.Name.Equals("StartLocation") && bool.Parse(n.InnerText))
                        {
                            // Set starting location.
                            startVertex = vertex;
                        }
                    }

                    vertices.Add(vertex);
                }
                else if (node.Name.Equals("Connection"))
                {
                    int a = -1, b = -1;
                    double weight = -1;

                    foreach (XmlNode n in node.ChildNodes)
                    {
                        if (n.Name.Equals("A"))
                        {
                            a = Int32.Parse(n.InnerText);
                        }
                        else if (n.Name.Equals("B"))
                        {
                            b = Int32.Parse(n.InnerText);
                        }
                        else if (n.Name.Equals("Weight"))
                        {
                            weight = double.Parse(n.InnerText);
                        }
                    }

                    if (a != -1 && b != -1 && weight != -1)
                    {
                        var edge1 = new DirectedEdge(a, b, weight);
                        var edge2 = new DirectedEdge(b, a, weight);

                        edges.Add(edge1);
                        edges.Add(edge2);
                    }

                }
                else
                {
                    Debug.WriteLine("File is not formatted correctly.");
                }
            }

            graph = new EdgeWeightedDigraph(maxId + 1);

            foreach (var edge in edges)
            {
                graph.AddEdge(edge);
            }
        }

        /// <summary>
        /// Get the vertex for the specified ID
        /// </summary>
        /// <param name="vertexId"></param>
        /// <returns></returns>
        public Vertex GetVertex(int vertexId)
        {
            try
            {
                return vertices.First(x => x.Id == vertexId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// Get instructions for Roomba to get to room
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Instruction> GetDirections(int room)
        {
            _directions = new List<Instruction>();
            _distance = 0;
            int tempX = 0, tempY = 0; //for comparing X and Y of previous node

            var instructions = new List<Instruction>();
            endVertex = FindRoom(room);

            if (endVertex != null)
            {                
                var edgePath = DijkstraShortestPath.FindPath(graph, startVertex.Id, endVertex.Id);

                foreach (var edge in edgePath)
                {
                    var from = GetVertex(edge.From);
                    var to = GetVertex(edge.To);

                    //Turning from start
                    if (edge.From == startVertex.Id)
                    {
                        //ASSUME STARTING POSITION IS FACING NORTH (north as defined by map)
                        int xDiff = to.X - startVertex.X;
                        if (xDiff > 0)
                        {
                            //go east
                            //TURN RIGHT
                            instructions.Add(new Instruction(InstructionType.Angle, 90));
                            currentDirection += 90;
                        }
                        else if (xDiff < 0)
                        {
                            //go west
                            //TURN LEFT
                            instructions.Add(new Instruction(InstructionType.Angle, -90));
                            currentDirection -= 90;
                        }
                        else
                        {
                            int yDiff = to.Y - startVertex.Y;
                            if (yDiff < 0)
                            {
                                //go south
                                //TURN AROUND
                                instructions.Add(new Instruction(InstructionType.Angle, 180));
                                currentDirection += 180;
                            }
                            else if (yDiff > 0)
                            {
                                //go up (roomba starting should face north so no turn)
                            }
                        }
                    }
                    //Turning from a previous place
                    else
                    {
                        //Only turn if both X AND Y value is different
                        FindTurn(tempX, tempY, from.X, from.Y, to.X, to.Y);
                    }

                    //Add onto move distance (continuing in same direction)
                    _distance += edge.Weight;

                    //Check if we've reached the end and are done getting directions!
                    if (to.Id == endVertex.Id)
                    {
                        _directions.Add(new Instruction(InstructionType.Distance, _distance));
                        break;
                    }

                    //Set for next iteration of loop
                    tempX = from.X;
                    tempY = from.Y;
                }

                PrepareNextNavigation();
            }

            return _directions;
        }

        /// <summary>
        /// Find the Vertex associated with the room
        /// </summary>
        /// <param name="room">Room number</param>
        /// <returns></returns>
        private Vertex FindRoom(int room)
        {
            return vertices.First(x => x.Rooms.Contains(room));
        }

        /// <summary>
        /// Finds turn angle if applicable from two vectors' (connections) X & Ys, adds it to list of directions.
        /// Will only calculate left or right turns, NOT any other angles.
        /// </summary>
        /// <param name="_room"></param>
        private void FindTurn(int prevAX, int prevAY, int currAX, int currAY, int currBX, int currBY)
        {
            // Calculate turn from cross product of prev and curr vectors
            float prevX = currAX - prevAX;
            float prevY = currAY - prevAY;
            float currX = currBX - currAX;
            float currY = currBY - currAY;
            float zComp = (prevX * currY) - (prevY * currX); // Z component of cross product
            if (zComp == 0.0)
            {
                return; //No turn
            }
            else
            {
                //TURN!!
                //add previous accumulated move distance to directions list FIRST
                _directions.Add(new Instruction(InstructionType.Distance, _distance));
                _distance = 0; //Reset current move distance to zero

                //calculate angle to turn. only handles left & right, NOT any other angles
                if (zComp > 0.0)
                {
                    _directions.Add(new Instruction(InstructionType.Angle, -90)); //LEFT
                    currentDirection -= 90;
                }
                else
                {
                    _directions.Add(new Instruction(InstructionType.Angle, 90)); //RIGHT
                    currentDirection += 90;
                }
            }

        }

        /// <summary>
        /// After finishing a round of navigation, set up roomba for next navigation.
        /// </summary>
        private void PrepareNextNavigation()
        {
            startVertex = endVertex; //set prev end location as new start location
            _distance = 0; //reset

            //reset current direction of roomba to face absolute "north"
            currentDirection %= 360;
            if (currentDirection == 0)
            {
                //facing "north" already
            }
            else if (currentDirection % 180 == 0)
            { //facing "south"
                _directions.Add(new Instruction(InstructionType.Angle, 180));
            }
            else
            {
                //turn back the angle from north
                if (currentDirection > 0)
                {
                    if (currentDirection > 180)
                    {
                        currentDirection = 360 - currentDirection;
                    }
                    else
                    {
                        currentDirection = currentDirection * -1;
                    }
                }
                else
                {
                    if (currentDirection < -180)
                    {
                        currentDirection = -360 - currentDirection;
                    }
                    else
                    {
                        currentDirection = currentDirection * -1;
                    }
                }
                _directions.Add(new Instruction(InstructionType.Angle, currentDirection));
            }

            currentDirection = 0;
        }// End of PrepareNextNavigation
    }

    public struct Instruction
    {
        public InstructionType Type; //angle to turn or distance to travel
        public double Data;

        public Instruction(InstructionType t, double f)
        {
            Type = t; Data = f;
        }

        //For outputting directions
        public override string ToString()
        {
            if (Type == InstructionType.Angle)
            {
                string turnAngle = "";
                switch (Data)
                {
                    case 90: turnAngle = "right"; break;
                    case -90: turnAngle = "left"; break;
                    case 180: turnAngle = "around"; break;
                    default: turnAngle = Data.ToString() + " degrees"; break;
                }
                return "Turn " + turnAngle + ".";
            }
            else if (Type == InstructionType.Distance)
            {
                return "Move " + Data + " meters.";
            }
            return "";
        }

    }// End of Instruction

    public enum InstructionType
    {
        Angle,
        Distance
    }// End of InstructionType

    public class Vertex
    {
        public int Id;
        public int X, Y;
        public List<int> Rooms = new List<int>(); //list of rooms associated with this vertex
    }
}
