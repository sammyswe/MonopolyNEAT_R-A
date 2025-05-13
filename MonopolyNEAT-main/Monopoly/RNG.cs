// RNG.cs
// -------
// This class provides a centralized and reusable random number generator (RNG) instance.
// It supports common shuffling operations needed for gameplay randomness, including:
// - Shuffling cards
// - Shuffling AI players
// - Shuffling NEAT neural network representations (phenotypes/genotypes)
// - Performing synchronized shuffles of paired lists (used for aligned genetic data)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RNG
{
    // Singleton instance to allow global access to a single RNG
    public static RNG instance;

    // Random number generator
    public Random gen;

    // Constructor initializes the random number generator
    public RNG()
    {
        gen = new Random();
    }

    // Initializes the singleton instance (if not already created)
    public static void Initialise()
    {
        if (instance == null)
        {
            instance = new RNG();
        }
    }

    // Shuffles a list of CardEntry objects (e.g., Chance or Community Chest cards)
    public List<MONOPOLY.Board.CardEntry> Shuffle(List<MONOPOLY.Board.CardEntry> cards)
    {
        List<MONOPOLY.Board.CardEntry> shuffle = new List<MONOPOLY.Board.CardEntry>();

        // Randomly select cards and remove them from the input list
        for (int i = 0; i < cards.Count;)
        {
            int r = gen.Next(0, cards.Count);
            shuffle.Add(cards[r]);
            cards.RemoveAt(r);
        }

        return shuffle;
    }

    // Shuffles an array of NeuralPlayer objects (AI players in the game)
    public MONOPOLY.NeuralPlayer[] Shuffle(MONOPOLY.NeuralPlayer[] list)
    {
        List<MONOPOLY.NeuralPlayer> container = new List<MONOPOLY.NeuralPlayer>(list);
        List<MONOPOLY.NeuralPlayer> shuffle = new List<MONOPOLY.NeuralPlayer>();

        for (int i = 0; i < container.Count;)
        {
            int r = gen.Next(0, container.Count);
            shuffle.Add(container[r]);
            container.RemoveAt(r);
        }

        return shuffle.ToArray();
    }

    // Shuffles a list of NEAT phenotypes (trained neural networks ready to act)
    public List<NEAT.Phenotype> Shuffle(List<NEAT.Phenotype> list)
    {
        List<NEAT.Phenotype> shuffle = new List<NEAT.Phenotype>();

        for (int i = 0; i < list.Count;)
        {
            int r = gen.Next(0, list.Count);
            shuffle.Add(list[r]);
            list.RemoveAt(r);
        }

        return shuffle;
    }

    // Shuffles two related lists (phenotypes and genotypes) in the same order
    public void DoubleShuffle(List<NEAT.Phenotype> phen, List<NEAT.Genotype> gene, ref List<NEAT.Phenotype> op, ref List<NEAT.Genotype> og)
    {
        for (int i = 0; i < phen.Count;)
        {
            int r = gen.Next(0, phen.Count);
            op.Add(phen[r]);
            og.Add(gene[r]);
            phen.RemoveAt(r);
            gene.RemoveAt(r);
        }
    }
}