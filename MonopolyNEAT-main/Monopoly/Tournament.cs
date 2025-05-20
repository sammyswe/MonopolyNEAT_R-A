// Tournament.cs
// --------------
// Manages the execution of a full NEAT training generation using tournaments.
// Each tournament runs multiple games between neural networks (Phenotypes) in brackets of 4 players.
// Winners advance, losers are eliminated. Champion is chosen and fitness is assigned based on performance.
// Uses multithreading to parallelize game simulations for speed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monopoly
{
    public class Tournament
    {
        public static int TOURNAMENT_SIZE = 256; // Total number of networks per generation
        public static int ROUND_SIZE = 2000;     // Number of games each bracket plays per round

        public static int WORKERS = 5;          // Threads used per bracket
        public static int BATCH_SIZE = 5;       // Games per worker thread

        public NEAT.Genotype champion = null;    // The winning genotype
        public float championScore = 0.0f;       // Fitness score of the winner

        public List<NEAT.Phenotype> contestants;     // Active networks for play
        public List<NEAT.Genotype> contestants_g;    // Corresponding genotypes

        public Tournament()
        {
            contestants = new List<NEAT.Phenotype>();
            contestants_g = new List<NEAT.Genotype>();
        }

        // Create the initial population of neural networks
        public void Initialise()
        {
            int INPUTS = 126;
            int OUTPUTS = 9;

            NEAT.Population.instance.GenerateBasePopulation(TOURNAMENT_SIZE, INPUTS, OUTPUTS);
        }

        // Runs an entire tournament (until one winner remains)
        public void ExecuteTournament()
        {
            Console.WriteLine("TOURNAMENT #" + NEAT.Population.instance.GENERATION);

            contestants.Clear();
            contestants_g.Clear();

            // Reset network scores and collect all contestants
            for (int i = 0; i < TOURNAMENT_SIZE; i++)
            {
                NEAT.Population.instance.genetics[i].bracket = 0;
                NEAT.Population.instance.population[i].score = 0.0f;

                contestants.Add(NEAT.Population.instance.population[i]);
                contestants_g.Add(NEAT.Population.instance.genetics[i]);
            }

            // Keep reducing contestants by running rounds until one is left
            while (contestants.Count > 1)
            {
                ExecuteTournamentRound();
            }

            // Assign fitness based on how far each network advanced
            for (int i = 0; i < TOURNAMENT_SIZE; i++)
            {
                float top = champion != null ? champion.bracket : 0.0f;
                float diff = NEAT.Population.instance.genetics[i].bracket - top;
                NEAT.Population.instance.genetics[i].fitness = championScore + diff * 5;
            }

            // Set final champion
            champion = contestants_g[0];
            championScore = contestants_g[0].fitness;
        }

        // Executes one round of the tournament (many brackets)
        public void ExecuteTournamentRound()
        {
            Console.WriteLine("ROUND SIZE " + contestants.Count);

            List<NEAT.Phenotype> cs = new List<NEAT.Phenotype>();
            List<NEAT.Genotype> cs_g = new List<NEAT.Genotype>();

            RNG.instance.DoubleShuffle(contestants, contestants_g, ref cs, ref cs_g);

            for (int i = 0; i < TOURNAMENT_SIZE; i++)
            {
                NEAT.Population.instance.population[i].score = 0.0f;
            }

            contestants = cs;
            contestants_g = cs_g;

            // Process in brackets of 4 contestants
            for (int i = 0; i < contestants.Count; i += 4)
            {
                int played = 0;
                Console.WriteLine("BRACKET (" + (i / 4) + ")");

                // Simulate games until ROUND_SIZE games are played
                while (played < ROUND_SIZE)
                {
                    Console.WriteLine("Initialised Workers");

                    Thread[] workers = new Thread[WORKERS];

                    // Launch worker threads
                    for (int t = 0; t < WORKERS; t++)
                    {
                        workers[t] = new Thread(() => PlayGameThread(this, i));
                        workers[t].Start();
                    }

                    // Wait for all workers to finish
                    for (int t = 0; t < WORKERS; t++)
                    {
                        workers[t].Join();
                    }

                    played += WORKERS * BATCH_SIZE;

                    // Output analytics after each batch
                    for (int c = 0; c < 40; c++)
                    {
                        Console.WriteLine("index: " + c + ", " + String.Format("{0:0.000}", Monopoly.Analytics.instance.ratio[c]));
                    }
                }

                // Find best player in the bracket
                int mi = 0;
                float ms = contestants[i].score;

                for (int j = 1; j < 4; j++)
                {
                    if (ms < contestants[i + j].score)
                    {
                        mi = j;
                        ms = contestants[i + j].score;
                    }
                }

                // Promote winner, eliminate others
                for (int j = 0; j < 4; j++)
                {
                    if (j == mi)
                    {
                        contestants_g[i + j].bracket++;
                        continue;
                    }
                    contestants[i + j] = null;
                }
            }

            // Remove eliminated players
            for (int i = 0; i < contestants.Count; i++)
            {
                if (contestants[i] == null)
                {
                    contestants.RemoveAt(i);
                    contestants_g.RemoveAt(i);
                    i--;
                }
            }
        }

        // Runs a game between 4 players inside a thread
        public static void PlayGameThread(Tournament instance, int i)
        {
            for (int game = 0; game < BATCH_SIZE; game++)
            {
                // Setup board and network adapter
                NetworkAdapter adapter = new NetworkAdapter();
                MONOPOLY.Board board = new MONOPOLY.Board(adapter);

                // Assign networks to players
                board.players[0].network = instance.contestants[i];
                board.players[1].network = instance.contestants[i + 1];
                board.players[2].network = instance.contestants[i + 2];
                board.players[3].network = instance.contestants[i + 3];

                // Share adapter for all players
                board.players[0].adapter = adapter;
                board.players[1].adapter = adapter;
                board.players[2].adapter = adapter;
                board.players[3].adapter = adapter;

                // Randomize turn order for fairness
                board.players = RNG.instance.Shuffle(board.players);

                // Simulate game until it ends
                MONOPOLY.Board.EOutcome outcome = MONOPOLY.Board.EOutcome.ONGOING;
                while (outcome == MONOPOLY.Board.EOutcome.ONGOING)
                {
                    outcome = board.Step();
                }

                // Assign score to winner and update property win stats
                if (outcome == MONOPOLY.Board.EOutcome.WIN1)
                {
                    lock (board.players[0].network)
                        board.players[0].network.score += 1.0f;

                    foreach (var prop in board.players[0].items)
                    {
                        lock (Monopoly.Analytics.instance.wins)
                            Monopoly.Analytics.instance.MarkWin(prop);
                    }
                }
                else if (outcome == MONOPOLY.Board.EOutcome.WIN2)
                {
                    lock (board.players[1].network)
                        board.players[1].network.score += 1.0f;

                    foreach (var prop in board.players[1].items)
                    {
                        lock (Monopoly.Analytics.instance.wins)
                            Monopoly.Analytics.instance.MarkWin(prop);
                    }
                }
                else if (outcome == MONOPOLY.Board.EOutcome.WIN3)
                {
                    lock (board.players[2].network)
                        board.players[2].network.score += 1.0f;

                    foreach (var prop in board.players[2].items)
                    {
                        lock (Monopoly.Analytics.instance.wins)
                            Monopoly.Analytics.instance.MarkWin(prop);
                    }
                }
                else if (outcome == MONOPOLY.Board.EOutcome.WIN4)
                {
                    lock (board.players[3].network)
                        board.players[3].network.score += 1.0f;

                    foreach (var prop in board.players[3].items)
                    {
                        lock (Monopoly.Analytics.instance.wins)
                            Monopoly.Analytics.instance.MarkWin(prop);
                    }
                }
                else if (outcome == MONOPOLY.Board.EOutcome.DRAW)
                {
                    lock (board.players)
                    {
                        foreach (var player in board.players)
                        {
                            player.network.score += 0.25f;
                        }
                    }
                }
            }
        }
    }
}