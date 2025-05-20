// This file defines the NetworkFactory class, which provides utility methods to generate
// base neural network structures (Genotypes and Phenotypes) used in NEAT evolution.
// It handles setting up initial networks with input/output nodes and optionally adds
// edges (connections) between them. It also supports recurrent and debugging networks.

using System; // Required for system-level features
using System.Collections.Generic; // Provides list data structures
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEAT // Namespace for the NEAT (NeuroEvolution of Augmenting Topologies) project
{
    public class NetworkFactory // Class responsible for creating and initializing base networks
    {
        public static NetworkFactory instance = null; // Singleton instance for global access

        public static void Initialise() // Singleton initializer method
        {
            if (instance == null) // Only create if it doesn't already exist
            {
                instance = new NetworkFactory(); // Create a new instance of NetworkFactory
            }
        }

        public Genotype CreateBaseGenotype(int inputs, int outputs) // Create a simple Genotype with given input/output nodes.
        //the parameters specify the number of desired input and output nodes.
        {
            Genotype network = new Genotype(); // Initialize new Genotype

            for (int i = 0; i < inputs; i++) // Add input nodes
            {
                network.AddVertex(VertexInfo.EType.INPUT, i);
            }

            for (int i = 0; i < outputs; i++) // Add output nodes
            {
                network.AddVertex(VertexInfo.EType.OUTPUT, i + inputs); // Offset output index by input count
            }

            network.AddEdge(0, inputs, 0.0f, true, 0); // Add a single connection from input 0 to first output


            return network; // Return the constructed Genotype
        }

        public void RegisterBaseMarkings(int inputs, int outputs) // Registers innovation numbers for initial connections
        {
            for (int i = 0; i < inputs; i++) // Loop through input nodes
            {
                for (int j = 0; j < outputs; j++) // Loop through output nodes
                {
                    int input = i;
                    int output = j + inputs; //inputs are indexed before outputs, so this addition ensures that the output index is correct.

                    EdgeInfo info = new EdgeInfo(input, output, 0.0f, true); // Define edge

                    Mutation.instance.RegisterMarking(info); // Register innovation number for this connection
                }
            }
        }

        public Genotype CreateBaseRecurrent() // Create a minimal recurrent Genotype (a network with a loop)
        {
            Genotype network = new Genotype(); // Step 1: Create a new, empty Genotype object (neural network blueprint)

            int nodeNum = 0; // Counter to assign unique indices to nodes

            for (int i = 0; i < 1; i++) // Step 2: Add one input node
            {
                network.AddVertex(VertexInfo.EType.INPUT, nodeNum); // Add an input node with index 0
                nodeNum++; // Move to next available index
            }

            for (int i = 0; i < 1; i++) // Step 3: Add one output node
            {
                network.AddVertex(VertexInfo.EType.OUTPUT, nodeNum); // Add an output node with index 1
                nodeNum++; // Move to next available index
            }

            network.AddEdge(0, 1, 0.0f, true, 0); // Step 4: Create a forward connection from input (0) to output (1) with weight 0, enabled, innovation 0
            network.AddEdge(1, 0, 0.0f, true, 1); // Step 5: Add a RECURSIVE (loopback) connection from output (1) to input (0), innovation 1

            Phenotype physicals = new Phenotype(); // Step 6: Instantiate a Phenotype (actual runnable network structure)

            physicals.InscribeGenotype(network); // Step 7: Convert Genotype to Phenotype (copy structure into a network that can be evaluated)
            physicals.ProcessGraph(); // Step 8: Finalize the connection graph (resolve node layers and sort them for evaluation)

            return network; // Step 9: Return the built Genotype with the recurrent structure
        }

        

//continue from here
        public Genotype CreateBuggyNetwork() // Generate a malformed/test Genotype structure for debugging
        {
            Genotype network = new Genotype(); // Create new Genotype

            int nodeNum = 0;

            for (int i = 0; i < 2; i++) // Add two input nodes
            {
                network.AddVertex(VertexInfo.EType.INPUT, nodeNum);
                nodeNum++;
            }

            for (int i = 0; i < 1; i++) // Add one output node
            {
                network.AddVertex(VertexInfo.EType.OUTPUT, nodeNum);
                nodeNum++;
            }

            for (int i = 0; i < 2; i++) // Add two hidden nodes
            {
                network.AddVertex(VertexInfo.EType.HIDDEN, nodeNum);
                nodeNum++;
            }

            // Add various connections (intentionally more complex and possibly cyclic)
            network.AddEdge(0, 2, 0.0f, true, 0);
            network.AddEdge(1, 2, 0.0f, true, 1);
            network.AddEdge(1, 3, 0.0f, true, 2);
            network.AddEdge(3, 2, 0.0f, true, 3);

            Phenotype physicals = new Phenotype(); // Generate phenotype
            physicals.InscribeGenotype(network); // Translate Genotype
            physicals.ProcessGraph(); // Process connections

            return network; // Return the debug network
        }

        public Phenotype CreateBasePhenotype(int inputs, int outputs) // Build a Phenotype (ready-to-run network)
        {
            Phenotype network = new Phenotype(); // Create Phenotype object

            for (int i = 0; i < inputs; i++) // Add input vertices
            {
                network.AddVertex(Vertex.EType.INPUT, i);
            }

            for (int i = 0; i < outputs; i++) // Add output vertices
            {
                network.AddVertex(Vertex.EType.OUTPUT, i + inputs);
            }

            for (int i = 0; i < inputs; i++) // Fully connect input to output
            {
                for (int j = 0; j < outputs; j++)
                {
                    int input = i;
                    int output = j + inputs;

                    network.AddEdge(input, output, 0.0f, true); // Add connection
                }
            }

            return network; // Return the fully connected base phenotype
        }
    }
}
s