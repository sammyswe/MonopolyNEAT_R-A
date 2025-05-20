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

        public int RegisterMarking(EdgeInfo info) // Assigns an innovation number for a new edge if not already existing. Returns innovation number
        //of inputted edge.
        {
            int count = historical.Count; // Get current number of markings (how many innovations already exist)

            for (int i = 0; i < count; i++) // Check if this mutation already exists
            {
                Marking marking = historical[i];

                if (marking.source == info.source && marking.destination == info.destination) // If it matches an existing one
                {
                    return marking.order; // Return existing innovation number
                }
            }

            //If the innovation doesn't already exist,

            Marking creation = new Marking(); // Create new marking
            creation.order = historical.Count; // Assign next innovation number
            creation.source = info.source; // Set source
            creation.destination = info.destination; // Set destination

            historical.Add(creation); // Add to history (the innovation tracker)

            return historical.Count - 1; // Return the new innovation number (-1 because it starts from 0)
        }
        public void MutateAll(Genotype genotype) // This functions rolls an RNG, calling other functions based on whether or not they
        //fall within the specified probabilities.
        {
            // Reset mutation probabilities (optional; this overrides any dynamic adjustments)
            MUTATE_LINK = 0.2f; // Probability of adding a new connection
            MUTATE_NODE = 0.1f; // Probability of adding a new node (splitting a connection)
            MUTATE_ENABLE = 0.6f; // Probability of enabling a disabled connection
            MUTATE_DISABLE = 0.2f; // Probability of disabling an enabled connection
            MUTATE_WEIGHT = 2.0f; // Number of attempts to mutate weights (can be >1)

            float p = MUTATE_WEIGHT; // Start with weight mutation passes

            while (p > 0) // As long as p > 0, attempt weight mutation
            {
                float roll = (float)RNG.instance.gen.NextDouble(); // Generate a random float between 0 and 1

                if (roll < p) // If roll is within mutation probability
                {
                    MutateWeight(genotype); // Apply a weight mutation to a random edge
                }

                p--; // Decrease p by 1 (loop is repeated MUTATE_WEIGHT times)
            }

            p = MUTATE_LINK; // Set p to probability of link mutation

            while (p > 0) // Repeat link mutation attempts
            {
                float roll = (float)RNG.instance.gen.NextDouble(); // Roll again

                if (roll < p) // If roll allows mutation
                {
                    MutateLink(genotype); // Try to add a new connection
                }

                p--; // Decrease mutation probability counter
            }

            p = MUTATE_NODE; // Set p to probability of node mutation

            while (p > 0) // Repeat node mutation attempts
            {
                float roll = (float)RNG.instance.gen.NextDouble(); // Roll again

                if (roll < p) // If roll allows mutation
                {
                    MutateNode(genotype); // Try to split an edge by inserting a new node
                }

                p--; // Decrease mutation probability counter
            }

            p = MUTATE_DISABLE; // Set p to probability of disabling a connection

            while (p > 0) // Repeat disable attempts
            {
                float roll = (float)RNG.instance.gen.NextDouble(); // Roll again

                if (roll < p) // If roll allows mutation
                {
                    MutateDisable(genotype); // Disable a random enabled edge
                }

                p--; // Decrease mutation probability counter
            }

            p = MUTATE_ENABLE; // Set p to probability of enabling a connection

            while (p > 0) // Repeat enable attempts
            {
                float roll = (float)RNG.instance.gen.NextDouble(); // Roll again

                if (roll < p) // If roll allows mutation
                {
                    MutateEnable(genotype); // Enable a random disabled edge
                }

                p--; // Decrease mutation probability counter
            }
        }

        public void MutateLink(Genotype genotype) // Attempt to add a new connection between two nodes
        {
            int vertexCount = genotype.vertices.Count; // Count how many nodes the network currently has
            int edgeCount = genotype.edges.Count; // Count how many edges (connections) exist

            List<EdgeInfo> potential = new List<EdgeInfo>(); // Prepare a list to store valid new connections

            // Try all possible pairs of vertices to see if a valid connection can be made
            for (int i = 0; i < vertexCount; i++) // Loop through all possible source nodes
            {
                for (int j = 0; j < vertexCount; j++) // Loop through all possible destination nodes
                {
                    int source = genotype.vertices[i].index; // Get source node index
                    int destination = genotype.vertices[j].index; // Get destination node index

                    VertexInfo.EType t1 = genotype.vertices[i].type; // Get type of source node
                    VertexInfo.EType t2 = genotype.vertices[j].type; // Get type of destination node

                    if (t1 == VertexInfo.EType.OUTPUT || t2 == VertexInfo.EType.INPUT) // Prevent creating backward or invalid connections
                    {
                        continue; // Skip this pair
                    }

                    if (source == destination) // Prevent self-connections (a node connecting to itself)
                    {
                        continue; // Skip this pair
                    }

                    bool search = false; // Flag to check if edge already exists

                    for (int k = 0; k < edgeCount; k++) // Loop through all current edges
                    {
                        EdgeInfo edge = genotype.edges[k]; // Get each edge

                        if (edge.source == source && edge.destination == destination) // If the edge already exists
                        {
                            search = true; // Mark that the edge is found
                            break; // No need to keep searching
                        }
                    }

                    if (!search) // If the edge does not exist yet
                    {
                        float weight = (float)RNG.instance.gen.NextDouble() * 4.0f - 2.0f; // Generate a random weight between -2 and 2
                        EdgeInfo creation = new EdgeInfo(source, destination, weight, true); // Create a new edge with that weight and enable it

                        potential.Add(creation); // Add the new edge to the list of potential mutations
                    }
                }
            }

            if (potential.Count <= 0) // If there are no valid new connections to make
            {
                return; // Exit the function early
            }

            int selection = RNG.instance.gen.Next(0, potential.Count); // Choose one potential new edge at random

            EdgeInfo mutation = potential[selection]; // Select the edge
            mutation.innovation = RegisterMarking(mutation); // Assign it an innovation number (track in history)

            genotype.AddEdge(mutation.source, mutation.destination, mutation.weight, mutation.enabled, mutation.innovation); // Add this new edge to the genotype
        }


        public void MutateNode(Genotype genotype) // Insert a new node by splitting an existing edge
        {
            int edgeCount = genotype.edges.Count; // Get the total number of edges in the genotype

            int selection = RNG.instance.gen.Next(0, edgeCount); // Select a random edge index

            EdgeInfo edge = genotype.edges[selection]; // Get the selected edge

            if (edge.enabled == false) // If the selected edge is already disabled, skip mutation
            {
                return; // Exit the function
            }

            edge.enabled = false; // Disable the original edge (we're about to split it)

            int vertex_new = genotype.vertices[genotype.vertices.Count - 1].index + 1; // Create a new unique index for the new node

            VertexInfo vertex = new VertexInfo(VertexInfo.EType.HIDDEN, vertex_new); // Create a new hidden node with the new index

            EdgeInfo first = new EdgeInfo(edge.source, vertex_new, 1.0f, true); // Create a new edge from the original source to the new node
            EdgeInfo second = new EdgeInfo(vertex_new, edge.destination, edge.weight, true); // Create a new edge from the new node to the original destination

            first.innovation = RegisterMarking(first); // Assign an innovation number to the first new edge
            second.innovation = RegisterMarking(second); // Assign an innovation number to the second new edge

            genotype.AddVertex(vertex.type, vertex.index); // Add the new hidden node to the genotype

            genotype.AddEdge(first.source, first.destination, first.weight, first.enabled, first.innovation); // Add the first new edge to the genotype
            genotype.AddEdge(second.source, second.destination, second.weight, second.enabled, second.innovation); // Add the second new edge to the genotype
        }


        public void MutateEnable(Genotype genotype) // Enable a disabled edge. Create list of candidates(disabled edges) and randomly select one to enable
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

        public void MutateDisable(Genotype genotype) // Disable an enabled edge. Create list of candidates(enabled edges) and randomly select one to disable
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

        public void MutateWeightShift(EdgeInfo edge, float step) // Slightly adjust a weight. Step is set to 0.1, so this will change the weight
        //by either +0.05 or -0.05
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
