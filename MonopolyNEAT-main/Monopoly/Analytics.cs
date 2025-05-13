using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monopoly
{
    // The Analytics class is used to collect and analyze gameplay data
    // for each property on the board. It tracks statistics like bids,
    // average bid price, trades, and win contribution to help evaluate
    // what strategies lead to success.
    public class Analytics
    {
        // Singleton instance for global access
        public static Analytics instance = null;

        // Tracks how many times a bid was made on each property
        public int[] bids;

        // Total money spent in bids per property
        public int[] money;

        // Average money spent per bid on each property
        public float[] average;

        // Ratio of average bid to actual property cost
        public float[] price;

        // Number of trades involving each property
        public int[] trades;

        // Possibly counts direct exchanges (e.g., swaps without money)
        public int[] exchanges;

        // Number of times each property was owned by a winning player
        public int[] wins;

        // Highest number of wins for any property (used for normalization)
        public int max = 0;

        // Lowest number of wins among non-zero win counts (used for normalization)
        public int min = 0;

        // Normalized win contribution ratio for each property (0 to 1 scale)
        public float[] ratio;

        // Constructor initializes all arrays for 40 board positions
        public Analytics()
        {
            bids = new int[40];
            money = new int[40];
            average = new float[40];
            price = new float[40];

            trades = new int[40];
            exchanges = new int[40];

            wins = new int[40];
            ratio = new float[40];
        }

        // Called when a player bids on a property
        public void MakeBid(int index, int bid)
        {
            // Increment bid count for that property
            bids[index]++;

            // Add bid amount to total money spent
            money[index] += bid;

            // Recalculate average bid
            average[index] = money[index] / bids[index];

            // Calculate price ratio (average bid / actual cost)
            price[index] = average[index] / MONOPOLY.Board.COSTS[index];
        }

        // Called when a trade involving a property occurs
        public void MadeTrade(int index)
        {
            trades[index]++;
        }

        // Called when a winning player owns a property
        public void MarkWin(int index)
        {
            wins[index]++;

            // Update max win count if needed
            if (max < wins[index])
            {
                max = wins[index];
            }

            // Recalculate min win count (ignoring zeros)
            int tempMin = int.MaxValue;

            for (int i = 0; i < 40; i++)
            {
                if (wins[i] != 0 && wins[i] < tempMin)
                {
                    tempMin = wins[i];
                }
            }

            // Normalize win ratio for the property
            ratio[index] = (float)(wins[index] - tempMin) / (float)(max - tempMin);
        }
    }
}
