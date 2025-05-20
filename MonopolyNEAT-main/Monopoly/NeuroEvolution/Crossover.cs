// This file defines the Crossover class used in the NEAT (NeuroEvolution of Augmenting Topologies) algorithm
// It handles producing offspring genotypes from two parents and calculating genetic distance (speciation) between them.

using System; // Required for Math and basic types
using System.Collections.Generic; // Required for List<T>
using System.Linq; // Required for LINQ operations (not directly used here)
using System.Text; // Required for text encoding (not used here)
using System.Threading.Tasks; // Required for task-based async operations (not used here)

namespace NEAT // Declares namespace NEAT
{
    public class Crossover // Crossover class to manage gene mixing and speciation
    {
        public static Crossover instance = null; // There will only be one instance of the crossover class used across the entire
        //program. Crossover is just a toolbox for mixing and comparing genes.
        public float CROSSOVER_CHANCE = 0.75f; // Chance that a new generation of neural network will be created by mixing two 
        //parents genes together. Alternatively, the fitter of two parents will be taken AND/OR mutations will be added to the fitter of two
        //parents. If you set this value higher, there will be more gene crossover early on.

        /*
        The below coefficients are used to measure how different two neural networks are.
        There is a formula:
        
        distance = C1 * E + C2 * D + C3 * W
        E - Excess genes count. Excess genes are new gene
        D - Disjoint genes count
        W - Average weight difference between matching genes.

        How do we check if a gene is excess or disjoint?

        1. Check innovation number (number used to track when the gene was added, higher innovation number = created more recently)
        2. take lower innovation number (comparison threshold) 
        3. innovation number <= comparison threshold is a disjoint gene
        4. innovation number > comparison threshold is an excess gene

        This formula is used to seperate genes into species so that there is a wider range of different networks
        and there is no early convergence (networks dont do the same strategy.)


        */
        public float C1 = 1.0f; // Weighting for excess genes. 

        /*

        Increases distance if two networks have lots of extra genes at the end.
        Higher C1 means: networks with many extra genes are considered more different.
        Lower C1 means: we don’t care much about extra genes.
        Higher C1 = favors simpler genomes (less bloating)
        Lower C1 = tolerates large, complex genomes
        
        */

        public float C2 = 1.0f; // Weighting for disjoint genes. 

        /*

        Increases distance if one network has lots of extra genes in the middle.
        Higher C2 means: we strongly penalize differences in structure.
        Lower C2 means: we allow more variety in how genes are wired.
        Higher C2 = encourages structure similarity
        Lower C2 = allows experimentation in structure
        
        */


        public float C3 = 0.4f; // Weighting for strenght of connection between nodes.

        /*
        Measures how different the strengths of connections are.
        Higher C3 means: small differences in weights matter a lot.
        Lower C3 means: we mostly care about which connections exist, not their strength.
        Higher C3 = favors weight precision
        Lower C3 = focuses more on structure than exact values

        */
        public float DISTANCE = 1.0f;// Speciation distance threshold (unused here)

        public static void Initialise() // Initializes the singleton instance
        {
            if (instance == null) // Only create if it doesn't exist
            {
                instance = new Crossover(); // Instantiate singleton
            }
        }

        public Crossover() // Constructor for the Crossover class
        {
           
        }//this is empty because the constructor is required to make an instance of the class even though there is no special
        //setup for the class.

        public Genotype ProduceOffspring(Genotype first, Genotype second) // Create a new genotype by crossing two parents
        {
            List<EdgeInfo> copy_first = new List<EdgeInfo>(); // Create list to store all the edges of both genotypes
            List<EdgeInfo> copy_second = new List<EdgeInfo>(); 

            copy_first.AddRange(first.edges); // Copy all edges from first
            copy_second.AddRange(second.edges); // Copy all edges from second

            List<EdgeInfo> match_first = new List<EdgeInfo>(); // Storing the matching edges from both lists. We keep two lists here even though the 
            List<EdgeInfo> match_second = new List<EdgeInfo>(); // contents will have the same edges because we need to be able to store
                                                                // that they may have different weights/enabled flags.

            List<EdgeInfo> disjoint_first = new List<EdgeInfo>(); // Disjoint genes are non matching genes in the middle (smaller than comparison threshold.)
            List<EdgeInfo> disjoint_second = new List<EdgeInfo>(); // Create a list of disjoint genes for both genotypes (will fill later)

            List<EdgeInfo> excess_first = new List<EdgeInfo>(); // Excess genes are non matching genes on the end of a genotype (greater than comparison threshold)
            List<EdgeInfo> excess_second = new List<EdgeInfo>(); // Create a list of excess genes for both genotypes (will fill later)

            int genes_first = first.edges.Count; // Total edge count in first
            int genes_second = second.edges.Count; // Total edge count in second

            int invmax_first = first.edges[first.edges.Count - 1].innovation; // Max innovation number from first
            int invmax_second = second.edges[second.edges.Count - 1].innovation; // Max innovation number from second

            int invmin = invmax_first > invmax_second ? invmax_second : invmax_first; // This finds the comparison threshold (lower of the two innovation numbers)

            for (int i = 0; i < genes_first; i++) // Loop over each edge in first
            {
                for (int j = 0; j < genes_second; j++) // Loop over each edge in second
                {
                    EdgeInfo info_first = copy_first[i]; // Populates the list of unmatched genes
                    EdgeInfo info_second = copy_second[j]; // genes will be removed when they are found to match

                    //matching genes
                    if (info_first.innovation == info_second.innovation) // If innovation numbers match
                    {
                        match_first.Add(info_first); // Add to matching list for first
                        match_second.Add(info_second); // Add to matching list for second

                        copy_first.Remove(info_first); // Remove from unmatched list
                        copy_second.Remove(info_second); // Remove from unmatched list

                        i--; // Adjust loop index due to removal
                        genes_first--; // Decrement count
                        genes_second--; // Decrement count
                        break; // Break inner loop
                    }
                }
            }

            //continue from here.

            for (int i = 0; i < copy_first.Count; i++) // Assign unmatched genes to either disjoint or excess in first genotype
            {
                if (copy_first[i].innovation > invmin) // Check vs comparison threshold
                {
                    excess_first.Add(copy_first[i]); // Considered excess genes (on the end)
                }
                else
                {
                    disjoint_first.Add(copy_first[i]); // Otherwise disjoint
                }
            }

            for (int i = 0; i < copy_second.Count; i++) // Assign unmatched genes to either disjoint or excess in second genotype
            {
                if (copy_second[i].innovation > invmin) // Higher than other parent's max
                {
                    excess_second.Add(copy_second[i]); // Considered excess
                }
                else
                {
                    disjoint_second.Add(copy_second[i]); // Otherwise disjoint
                }
            }

            Genotype child = new Genotype(); // Create new genotype called child

            int matching = match_first.Count; // Number of matching edges (the match_first and match_second will be the same)

            for (int i = 0; i < matching; i++) // Loop over matches
            {
                int roll = RNG.instance.gen.Next(0, 2); // 0 is first, 1 is second
                
                if (roll == 0 || !match_second[i].enabled) // If the edge is disabled in the second or we roll a 0, then we copy the egde from parent 1
                {
                    child.AddEdge(match_first[i].source, match_first[i].destination, match_first[i].weight, match_first[i].enabled, match_first[i].innovation); // Add edge from first
                }
                else //otherwise we copy the edge from the second.
                {
                    child.AddEdge(match_second[i].source, match_second[i].destination, match_second[i].weight, match_second[i].enabled, match_second[i].innovation); // 
                }
            }

            for (int i = 0; i < disjoint_first.Count; i++) // Add all disjoint genes from first. We only take disjoint and excess genes from the most fit parent.
            {
                child.AddEdge(disjoint_first[i].source, disjoint_first[i].destination, disjoint_first[i].weight, disjoint_first[i].enabled, disjoint_first[i].innovation);
            }

            for (int i = 0; i < excess_first.Count; i++) // Add all excess genes from first
            {
                child.AddEdge(excess_first[i].source, excess_first[i].destination, excess_first[i].weight, excess_first[i].enabled, excess_first[i].innovation);
            }

            child.SortEdges(); // Sort the child’s edges by innovation number

            List<int> ends = new List<int>(); // Initialise empty list to track non-hidden vertices

            int vertexCount = first.vertices.Count; // Count of vertices in first

            for (int i = 0; i < first.vertices.Count; i++) // Loop through all vertices in first
            {
                VertexInfo vertex = first.vertices[i]; // Get vertex

                if (vertex.type == VertexInfo.EType.HIDDEN) // If we reach a hidden vertex, exit the for loop
                {
                    break; // Hidden layer vertices come later
                }
                //if the vertex is not hidden
                ends.Add(vertex.index); // Add to list of visible vertices
                child.AddVertex(vertex.type, vertex.index); // Add the vertex to the child genotype
            }

            AddUniqueVertices(child, ends); // Add all hidden nodes

            child.SortVertices(); // Sort vertices by index

            return child; // Return resulting genotype
        }

        public void AddUniqueVertices(Genotype genotype, List<int> ends) // Adds hidden nodes to a genotype
        {
            List<int> unique = new List<int>(); // initialise empty list to store hidden vertices

            int edgeCount = genotype.edges.Count; // We will populate the vertices from the sorted edge list we already have. 

            for (int i = 0; i < edgeCount; i++) // Loop through edges
            {
                EdgeInfo info = genotype.edges[i]; // Get edge

                if (!ends.Contains(info.source) && !unique.Contains(info.source)) // If the source isn't already known (we have added input/output nodes)
                { //and not already added to the vertices list
                    unique.Add(info.source); //Add the source to the vertex list
                }

                if (!ends.Contains(info.destination) && !unique.Contains(info.destination)) // Do the same for the destination
                {
                    unique.Add(info.destination);
                }
            }

            int uniques = unique.Count; // Count of unique hidden nodes

            for (int i = 0; i < uniques; i++) // Add them all to the instance of the child. 
            {
                genotype.AddVertex(VertexInfo.EType.HIDDEN, unique[i]);
            }
        }

        public float SpeciationDistance(Genotype first, Genotype second) // Measures distance between two genotypes
        {
            List<EdgeInfo> copy_first = new List<EdgeInfo>(); // Empty list to store first parent edges
            List<EdgeInfo> copy_second = new List<EdgeInfo>(); // Empty list to store second parent edges

            copy_first.AddRange(first.edges); // Populate lists
            copy_second.AddRange(second.edges);

            List<EdgeInfo> match_first = new List<EdgeInfo>(); // Matching edges from the parents (these two lists will be the same nodes)
            List<EdgeInfo> match_second = new List<EdgeInfo>(); // They may have different weights, enablement settings.

            List<EdgeInfo> disjoint_first = new List<EdgeInfo>(); // Empty list to store disjoint edges from the first
            List<EdgeInfo> disjoint_second = new List<EdgeInfo>(); // Empty list to store disjoint edges from the second

            List<EdgeInfo> excess_first = new List<EdgeInfo>(); // Empty list to store excess edges from the first
            List<EdgeInfo> excess_second = new List<EdgeInfo>(); // Empty list to store excess edges from the second

            int genes_first = first.edges.Count; // Edge count for first
            int genes_second = second.edges.Count; // Edge count for second

            int invmax_first = first.edges[first.edges.Count - 1].innovation; // Max innovation in first
            int invmax_second = second.edges[second.edges.Count - 1].innovation; // Max innovation in second

            int invmin = invmax_first > invmax_second ? invmax_second : invmax_first; // Calculate comparison threshold 

            float diff = 0.0f; // Sum of weight differences

            for (int i = 0; i < genes_first; i++) // Check for matching edges to calculate weight difference
            {
                for (int j = 0; j < genes_second; j++)
                {
                    EdgeInfo info_first = copy_first[i];
                    EdgeInfo info_second = copy_second[j];

                    //matching genes
                    if (info_first.innovation == info_second.innovation)
                    {
                        float weightDiff = Math.Abs(info_first.weight - info_second.weight); // Weight difference
                        diff += weightDiff; // Accumulate

                        match_first.Add(info_first); // Add both to the matches list
                        match_second.Add(info_second);

                        copy_first.Remove(info_first); // Remove matched nodes from copy list so after we are done
                        copy_second.Remove(info_second); //we will have the list of unmatched nodes

                        i--; // Adjust index due to removal
                        genes_first--;
                        genes_second--;
                        break; // we break the inner loop here because we are moving onto the next 
                        //edge from the first parent. We incrementally check each edge in parent 1
                        //looping through all edges in parent 2 to check for a match.
                    }
                }
            }

            for (int i = 0; i < copy_first.Count; i++) // Separate excess/disjoint in first parent
            {
                if (copy_first[i].innovation > invmin)
                {
                    excess_first.Add(copy_first[i]);
                }
                else
                {
                    disjoint_first.Add(copy_first[i]);
                }
            }

            for (int i = 0; i < copy_second.Count; i++) // Separate excess/disjoint in second parent
            {
                if (copy_second[i].innovation > invmin)
                {
                    excess_second.Add(copy_second[i]);
                }
                else
                {
                    disjoint_second.Add(copy_second[i]);
                }
            }

            int match = match_first.Count; // Matching gene count
            int disjoint = disjoint_first.Count + disjoint_second.Count; // Total disjoint
            int excess = excess_first.Count + excess_second.Count; // Total excess

            int n = Math.Max(first.edges.Count, second.edges.Count); // Normalize by larger genome
            // we do this to make the disjoint and excess gene count relative to the size of the network
            //a difference of 4 edges means a lot in a network of 10 edges, but a lot less in a network
            //with a thousand edges.

            float E = excess / (float)n; // Normalized excess
            float D = disjoint / (float)n; // Normalized disjoint
            float W = diff / (float)match; // Avg weight diff

            return E * C1 + D * C2 + W * C3; // NEAT distance formula
        }
    }
}
