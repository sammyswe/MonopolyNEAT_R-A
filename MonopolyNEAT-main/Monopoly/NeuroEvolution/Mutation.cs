// This file implements the Mutation class in NEAT (NeuroEvolution of Augmenting Topologies)
// It handles genetic mutations: adding/removing connections or nodes, changing weights, and tracking historical changes

using System; // Provides basic types like float and int
using System.Collections.Generic; // For using lists
using System.Linq; // Not used, but would allow LINQ operations
using System.Text; // Not used here
using System.Threading.Tasks; // Not used here

namespace NEAT // Define the NEAT namespace
{
    public class Marking // Class to track structural mutation history (innovation tracking)
    {
        public int order = 0; // Innovation number (unique ID)
        public int source = 0; // Source node ID
        public int destination = 0; // Destination node ID
    }

    public class Mutation // Class responsible for mutating genomes (genotypes)
    {
        public static Mutation instance = null; // Singleton instance of Mutation. We only want one instance of this class
        //we can just consider this as a mutation tool box.

        public float MUTATE_LINK = 0.2f; 

        /*
        Meaning: 20% chance to add a new connection between two nodes.
        If increased: The network will grow connections faster.
        If decreased: Networks will be slower to add new paths of information.
        */

        public float MUTATE_NODE = 0.1f; 

        /*
        Meaning: 10% chance to add a new node by splitting an existing connection (add a new node in the middle of an existing edge.)
        If increased: You'll get more hidden layers/nodes quicker, allowing more complex behavior — but potentially overfitting or instability.
        If decreased: The network stays simpler for longer, which may improve stability early on but limit complexity.
        */

        public float MUTATE_ENABLE = 0.6f; 

        /*
        Meaning: 60% chance to re-enable a disabled gene (connection).
        If increased: More previously-disabled genes come back to life, encouraging experimentation.
        If decreased: Disabled genes stay off, leading to more conservative evolution.
        */

        public float MUTATE_DISABLE = 0.2f; 

        /*
        Meaning: 20% chance to disable a currently active connection.
        If increased: The network simplifies faster (can reduce over-complexity).
        If decreased: More structure is preserved, but you may get stuck with bad connections.
        */

        public float MUTATE_WEIGHT = 2.0f; 

        /*
        Meaning: The intensity/frequency of weight mutations. It’s a float >1 so multiple weight mutations can happen in one round.
        If increased: More weights will be mutated in a single mutation pass.
        If decreased: Less chance to change weights — good for stability, bad for exploration.
        */

        public float PETRUB_CHANCE = 0.9f; 

        /*
        Meaning: 90% chance to slightly shift an existing weight.
        If increased: Encourages gentle fine-tuning of weights (more stable).
        If decreased: More weights will be completely re-randomized (more volatile).
        */.

        public float SHIFT_STEP = 0.1f; 

        /*
        Meaning: When shifting a weight, this is the maximum step size.
        If increased: Weights can shift more drastically (can escape local optima but also destabilize).
        If decreased: Weights change more carefully — slower but steadier progress.
        */

        public List<Marking> historical = new List<Marking>(); // Signular instance of the marking class 
        // to track the mutation history (for innovation tracking)

        public static void Initialise() // Singleton initializer
        {
            if (instance == null) // Only create if not already existing
            {
                instance = new Mutation(); // Create the Mutation instance
            }
        }

        public Mutation() // Constructor (empty)
        {

        }

        //continue from here.

        public int RegisterMarking(EdgeInfo info) // Assigns an innovation number for a new edge if not already existing
        {
            int count = historical.Count; // Get current number of markings

            for (int i = 0; i < count; i++) // Check if this mutation already exists
            {
                Marking marking = historical[i];

                if (marking.source == info.source && marking.destination == info.destination) // If it matches an existing one
                {
                    return marking.order; // Return existing innovation number
                }
            }

            Marking creation = new Marking(); // Create new marking
            creation.order = historical.Count; // Assign next innovation number
            creation.source = info.source; // Set source
            creation.destination = info.destination; // Set destination

            historical.Add(creation); // Add to history

            return historical.Count - 1; // Return the new innovation number
        }

        public void MutateAll(Genotype genotype) // Perform all possible mutations on a genotype
        {
            // Reset mutation probabilities (may not be necessary every time)
            MUTATE_LINK = 0.2f;
            MUTATE_NODE = 0.1f;
            MUTATE_ENABLE = 0.6f;
            MUTATE_DISABLE = 0.2f;
            MUTATE_WEIGHT = 2.0f;

            float p = MUTATE_WEIGHT; // Start with weight mutation

            while (p > 0)
            {
                float roll = (float)RNG.instance.gen.NextDouble(); // Random roll

                if (roll < p)
                {
                    MutateWeight(genotype); // Possibly mutate a weight
                }

                p--; // Decrement mutation probability
            }

            p = MUTATE_LINK; // Now try adding links

            while (p > 0)
            {
                float roll = (float)RNG.instance.gen.NextDouble();

                if (roll < p)
                {
                    MutateLink(genotype); // Possibly add link
                }

                p--;
            }

            p = MUTATE_NODE; // Now try adding nodes

            while (p > 0)
            {
                float roll = (float)RNG.instance.gen.NextDouble();

                if (roll < p)
                {
                    MutateNode(genotype); // Possibly add node
                }

                p--;
            }

            p = MUTATE_DISABLE; // Now try disabling connections

            while (p > 0)
            {
                float roll = (float)RNG.instance.gen.NextDouble();

                if (roll < p)
                {
                    MutateDisable(genotype); // Possibly disable edge
                }

                p--;
            }

            p = MUTATE_ENABLE; // Now try enabling connections

            while (p > 0)
            {
                float roll = (float)RNG.instance.gen.NextDouble();

                if (roll < p)
                {
                    MutateEnable(genotype); // Possibly enable edge
                }

                p--;
            }

        }

        public void MutateLink(Genotype genotype) // Attempt to add a new connection between two nodes
        {
            int vertexCount = genotype.vertices.Count; // Number of nodes
            int edgeCount = genotype.edges.Count; // Number of connections

            List<EdgeInfo> potential = new List<EdgeInfo>(); // All valid new connections
            
            // Try all possible pairs of vertices
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    int source = genotype.vertices[i].index; // Get source node
                    int destination = genotype.vertices[j].index; // Get destination node

                    VertexInfo.EType t1 = genotype.vertices[i].type; // Source type
                    VertexInfo.EType t2 = genotype.vertices[j].type; // Destination type

                    if (t1 == VertexInfo.EType.OUTPUT || t2 == VertexInfo.EType.INPUT) // Avoid backward connections
                    {
                        continue;
                    }

                    if (source == destination) // Prevent self-loops
                    {
                        continue;
                    }

                    bool search = false; // To check if this edge already exists

                    for (int k = 0; k < edgeCount; k++)
                    {
                        EdgeInfo edge = genotype.edges[k];

                        if (edge.source == source && edge.destination == destination) // If edge exists
                        {
                            search = true;
                            break;
                        }
                    }

                    if (!search) // If edge doesn't exist yet
                    {
                        float weight = (float)RNG.instance.gen.NextDouble() * 4.0f - 2.0f; // Random weight (-2 to 2)
                        EdgeInfo creation = new EdgeInfo(source, destination, weight, true); // Create new edge

                        potential.Add(creation); // Add to candidate list
                    }
                }
            }

            if (potential.Count <= 0) // If no valid new connections
            {
                return;
            }

            int selection = RNG.instance.gen.Next(0, potential.Count); // Pick one at random

            EdgeInfo mutation = potential[selection]; // Get selected edge
            mutation.innovation = RegisterMarking(mutation); // Assign innovation number

            genotype.AddEdge(mutation.source, mutation.destination, mutation.weight, mutation.enabled, mutation.innovation); // Add to genotype
        }

        public void MutateNode(Genotype genotype) // Insert a node in the middle of an existing edge
        {
            int edgeCount = genotype.edges.Count; // Total edges

            int selection = RNG.instance.gen.Next(0, edgeCount); // Pick random edge

            EdgeInfo edge = genotype.edges[selection]; // Get edge

            if (edge.enabled == false) // Skip if already disabled
            {
                return;
            }

            edge.enabled = false; // Disable original edge

            int vertex_new = genotype.vertices[genotype.vertices.Count - 1].index + 1; // New node ID

            VertexInfo vertex = new VertexInfo(VertexInfo.EType.HIDDEN, vertex_new); // Create new hidden node

            EdgeInfo first = new EdgeInfo(edge.source, vertex_new, 1.0f, true); // New edge from source to new node
            EdgeInfo second = new EdgeInfo(vertex_new, edge.destination, edge.weight, true); // New edge from new node to destination

            first.innovation = RegisterMarking(first); // Assign innovations
            second.innovation = RegisterMarking(second);

            genotype.AddVertex(vertex.type, vertex.index); // Add new node

            genotype.AddEdge(first.source, first.destination, first.weight, first.enabled, first.innovation); // Add new edges
            genotype.AddEdge(second.source, second.destination, second.weight, second.enabled, second.innovation);
        }

        public void MutateEnable(Genotype genotype) // Enable a disabled edge
        {
            int edgeCount = genotype.edges.Count; // Total edges

            List<EdgeInfo> candidates = new List<EdgeInfo>(); // List of edges to enable

            for (int i =0; i < edgeCount; i++)
            {
                if (!genotype.edges[i].enabled) // If edge is disabled
                {
                    candidates.Add(genotype.edges[i]);
                }
            }

            if (candidates.Count == 0) // No edges to enable
            {
                return;
            }

            int selection = RNG.instance.gen.Next(0, candidates.Count); // Pick one

            EdgeInfo edge = candidates[selection];
            edge.enabled = true; // Enable it
        }

        public void MutateDisable(Genotype genotype) // Disable an enabled edge
        {
            int edgeCount = genotype.edges.Count;

            List<EdgeInfo> candidates = new List<EdgeInfo>(); // List of edges to disable

            for (int i = 0; i < edgeCount; i++)
            {
                if (genotype.edges[i].enabled) // If edge is enabled
                {
                    candidates.Add(genotype.edges[i]);
                }
            }

            if (candidates.Count == 0) // Nothing to disable
            {
                return;
            }

            int selection = RNG.instance.gen.Next(0, candidates.Count); // Pick one

            EdgeInfo edge = candidates[selection];
            edge.enabled = false; // Disable it
        }

        public void MutateWeight(Genotype genotype) // Mutate a random weight
        {
            int selection = RNG.instance.gen.Next(0, genotype.edges.Count); // Pick a random edge

            EdgeInfo edge = genotype.edges[selection]; // Get edge

            float roll = (float)RNG.instance.gen.NextDouble(); // Roll to decide mutation type

            if (roll < PETRUB_CHANCE) // If roll is low enough
            {
                MutateWeightShift(edge, SHIFT_STEP); // Slightly shift weight
            }
            else
            {
                MutateWeightRandom(edge); // Assign completely new weight
            }
        }

        public void MutateWeightShift(EdgeInfo edge, float step) // Slightly adjust a weight
        {
            float scalar = (float)RNG.instance.gen.NextDouble() * step - step * 0.5f; // Small random shift
            edge.weight += scalar; // Apply shift
        }

        public void MutateWeightRandom(EdgeInfo edge) // Assign new random weight
        {
            float value = (float)RNG.instance.gen.NextDouble() * 4.0f - 2.0f; // New random value (-2 to 2)
            edge.weight = value; // Set weight
        }
    }
}
