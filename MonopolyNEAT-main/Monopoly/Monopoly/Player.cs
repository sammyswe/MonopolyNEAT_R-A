// Import necessary namespaces for basic system and collection functionality
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Declare the MONOPOLY namespace
namespace MONOPOLY
{
    // Define a base Player class to be inherited by AI or human-controlled players
    public class Player
    {
        // Enum for tracking the current state of the player
        public enum EState
        {
            NORMAL,    // Player is free to move
            JAIL,      // Player is currently in jail
            RETIRED,   // Player is out of the game
        }

        // Enum representing decisions related to purchasing a property
        public enum EBuyDecision
        {
            BUY,        // Player chooses to buy the property
            AUCTION,    // Player chooses to let the property go to auction
        }

        // Enum representing decisions a player can make while in jail
        public enum EJailDecision
        {
            ROLL,       // Attempt to roll doubles to get out
            PAY,        // Pay to get out of jail
            CARD        // Use a Get Out of Jail Free card
        }

        // General yes/no decision enum used in mortgage, advance, etc.
        public enum EDecision
        {
            YES,        // Affirmative decision
            NO          // Negative decision
        }

        // -------------------- Player Properties --------------------

        // The current state of the player (normal, jail, retired)
        public EState state = EState.NORMAL;

        // The player's current position on the board
        public int position = 0;

        // The player's current amount of money
        public int funds = 1500;

        // Number of turns spent in jail
        public int jail = 0;

        // Count of consecutive doubles rolled (used for jail rule)
        public int doub = 0;

        // Number of Get Out of Jail Free cards held
        public int card = 0;

        // List of indices representing properties owned by the player
        public List<int> items;

        // Constructor initializes the owned items list
        public Player()
        {
            items = new List<int>();
        }

        // -------------------- Decision Stubs --------------------

        // Decision logic for whether to buy or auction a property
        public virtual EBuyDecision DecideBuy(int index)
        {
            return EBuyDecision.BUY; // Default behavior: always buy
        }

        // Decision logic for how to respond when in jail
        public virtual EJailDecision DecideJail()
        {
            return EJailDecision.ROLL; // Default behavior: try to roll to get out
        }

        // Decision logic for whether to mortgage a property
        public virtual EDecision DecideMortgage(int index)
        {
            // If the player is in debt, opt to mortgage
            if (funds < 0)
            {
                return EDecision.YES;
            }

            return EDecision.NO; // Otherwise, decline to mortgage
        }

        // Decision logic for whether to unmortgage (advance) a property
        public virtual EDecision DecideAdvance(int index)
        {
            return EDecision.YES; // Default: always unmortgage if prompted
        }

        // Decision logic for how much to bid at auction
        public virtual int DecideAuctionBid(int index)
        {
            return Board.COSTS[index]; // Default: bid the listed cost of the property
        }

        // Decision logic for how many houses to build on a set
        public virtual int DecideBuildHouse(int set)
        {
            return 15; // Default: max build allowed (capped elsewhere if needed)
        }

        // Decision logic for how many houses to sell from a set
        public virtual int DecideSellHouse(int set)
        {
            if (funds < 0)
            {
                return 15; // If in debt, sell as many as possible
            }

            return 0; // Otherwise, sell none
        }

        // Decision logic for whether to offer a trade
        public virtual EDecision DecideOfferTrade()
        {
            return EDecision.NO; // Default: don't offer trades
        }

        // Decision logic for whether to accept a trade
        public virtual EDecision DecideAcceptTrade()
        {
            return EDecision.NO; // Default: don't accept trades
        }
    }
}

