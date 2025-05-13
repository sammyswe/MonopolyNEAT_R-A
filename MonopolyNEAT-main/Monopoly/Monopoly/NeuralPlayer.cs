// NeuralPlayer extends the abstract Player class to implement behavior driven by a NEAT neural network
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONOPOLY
{
    // A NeuralPlayer is a Monopoly player whose decisions are made using a neural network (NEAT phenotype)
    public class NeuralPlayer : Player
    {
        // Neural network controlling this player
        public NEAT.Phenotype network;

        // Adapter providing access to normalized game state
        public NetworkAdapter adapter;

        // Constructor initializes the player's owned property list
        public NeuralPlayer()
        {
            items = new List<int>(); // Holds indexes of properties owned
        }

        // Decision to buy or auction a property
        public override EBuyDecision DecideBuy(int index)
        {
            float[] Y = network.Propagate(adapter.pack); // Feed current game state into neural net

            if (Y[0] > 0.5f) // If first output neuron > 0.5, buy
            {
                return EBuyDecision.BUY;
            }

            return EBuyDecision.AUCTION; // Otherwise, auction
        }

        // Decide what to do in jail: use card, roll, or pay
        public override EJailDecision DecideJail()
        {
            float[] Y = network.Propagate(adapter.pack);

            if (Y[1] < 0.333f)
            {
                return EJailDecision.CARD; // Use get out of jail card
            }
            else if (Y[1] < 0.666f)
            {
                return EJailDecision.ROLL; // Attempt to roll doubles
            }

            return EJailDecision.PAY; // Otherwise, pay the fine
        }

        // Decide whether to mortgage a property
        public override EDecision DecideMortgage(int index)
        {
            float[] Y = network.Propagate(adapter.pack);

            if (Y[2] > 0.5f)
            {
                return EDecision.YES;
            }

            return EDecision.NO;
        }

        // Decide whether to unmortgage (advance) a property
        public override EDecision DecideAdvance(int index)
        {
            float[] Y = network.Propagate(adapter.pack);

            if (Y[3] > 0.5f)
            {
                return EDecision.YES;
            }

            return EDecision.NO;
        }

        // Decide how much to bid in an auction
        public override int DecideAuctionBid(int index)
        {
            float[] Y = network.Propagate(adapter.pack);

            float result = Y[4];
            float money = adapter.ConvertMoneyValue(result); // Convert normalized value to dollar amount

            Monopoly.Analytics.instance.MakeBid(index, (int)money); // Record analytics

            return (int)money; // Return integer bid
        }

        // Decide how much to spend on building houses for a set
        public override int DecideBuildHouse(int set)
        {
            float[] Y = network.Propagate(adapter.pack);

            float result = Y[5];
            float money = adapter.ConvertHouseValue(result); // Convert normalized to count of houses

            return (int)money;
        }

        // Decide how many houses to sell from a set
        public override int DecideSellHouse(int set)
        {
            float[] Y = network.Propagate(adapter.pack);

            float result = Y[6];
            float money = adapter.ConvertHouseValue(result);

            return (int)money;
        }

        // Decide whether to propose a trade
        public override EDecision DecideOfferTrade()
        {
            float[] Y = network.Propagate(adapter.pack);

            if (Y[7] > 0.5f)
            {
                return EDecision.YES;
            }

            return EDecision.NO;
        }

        // Decide whether to accept a proposed trade
        public override EDecision DecideAcceptTrade()
        {
            float[] Y = network.Propagate(adapter.pack);

            if (Y[8] > 0.5f)
            {
                return EDecision.YES;
            }

            return EDecision.NO;
        }
    }
}
