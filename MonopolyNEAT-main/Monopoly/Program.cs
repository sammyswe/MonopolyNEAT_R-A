// Program.cs
// -----------
// Entry point of the Monopoly NEAT AI application. This file:
// - Initializes components (Analytics, NEAT modules, RNG)
// - Loads a saved population of neural networks from file if available
// - Runs a training loop where tournaments are executed and new generations are created
// - Saves the population state after every generation for persistence

using System;
using System.Collections.Generic;
using System.IO;

namespace Monopoly
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize analytics tracking system and assign singleton instance
            Analytics a = new Analytics();
            Analytics.instance = a;

            // Path to the population save file
            string path = "MonopolyNEAT-main/samtest.txt";

            // Initialize random number generator
            RNG.Initialise();

            // Initialize NEAT modules
            NEAT.NetworkFactory.Initialise();
            NEAT.Mutation.Initialise();
            NEAT.Crossover.Initialise();
            NEAT.Population.Initialise();

            // Create tournament manager
            Tournament tournament = new Tournament();

            // Load saved state if file exists, otherwise start fresh
            if (File.Exists(path))
            {
                LoadState(path, ref tournament);
            }
            else
            {
                tournament.Initialise();
            }

            // Run 1000 generations of tournaments
            for (int i = 0; i < 1000; i++)
            {
                tournament.ExecuteTournament();
                NEAT.Population.instance.NewGeneration();
                SaveState(path, tournament);
            }
        }

        // Delimiters used to split serialized save data
        public static char DELIM_MAIN = ';';
        public static char DELIM_COMMA = ',';

        // Saves the current population and tournament state to file
        public static void SaveState(string target, Tournament tournament)
        {
            Console.WriteLine("SAVING POPULATION");

            string build = "";
            string build2 = "";

            // Save generation number and current champion score
            build += NEAT.Population.instance.GENERATION.ToString();
            build += DELIM_MAIN;
            build += tournament.championScore.ToString();
            build += DELIM_MAIN;

            int markings = 0;

            // Save historical mutation markings (innovation tracking)
            for (int i = 0; i < NEAT.Mutation.instance.historical.Count; i++)
            {
                build += NEAT.Mutation.instance.historical[i].order + DELIM_COMMA;
                build += NEAT.Mutation.instance.historical[i].source + DELIM_COMMA;
                build += NEAT.Mutation.instance.historical[i].destination;

                if (i != NEAT.Mutation.instance.historical.Count - 1)
                    build += DELIM_COMMA;

                markings++;
            }

            List<string> net_build = new List<string>();
            int net_count = -1;
            int gene_count = 0;

            build += DELIM_MAIN; // Start of network section

            // Save neural networks, species by species
            for (int i = 0; i < NEAT.Population.instance.species.Count; i++)
            {
                net_build.Add("");
                net_count++;

                // Save species metadata
                net_build[net_count] += NEAT.Population.instance.species[i].topFitness + DELIM_COMMA;
                net_build[net_count] += NEAT.Population.instance.species[i].staleness;

                net_build[net_count] += "&";

                int members = NEAT.Population.instance.species[i].members.Count;

                // Save each network (genotype)
                for (int j = 0; j < members; j++)
                {
                    net_build.Add("");
                    net_count++;
                    gene_count++;

                    Console.WriteLine(gene_count + "/" + NEAT.Population.instance.genetics.Count);

                    NEAT.Genotype genes = NEAT.Population.instance.species[i].members[j];

                    // Save vertices
                    foreach (var vertex in genes.vertices)
                    {
                        net_build[net_count] += vertex.index + DELIM_COMMA + vertex.type + DELIM_COMMA;
                    }

                    net_build[net_count] += '#';

                    // Save edges (connections)
                    foreach (var edge in genes.edges)
                    {
                        net_build[net_count] += edge.source + DELIM_COMMA;
                        net_build[net_count] += edge.destination + DELIM_COMMA;
                        net_build[net_count] += edge.weight + DELIM_COMMA;
                        net_build[net_count] += edge.enabled.ToString() + DELIM_COMMA;
                        net_build[net_count] += edge.innovation + DELIM_COMMA;
                    }

                    if (j != members - 1)
                        net_build[net_count] += "n"; // Next genotype in species
                }

                if (i != NEAT.Population.instance.species.Count - 1)
                    net_build[net_count] += "&"; // Next species
            }

            build2 += DELIM_MAIN;

            // Write everything to file
            using (StreamWriter sw = new StreamWriter(target))
            {
                sw.Write(build);
                foreach (string b in net_build)
                {
                    sw.Write(b);
                }
                sw.Write(build2);
            }

            Console.WriteLine(markings + " MARKINGS");
        }

        // Loads the population and tournament state from a saved file
        public static void LoadState(string location, ref Tournament tournament)
        {
            string load = "";

            using (StreamReader sr = new StreamReader(location))
            {
                load = sr.ReadToEnd();
            }

            string[] parts = load.Split(DELIM_MAIN);

            // Restore generation and champion score
            int gen = int.Parse(parts[0]);
            float score = float.Parse(parts[1]);

            NEAT.Population.instance.GENERATION = gen;
            tournament.championScore = score;

            // Restore mutation history
            string[] markingParts = parts[2].Split(DELIM_COMMA);
            for (int i = 0; i < markingParts.Length; i += 3)
            {
                var recreation = new NEAT.Marking
                {
                    order = int.Parse(markingParts[i]),
                    source = int.Parse(markingParts[i + 1]),
                    destination = int.Parse(markingParts[i + 2])
                };

                NEAT.Mutation.instance.historical.Add(recreation);
            }

            // Restore species and networks
            string[] speciesParts = parts[3].Split('&');
            for (int x = 0; x < speciesParts.Length; x += 2)
            {
                string[] firstParts = speciesParts[x].Split(DELIM_COMMA);

                var species = new NEAT.Species
                {
                    topFitness = float.Parse(firstParts[0]),
                    staleness = int.Parse(firstParts[1])
                };

                NEAT.Population.instance.species.Add(species);

                string[] networkParts = speciesParts[x + 1].Split('n');
                foreach (string network in networkParts)
                {
                    var genotype = new NEAT.Genotype();

                    string[] nparts = network.Split('#');
                    string[] vparts = nparts[0].Split(',');
                    for (int j = 0; j < vparts.Length - 1; j += 2)
                    {
                        int index = int.Parse(vparts[j]);
                        var type = (NEAT.VertexInfo.EType)Enum.Parse(typeof(NEAT.VertexInfo.EType), vparts[j + 1]);
                        genotype.AddVertex(type, index);
                    }

                    string[] eparts = nparts[1].Split(',');
                    for (int j = 0; j < eparts.Length - 1; j += 5)
                    {
                        int source = int.Parse(eparts[j]);
                        int destination = int.Parse(eparts[j + 1]);
                        float weight = float.Parse(eparts[j + 2]);
                        bool enabled = bool.Parse(eparts[j + 3]);
                        int innovation = int.Parse(eparts[j + 4]);

                        genotype.AddEdge(source, destination, weight, enabled, innovation);
                    }

                    species.members.Add(genotype);
                    NEAT.Population.instance.genetics.Add(genotype);
                }
            }

            // Finalize population structure
            NEAT.Population.instance.InscribePopulation();
        }
    }
}
