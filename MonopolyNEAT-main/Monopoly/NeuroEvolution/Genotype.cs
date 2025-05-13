// This file defines the Genotype class, which represents the genetic structure (vertices and edges)
// of a neural network in NEAT. It also includes helper classes VertexInfo and EdgeInfo.
// Genotype supports cloning, adding nodes/edges, and sorting for crossover and speciation.

using System; // Provides basic types and functions
using System.Collections.Generic; // Required for List<T>
using System.Linq; // Useful LINQ operations (not used here)
using System.Text; // For encoding support (not used here)
using System.Threading.Tasks; // For async features (not used here)

namespace NEAT // Namespace for NEAT algorithm components
{
    public class VertexInfo // Class representing a node (neuron) in the network. These are all the properties that a neuron can/will have.
    {
        public enum EType // Type of vertex: input, hidden, or output
        {
            INPUT = 0, // Input node

            //input nodes represents values from the outside world e.g. game state, board tile or some other value extracted
            //from the current situation within the monopoly game (is in jail, current money, position)

            HIDDEN = 1, // Hidden node

            //Hidden nodes are the nodes that are changed over time during the NEAT algorithm. They connect inputs
            //to outputs through weighted edges. Most hidden nodes have multiple input and output nodes. They
            //exist to improve the networks internal logic and reasoning. They allow the network to model complex
            //patterns and decision making

            OUTPUT = 2, // Output node

            //Output nodes produce the final decision of the neural network e.g. Do i bid on this property? These outputs are 
            //received by the game state to essentially allow the network to play the game.
        }

        public EType type; // The type of this vertex
        public int index = 0; // The unique index of this vertex (just used to distinguish nodes and also distinguish edges e.g.
        //we might say that there is a connection between node 1 and 2, we need an index for this.)

        public VertexInfo(EType t, int i) // Constructor - allows nodes to be produced
        {
            type = t; // Set type
            index = i; // Set index
        }
    }

    public class EdgeInfo // Class representing a connection between nodes in the neural network
    {
        //structural information
        public int source = 0; // Index of source vertex  
        public int destination = 0; // Index of destination vertex

        //network information
        public float weight = 0.0f; // Connection weight - this determines how strong of a signal is passed along the network.
        // in the context of monopoly, an edge between the following two nodes would have a strong connection:
        // Do I own orange properties -> Do I want to bid on the orange property up for auction
        public bool enabled = false; // Is this edge enabled?

        //enabling an edge can be used to test whether a connection is negatively impacting the performance of a neural network
        //without actually deleting it. It can be viewed as 'commenting out' an edge. It also enables us to preserve history
        //so that we can get a more accurate reading on the crossover function.
        
        public int innovation = 0; // Innovation number for matching edges. Every edge has an innovation number; innovation number 
        //denotes the order in which edges were created.

        public EdgeInfo(int s, int d, float w, bool e) // Constructor without innovation
        {
            source = s; // Set source
            destination = d; // Set destination
            weight = w; // Set weight
            enabled = e; // Set enabled flag
        }
        //we may construct an edge without an innovation number for the following reasons:
        //1. Prototype an edge before assigning it a permanent innovation number to test it
        //2. Assign innovation number later to keep innovation lgoic centralised and clean
    }

    public class Genotype // The main class representing a neural network's structure. A genotype is a distinct neural network.
    {

        public List<VertexInfo> vertices; // List of all vertices (neurons)
        public List<EdgeInfo> edges; // List of all edges (connections)

        public int inputs = 0; // Number of input nodes
        public int externals = 0; // Number of external nodes (input + output)

        public int bracket = 0; // Sometimes innovation can add multiple new edges in one go.
        //this bracket number is a way for us to associate edges added at the same time together.

        public float fitness = 0.0f; // Fitness score i.e how good is this neural network performing
        //in a game of monopoly. The calculation for this is defined elsewhere
        public float adjustedFitness = 0.0f; // Fitness adjusted for species. Fitness is adjuseted
        //based on how many similar species there are. This stops one big species just taking over. The
        //fitness scores are shared between all neural networks of a similar species.

        public Genotype() // Constructor
        {
            vertices = new List<VertexInfo>(); // Initialize empty vertex list
            edges = new List<EdgeInfo>(); // Initialize empty edge list
        }

        public void AddVertex(VertexInfo.EType type, int index) // method to add a vertex to the genotype
        {
            VertexInfo v = new VertexInfo(type, index); // Create new vertex
            vertices.Add(v); // Add to list

            if (v.type != VertexInfo.EType.HIDDEN) // Increases external count (total input/output nodes)
            {
                externals++; //If it is not a hidden node.
            }

            if (v.type == VertexInfo.EType.INPUT) // Increases input count if it is an input node
            {
                inputs++;
            }
        }

        public void AddEdge(int source, int destination, float weight, bool enabled) // Add edge (no innovation)
        {
            EdgeInfo e = new EdgeInfo(source, destination, weight, enabled); // Create edge
            edges.Add(e); // Add to list
        }

        public void AddEdge(int source, int destination, float weight, bool enabled, int innovation) // Add edge with innovation number
        {
            EdgeInfo e = new EdgeInfo(source, destination, weight, enabled); // Create edge
            e.innovation = innovation; // Set innovation
            edges.Add(e); // Add to list
        }

        public Genotype Clone() // Create a deep copy (identical copy that doesn't change the original) of this genotype
        {
            Genotype copy = new Genotype(); // Create new genotype to copy data into

            int vertexCount = vertices.Count; // Number of vertices

            for (int i = 0; i < vertexCount; i++) // Copy each vertex one by one
            {
                copy.AddVertex(vertices[i].type, vertices[i].index);
            }

            int edgeCount = edges.Count; // Number of edges

            for (int i = 0; i < edgeCount; i++) // Copy each edge one by one
            {
                copy.AddEdge(edges[i].source, edges[i].destination, edges[i].weight, edges[i].enabled, edges[i].innovation);
            }

            return copy; // Return the clone
        }

        public void SortTopology() // Sort both vertices and edges - this makes sure that the networks are in a predictable order
        //This is very important for debugging.
        {
            SortVertices(); // Sort vertices by index
            SortEdges(); // Sort edges by innovation
        }

        public void SortVertices() // Sort vertices using custom comparer
        {
            vertices.Sort(CompareVertexByOrder); // Sort by index
        }

        public void SortEdges() // Sort edges using custom comparer
        {
            edges.Sort(CompareEdgeByInnovation); // Sort by innovation number
        }

        public int CompareVertexByOrder(VertexInfo a, VertexInfo b) // Comparison logic basically allows us to compare indices of vertexes to sort them in a 
        //consistent way
        {
            if (a.index > b.index) // a comes after b
            {
                return 1;
            }
            else if (a.index == b.index) // Equal
            {
                return 0;
            }

            return -1; // a comes before b
        }

        public int CompareEdgeByInnovation(EdgeInfo a, EdgeInfo b) // Comparison logic for edges allows us to compare innovation number of edges and sort them
        //in a consistent way.
        {
            if (a.innovation > b.innovation) // a has higher innovation
            {
                return 1;
            }
            else if (a.innovation == b.innovation) // Equal
            {
                return 0;
            }

            return -1; // a has lower innovation
        }
    }
}