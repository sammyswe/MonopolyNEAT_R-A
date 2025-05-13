// Board.cs (MONOPOLY)
// -------------------
// This is the core game engine class responsible for running Monopoly games between neural network agents.
// It handles player movement, game state, auctions, property ownership, card effects, trading, turn logic, and endgame detection.
// Key rules like mortgages, jail behavior, and house building are encoded here.
// Neural network agents interact via the NetworkAdapter, and receive state info and make decisions through abstracted calls.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MONOPOLY
{
    public class Board
    {
        // ENUMS (used throughout logic for clarity)
        //------------------------------------------------------

        // Possible game outcomes for ending a game
        public enum EOutcome
        {
            ONGOING,  // game is still being played
            DRAW,     // game ended in a draw
            WIN1,     // player 1 won
            WIN2,     // player 2 won
            WIN3,     // player 3 won
            WIN4      // player 4 won
        }

        // Game mode: only one mode (ROLL) currently used
        public enum EMode
        {
            ROLL,
        }

        // Each type of tile on the board
        public enum ETile
        {
            NONE,      // blank tile or start
            PROPERTY,  // color-set property
            TRAIN,     // train station
            UTILITY,   // water/electric
            CHANCE,    // chance card
            CHEST,     // community chest
            TAX,       // tax tile
            JAIL       // jail tile
        }

        // Types of card actions from Chance/Chest
        public enum ECard
        {
            ADVANCE,     // move to position
            RAILROAD2,   // move to nearest railroad and pay double rent
            UTILITY10,   // move to utility and pay 10x dice
            REWARD,      // gain money
            CARD,        // get out of jail free
            BACK3,       // move back 3 spaces
            JAIL,        // go to jail
            REPAIRS,     // pay per house/hotel owned
            STREET,      // collect for each property
            FINE,        // pay fine
            CHAIRMAN,    // pay each player
            BIRTHDAY     // receive from each player
        }

        // Each card on a Chance or Chest deck
        public class CardEntry
        {
            public ECard card; // The type of card action
            public int val;    // The value/target associated with the action

            public CardEntry(ECard c, int v)
            {
                card = c;
                val = v;
            }
        }

        //------------------------------------------------------
        // CONSTANTS
        //------------------------------------------------------

        public static int PLAYER_COUNT = 4;              // Total number of players in game
        public static int BANK_INDEX = -1;               // Special index for bank-owned properties
        public static int BOARD_LENGTH = 40;             // Number of tiles on the board
        public static int STALEMATE_TURN = 300;          // Max turn count before draw is declared

        public static int GO_BONUS = 200;                // Collect for passing GO
        public static int GO_LANDING_BONUS = 200;        // Collect for landing directly on GO

        public static int JAIL_INDEX = 10;               // Tile index for jail
        public static int JAIL_PENALTY = 50;             // Pay to leave jail

        public static float MORTGAGE_INTEREST = 1.1f;    // Interest when unmortgaging

        // Rent table for all property sets [set, houses]
        public static int[,] PROPERTY_PENALTIES = new int[16, 6]
        {{2, 10, 30, 90, 160, 250 },
         { 4, 20, 60, 180, 320, 450 },
         { 6, 30, 90, 270, 400, 550 },
         { 8, 40, 100, 300, 450, 600 },
         { 10, 50, 150, 450, 625, 750 },
         { 12, 60, 180, 500, 700, 900 },
         { 14, 70, 200, 550, 750, 950 },
         { 16, 80, 220, 600, 800, 1000 },
         { 18, 90, 250, 700, 875, 1050 },
         { 20, 100, 300, 750, 925, 1100 },
         { 22, 110, 330, 800, 975, 1150 },
         { 22, 120, 360, 850, 1025, 1200 },
         { 26, 130, 390, 900, 1100, 1275 },
         { 28, 150, 450, 1000, 1200, 1400 },
         { 35, 175, 500, 1100, 1300, 1500 },
         { 50, 200, 600, 1400, 1700, 2000 }};

        // Utility tile positions and rent multipliers
        public static int[] UTILITY_POSIIONS = new int[2] { 12, 28 };
        public static int[] UTILITY_PENALTIES = new int[2] { 4, 10 };  // multiplier of roll (owned 1 or both)

        // Train positions and rent amounts based on how many stations owned
        public static int[] TRAIN_POSITIONS = new int[4] { 5, 15, 25, 35 };
        public static int[] TRAIN_PENALTIES = new int[4] { 25, 50, 100, 200 };

        // What kind of tile each board index is
        public static ETile[] TYPES = new ETile[40] 
        {ETile.NONE, ETile.PROPERTY, ETile.CHEST, ETile.PROPERTY, ETile.TAX, ETile.TRAIN, ETile.PROPERTY, ETile.CHANCE, ETile.PROPERTY, ETile.PROPERTY, ETile.NONE,
         ETile.PROPERTY, ETile.UTILITY, ETile.PROPERTY, ETile.PROPERTY, ETile.TRAIN, ETile.PROPERTY, ETile.CHEST, ETile.PROPERTY, ETile.PROPERTY, ETile.NONE,
         ETile.PROPERTY, ETile.CHANCE, ETile.PROPERTY, ETile.PROPERTY, ETile.TRAIN, ETile.PROPERTY, ETile.PROPERTY, ETile.UTILITY, ETile.PROPERTY, ETile.JAIL,
         ETile.PROPERTY, ETile.PROPERTY, ETile.CHEST, ETile.PROPERTY, ETile.TRAIN, ETile.CHANCE, ETile.PROPERTY, ETile.TAX, ETile.PROPERTY};

        // Property tile prices (used to buy, mortgage, build)
        public static int[] COSTS = new int[40] { 0, 60, 0, 60, 200, 200, 100, 0, 100, 120, 0, 140, 150, 140, 160, 200, 180, 0, 180, 200, 0, 220, 0, 220, 240, 200, 260, 260, 150, 280, 0, 300, 300, 0, 320, 200, 0, 250, 100, 400 };

        // House build costs for each property set (used in building houses evenly)
        public static int[] BUILD = new int[16] { 50, 50, 50, 50, 100, 100, 100, 100, 150, 150, 150, 150, 200, 200, 200, 200 };

                public static int[,] SETS = new int[8, 3]
        {{1, 3, -1},
         {6, 8, 9},
         {11, 13, 14},
         {16, 18, 19},
         {21, 23, 24},
         {26, 27, 29},
         {31, 32, 34},
         {37, 39, -1}}; // Color group tile indices (third can be -1 for 2-tile sets)


        //--------------------
        // Runtime board state tracking
        //--------------------

        public bool[] mortgaged = new bool[40] { false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false }; // If a property is mortgaged

        public int[] owners = new int[40] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }; // Who owns what (BANK_INDEX = unowned)

        public int[] property = new int[40] { -1, 0, -1, 1, -1, -1, 2, -1, 2, 3,
            -1, 4, -1, 4, 5, -1, 6, -1, 6, 7,
            -1, 8, -1, 8, 9, -1, 10, 10, -1, 11,
            -1, 12, 12, -1, 13, -1, -1, 14, -1, 15 }; // Maps board tile to property group index

        public int[] houses = new int[40] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // Number of houses on each tile

        public int[] original = new int[40] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }; // Snapshot to store original ownership (unused in current logic)

        //--------------------
        // Runtime control variables
        //--------------------

        public EMode mode = EMode.ROLL; // Default game mode is to roll dice

        public NeuralPlayer[] players; // Array of neural-network-controlled players
        public NetworkAdapter adapter; // Adapter for communicating game state to networks
        public RNG random;             // Custom RNG instance

        public int turn = 0;           // Whose turn it is
        public int count = 0;          // Number of total turns passed
        public int remaining = 0;      // Number of players still in the game

        public int last_roll = 0;      // Result of the most recent dice roll

        // Card decks
        public List<CardEntry> chance; // Chance card stack
        public List<CardEntry> chest;  // Community Chest card stack
        //--------------------
        // Constructor — initializes players, adapters, card stacks
        //--------------------

        public Board(NetworkAdapter _adapter)
        {
            players = new NeuralPlayer[PLAYER_COUNT];
            random = new RNG();

            adapter = _adapter;

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                players[i] = new NeuralPlayer();

                adapter.SetPosition(i, players[i].position); // Sync starting position
                adapter.SetMoney(i, players[i].funds);       // Sync starting money
            }

            remaining = PLAYER_COUNT; // All players still in game

            chance = new List<CardEntry>();
            chest = new List<CardEntry>();

            // Load and shuffle CHANCE cards
            chance.Add(new CardEntry(ECard.ADVANCE, 39));
            chance.Add(new CardEntry(ECard.ADVANCE, 0));
            chance.Add(new CardEntry(ECard.ADVANCE, 24));
            chance.Add(new CardEntry(ECard.ADVANCE, 11));
            chance.Add(new CardEntry(ECard.RAILROAD2, 0));
            chance.Add(new CardEntry(ECard.RAILROAD2, 0));
            chance.Add(new CardEntry(ECard.UTILITY10, 0));
            chance.Add(new CardEntry(ECard.REWARD, 50));
            chance.Add(new CardEntry(ECard.CARD, 0));
            chance.Add(new CardEntry(ECard.BACK3, 0));
            chance.Add(new CardEntry(ECard.JAIL, 0));
            chance.Add(new CardEntry(ECard.REPAIRS, 0));
            chance.Add(new CardEntry(ECard.FINE, 15));
            chance.Add(new CardEntry(ECard.ADVANCE, 5));
            chance.Add(new CardEntry(ECard.CHAIRMAN, 0));
            chance.Add(new CardEntry(ECard.REWARD, 150));
            chance = random.Shuffle(chance);
            


            // Load and shuffle CHEST cards
            chest.Add(new CardEntry(ECard.ADVANCE, 0));
            chest.Add(new CardEntry(ECard.REWARD, 200));
            chest.Add(new CardEntry(ECard.FINE, 50));
            chest.Add(new CardEntry(ECard.REWARD, 50));
            chest.Add(new CardEntry(ECard.CARD, 0));
            chest.Add(new CardEntry(ECard.JAIL, 0));
            chest.Add(new CardEntry(ECard.REWARD, 100));
            chest.Add(new CardEntry(ECard.REWARD, 20));
            chest.Add(new CardEntry(ECard.BIRTHDAY, 0));
            chest.Add(new CardEntry(ECard.REWARD, 100));
            chest.Add(new CardEntry(ECard.FINE, 100));
            chest.Add(new CardEntry(ECard.FINE, 50));
            chest.Add(new CardEntry(ECard.FINE, 25));
            chest.Add(new CardEntry(ECard.STREET, 0));
            chest.Add(new CardEntry(ECard.REWARD, 10));
            chest.Add(new CardEntry(ECard.REWARD, 100));
            chest = random.Shuffle(chest);

        }
               public EOutcome Step()
        {
            // Entry point for each turn; delegates to the correct game mode
            switch (mode)
            {
                case EMode.ROLL: return Roll(); // Currently, only rolling is implemented
            }

            return EOutcome.ONGOING; // Default return if no mode matched
        }

        public EOutcome Roll()
        {
            // Handle pre-turn logic like mortgage/unmortgage, houses, trading
            BeforeTurn();

            // Roll two 6-sided dice
            int d1 = random.gen.Next(1, 7);
            int d2 = random.gen.Next(1, 7);

            last_roll = d1 + d2; // Store the total

            bool isDouble = d1 == d2;         // Track if a double was rolled
            bool doubleInJail = false;        // If it was a double that freed a jailed player

            // ------------------------------
            // Handle jail state if player is currently jailed
            // ------------------------------
            if (players[turn].state == Player.EState.JAIL)
            {
                adapter.SetTurn(turn); // Inform adapter whose turn it is
                Player.EJailDecision decision = players[turn].DecideJail();

                if (decision == Player.EJailDecision.ROLL)
                {
                    // Try to roll out of jail
                    if (isDouble)
                    {
                        // Successful — leave jail
                        players[turn].jail = 0;
                        players[turn].state = Player.EState.NORMAL;

                        adapter.SetJail(turn, 0);

                        doubleInJail = true;
                    }
                    else
                    {
                        // Failed — increment jail counter
                        players[turn].jail++;

                        if (players[turn].jail >= 3)
                        {
                            // Must pay to leave jail after 3 failed attempts
                            Payment(turn, JAIL_PENALTY);

                            players[turn].jail = 0;
                            players[turn].state = Player.EState.NORMAL;

                            adapter.SetJail(turn, 0);
                        }
                    }
                }
                else if (decision == Player.EJailDecision.PAY)
                {
                    // Pay to get out
                    Payment(turn, JAIL_PENALTY);

                    players[turn].jail = 0;
                    players[turn].state = Player.EState.NORMAL;

                    adapter.SetJail(turn, 0);
                }
                else if (decision == Player.EJailDecision.CARD)
                {
                    // Use get-out-of-jail-free card
                    if (players[turn].card > 0)
                    {
                        players[turn].card--;
                        players[turn].jail = 0;
                        players[turn].state = Player.EState.NORMAL;

                        adapter.SetJail(turn, 0);
                        adapter.SetCard(turn, players[turn].card > 0 ? 1 : 0);
                    }
                    else
                    {
                        // Attempt roll again if card was not available
                        if (isDouble)
                        {
                            players[turn].jail = 0;
                            players[turn].state = Player.EState.NORMAL;

                            adapter.SetJail(turn, 0);
                        }
                        else
                        {
                            players[turn].jail++;

                            if (players[turn].jail >= 3)
                            {
                                Payment(turn, JAIL_PENALTY);

                                players[turn].jail = 0;
                                players[turn].state = Player.EState.NORMAL;

                                adapter.SetJail(turn, 0);
                            }
                        }
                    }
                }
            }

            // ------------------------------
            // If player is now in NORMAL state, proceed to move
            // ------------------------------
            if (players[turn].state == Player.EState.NORMAL)
            {
                // If it's not the 3rd double in a row
                bool notFinalDouble = (!isDouble) || (players[turn].doub <= 1);

                if (notFinalDouble)
                {
                    Movement(d1 + d2, isDouble); // Move the player
                }
            }

            // ------------------------------
            // If rolled a double and not jailed, allow extra turn
            // ------------------------------
            if (players[turn].state != Player.EState.RETIRED && isDouble && !doubleInJail)
            {
                players[turn].doub++;

                if (players[turn].doub >= 3)
                {
                    // Too many doubles — go to jail
                    players[turn].position = JAIL_INDEX;
                    players[turn].doub = 0;
                    players[turn].state = Player.EState.JAIL;

                    adapter.SetJail(turn, 1);
                }
            }

            // Final outcome of the turn — advance turn unless double was rolled
            EOutcome outcome = EndTurn((!isDouble || players[turn].state == Player.EState.RETIRED || players[turn].state == Player.EState.JAIL));
            return outcome;
        }

        public EOutcome EndTurn(bool increment = true)
        {
            // Determines whether to advance the turn and check win/draw conditions
            if (increment)
            {
                IncrementTurn(); // move to next player's turn

                int count = 0; // local retry count

                // Skip retired players
                while (players[turn].state == Player.EState.RETIRED && count <= PLAYER_COUNT * 2)
                {
                    IncrementTurn();
                    count++;
                }

                // If only one player remains, declare win
                if (remaining <= 1)
                {
                    switch (turn)
                    {
                        case 0: return EOutcome.WIN1;
                        case 1: return EOutcome.WIN2;
                        case 2: return EOutcome.WIN3;
                        case 3: return EOutcome.WIN4;
                    }
                }
            }

            count++; // increment turn counter

            // Check for stalemate
            if (count >= STALEMATE_TURN)
            {
                return EOutcome.DRAW;
            }

            return EOutcome.ONGOING; // otherwise game continues
        }

        public void IncrementTurn()
        {
            // Cycles turn index back to 0 if at the end
            turn++;

            if (turn >= PLAYER_COUNT)
            {
                turn = 0;
            }
        }

              public void BeforeTurn()
        {
            // Skip logic for retired players
            if (players[turn].state == Player.EState.RETIRED)
            {
                return;
            }

            // ------------------------------
            // Mortgage and Unmortgage logic
            // ------------------------------

            int itemCount = players[turn].items.Count; // Get how many properties the current player owns

            for (int j = 0; j < itemCount; j++)
            {
                int index = players[turn].items[j]; // Get the board index of each owned property

                if (mortgaged[index])
                {
                    // Property is mortgaged — consider unmortgaging it
                    int advancePrice = (int)(COSTS[index] * MORTGAGE_INTEREST); // Cost to unmortgage

                    if (advancePrice > players[turn].funds)
                    {
                        continue; // Skip if player cannot afford to unmortgage
                    }

                    adapter.SetTurn(turn); // Update adapter with current player
                    adapter.SetSelectionState(index, 1); // Highlight property

                    Player.EDecision decision = players[turn].DecideAdvance(index); // Ask network whether to unmortgage

                    adapter.SetSelectionState(index, 0); // Unhighlight property

                    if (decision == Player.EDecision.YES)
                    {
                        Advance(index); // Unmortgage the property
                    }
                }
                else
                {
                    // Property is NOT mortgaged — consider mortgaging it
                    adapter.SetTurn(turn);

                    adapter.SetSelectionState(index, 1);

                    Player.EDecision decision = players[turn].DecideMortgage(index);

                    adapter.SetSelectionState(index, 0);

                    if (decision == Player.EDecision.YES)
                    {
                        // Mortgage(index); <-- This is commented out; no mortgage logic active yet
                    }
                }
            }

            // ------------------------------
            // Consider selling houses
            // ------------------------------

            int[] sets = FindSets(turn); // Get all full color sets owned by the player
            int setCount = sets.GetLength(0); // Count how many sets there are

            for (int j = 0; j < setCount; j++)
            {
                // Count total houses in the set
                int houseTotal = houses[SETS[sets[j], 0]] + houses[SETS[sets[j], 1]];

                if (sets[j] != 0 && sets[j] != 7) // If it's a 3-property set (not brown or dark blue)
                {
                    houseTotal += houses[SETS[sets[j], 2]];
                }

                int sellMax = houseTotal; // Max houses player could sell

                adapter.SetTurn(turn);
                adapter.SetSelectionState(SETS[sets[j], 0], 1); // Highlight a property in the set

                int decision = players[turn].DecideSellHouse(sets[j]); // Ask network how many houses to sell

                adapter.SetSelectionState(SETS[sets[j], 0], 0);

                decision = Math.Min(decision, sellMax); // Limit sale to available houses

                if (decision > 0)
                {
                    SellHouses(sets[j], decision); // Remove houses
                    players[turn].funds += (int)(decision * BUILD[property[SETS[sets[j], 0]]] * 0.5f); // Add half-cost value to funds
                }
            }

            // ------------------------------
            // Consider building houses
            // ------------------------------

            sets = FindSets(turn); // Recalculate sets in case something changed
            setCount = sets.GetLength(0);

            for (int j = 0; j < setCount; j++)
            {
                int maxHouse = 10; // Default for 2-property sets
                int houseTotal = houses[SETS[sets[j], 0]] + houses[SETS[sets[j], 1]];

                if (sets[j] != 0 && sets[j] != 7) // 3-property sets have 15 house max
                {
                    maxHouse = 15;
                    houseTotal += houses[SETS[sets[j], 2]];
                }

                int buildMax = maxHouse - houseTotal; // Max houses player could build
                int costPerHouse = BUILD[property[SETS[sets[j], 0]]]; // House cost from BUILD array
                int affordMax = (int)Math.Floor(players[turn].funds / (float)costPerHouse); // How many houses player can afford

                if (affordMax < 0)
                {
                    affordMax = 0;
                }

                buildMax = Math.Min(buildMax, affordMax); // Limit to funds and rules

                adapter.SetTurn(turn);
                adapter.SetSelectionState(SETS[sets[j], 0], 1); // Highlight property in UI

                int decision = players[turn].DecideBuildHouse(sets[j]); // Ask AI how many houses to build

                adapter.SetSelectionState(SETS[sets[j], 0], 0);

                decision = Math.Min(decision, buildMax); // Clamp to valid amount

                if (decision > 0)
                {
                    BuildHouses(sets[j], decision); // Build the houses
                    Payment(turn, decision * costPerHouse); // Pay total cost
                }
            }

            // ------------------------------
            // Trading logic (not shown in detail here)
            // ------------------------------
            Trading();
        }

            // ------------------------------
            // Trading logic (not shown in detail here)
            // ------------------------------
            Trading();
        }
        public void Trading()
        {
            // Prepare lists of valid trade partners and their indices
            List<Player> candidates = new List<Player>();
            List<int> candidates_index = new List<int>();

            // Loop through all players to find valid candidates
            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                if (i == turn) // Skip self
                {
                    continue;
                }

                if (players[i].state == Player.EState.RETIRED) // Skip retired players
                {
                    continue;
                }

                candidates.Add(players[i]); // Add active player to trade candidates
                candidates_index.Add(i);    // Track their index for ownership purposes
            }

            if (candidates.Count == 0) // No one to trade with
            {
                return;
            }

            // Define trade parameters
            int TRADE_ATTEMPTS = 4;      // How many trade offers to generate per turn
            int TRADE_ITEM_MAX = 5;      // Max number of properties to give or receive
            int TRADE_MONEY_MAX = 500;   // Max money to offer/request in trade

            // Attempt trades
            for (int t = 0; t < TRADE_ATTEMPTS; t++)
            {
                // Randomly decide how many properties to offer from current player
                int give = random.gen.Next(0, Math.Min(players[turn].items.Count, TRADE_ITEM_MAX));

                // Select a random candidate to trade with
                int selectedPlayer = random.gen.Next(0, candidates.Count);

                Player other = candidates[selectedPlayer];
                int other_index = candidates_index[selectedPlayer];

                // Randomly decide how many properties to request from selected player
                int recieve = random.gen.Next(0, Math.Min(other.items.Count, TRADE_ITEM_MAX));

                // Skip trade if either player is in debt
                if (players[turn].funds < 0 || other.funds < 0)
                {
                    continue;
                }

                // Determine how much money is offered or requested by each side
                int moneyGive = random.gen.Next(0, Math.Min(players[turn].funds, TRADE_MONEY_MAX));
                int moneyRecieve = random.gen.Next(0, Math.Min(other.funds, TRADE_MONEY_MAX));
                int moneyBalance = moneyGive - moneyRecieve; // Net effect of money exchange

                // Skip if no properties are being exchanged
                if (give == 0 || recieve == 0)
                {
                    continue;
                }

                // Randomly select which properties to give
                List<int> gift = new List<int>();
                List<int> possible = new List<int>(players[turn].items);

                for (int i = 0; i < give; i++)
                {
                    int selection = random.gen.Next(0, possible.Count);

                    gift.Add(possible[selection]);
                    possible.RemoveAt(selection); // Avoid duplicates
                }

                // Randomly select which properties to receive
                List<int> returning = new List<int>();

                possible = new List<int>(other.items);

                for (int i = 0; i < recieve; i++)
                {
                    int selection = random.gen.Next(0, possible.Count);

                    returning.Add(possible[selection]);
                    possible.RemoveAt(selection);
                }

                // Highlight offered properties for neural network evaluation
                for (int i = 0; i < gift.Count; i++)
                {
                    adapter.SetSelectionState(gift[i], 1);
                }

                // Highlight requested properties for neural network evaluation
                for (int i = 0; i < returning.Count; i++)
                {
                    adapter.SetSelectionState(returning[i], 1);
                }

                // Set the money balance context for trade evaluation
                adapter.SetMoneyContext(moneyBalance);

                // Ask the current player if they want to make the trade
                Player.EDecision decision = players[turn].DecideOfferTrade();

                if (decision == Player.EDecision.NO)
                {
                    // Trade offer declined, clear highlights and skip
                    adapter.ClearSelectionState();
                    continue;
                }

                // Ask the other player if they accept the trade
                Player.EDecision decision2 = other.DecideAcceptTrade();

                if (decision2 == Player.EDecision.NO)
                {
                    continue; // Trade rejected
                }

                // Execute the property transfer from current player to other player
                for (int i = 0; i < gift.Count; i++)
                {
                    Monopoly.Analytics.instance.MadeTrade(gift[i]); // Log trade

                    players[turn].items.Remove(gift[i]); // Remove from current player
                    other.items.Add(gift[i]);            // Add to other player

                    owners[gift[i]] = other_index;       // Update owner reference
                    adapter.SetOwner(gift[i], other_index); // Update adapter
                }

                // Execute the property transfer from other player to current player
                for (int i = 0; i < returning.Count; i++)
                {
                    Monopoly.Analytics.instance.MadeTrade(returning[i]);

                    other.items.Remove(returning[i]);       // Remove from other player
                    players[turn].items.Add(returning[i]);  // Add to current player

                    owners[returning[i]] = turn;
                    adapter.SetOwner(returning[i], turn);
                }

                // Clear all selection highlights
                adapter.ClearSelectionState();

                // Execute the money transfer
                players[turn].funds -= moneyBalance; // Player gives moneyBalance
                other.funds += moneyBalance;         // Other receives it
            }
        }

         public void Auction(int index)
        {
            // Track which players are eligible to bid
            bool[] participation = new bool[PLAYER_COUNT];

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                participation[i] = players[i].state != Player.EState.RETIRED;
            }

            // Store each player's bid
            int[] bids = new int[PLAYER_COUNT];

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                adapter.SetTurn(i);                          // Inform neural net whose turn to decide
                adapter.SetSelectionState(index, 1);        // Highlight auction property
                bids[i] = players[i].DecideAuctionBid(index); // Ask neural net for bid
                adapter.SetSelectionState(index, 0);        // Clear selection state

                if (bids[i] > players[i].funds)
                {
                    participation[i] = false; // Disqualify overbidders
                }
            }

            // Determine highest bid
            int max = 0;

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                if (participation[i] && bids[i] > max)
                {
                    max = bids[i];
                }
            }

            // Gather all players who made the highest bid (to break ties randomly)
            List<int> candidates = new List<int>();
            List<int> backup = new List<int>(); // Anyone still active (used if no one bids)

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                if (participation[i] && bids[i] == max)
                {
                    candidates.Add(i);
                }
                if (players[i].state != Player.EState.RETIRED)
                {
                    backup.Add(i);
                }
            }

            if (candidates.Count > 0)
            {
                // Choose winner randomly among tied highest bidders
                int winner = candidates[random.gen.Next(0, candidates.Count)];
                Payment(winner, max); // Deduct payment

                owners[index] = winner;
                players[winner].items.Add(index);

                if (original[index] == -1)
                {
                    original[index] = winner; // Track original owner
                }

                adapter.SetOwner(index, winner); // Update game state
            }
            else
            {
                // No valid bids — assign randomly to active player (free of charge)
                int winner = backup[random.gen.Next(0, backup.Count)];

                owners[index] = winner;
                players[winner].items.Add(index);

                if (original[index] == -1)
                {
                    original[index] = winner;
                }

                adapter.SetOwner(index, winner);
            }
        }

        public void Movement(int roll, bool isDouble)
        {
            // Add the dice roll to the current player's position
            players[turn].position += roll;

            // Check if the player passed or landed on GO (board wrap-around)
            if (players[turn].position >= BOARD_LENGTH)
            {
                // Wrap position around the board
                players[turn].position -= BOARD_LENGTH;

                // Reward player depending on whether they landed exactly on GO
                if (players[turn].position == 0)
                {
                    players[turn].funds += GO_BONUS; // Exact landing on GO
                }
                else
                {
                    players[turn].funds += GO_LANDING_BONUS; // Passing GO
                }
            }

            // Update the neural network input with the new funds and position
            adapter.SetMoney(turn, players[turn].funds);
            adapter.SetPosition(turn, players[turn].position);

            // Activate the tile the player landed on
            ActivateTile();
        }

         public void ActivateTile()
    {
        // Get the index of the tile the current player landed on
        int index = players[turn].position;

        // Get the type of tile based on its index
        ETile tile = TYPES[index];

        // ----------- PROPERTY TILE LOGIC -----------
        if (tile == ETile.PROPERTY)
        {
            // Get the current owner of the property
            int owner = Owner(index);

            if (owner == BANK_INDEX) // If the property is unowned
            {
                adapter.SetTurn(turn); // Set the neural network's input to the current turn
                adapter.SetSelection(index); // Highlight the selected property tile

                // Ask the neural network if it wants to buy or auction this property
                Player.EBuyDecision decision = players[turn].DecideBuy(index);

                if (decision == Player.EBuyDecision.BUY) // If player wants to buy
                {
                    // Check if the player has enough money to buy it
                    if (players[turn].funds < COSTS[index])
                    {
                        Auction(index); // Can't afford it, go to auction instead
                    }
                    else
                    {
                        Payment(turn, COSTS[index]); // Pay the cost
                        owners[index] = turn; // Set player as owner of the property

                        if (original[index] == -1) // If first time being owned
                        {
                            original[index] = turn; // Set original owner
                        }

                        players[turn].items.Add(index); // Add property to player's items
                        adapter.SetOwner(index, turn); // Update the neural network with new ownership
                    }
                }
                else if (decision == Player.EBuyDecision.AUCTION) // If player chooses auction
                {
                    Auction(index); // Send property to auction
                }
            }
            else if (owner == turn) // If player landed on their own property
            {
                // No action needed
            }
            else if (!mortgaged[index]) // Property is owned by someone else and not mortgaged
            {
                // Get the rent value and pay the property owner
                int rent = PROPERTY_PENALTIES[property[index], houses[index]];
                PaymentToPlayer(turn, owner, rent); // Transfer rent
            }
        }

        // ----------- TRAIN TILE LOGIC -----------
        else if (tile == ETile.TRAIN)
        {
            // Get owner of train tile
            int owner = Owner(index);

            if (owner == BANK_INDEX) // If unowned
            {
                adapter.SetTurn(turn); // Set current player turn
                adapter.SetSelection(index); // Highlight train tile

                // Ask if player wants to buy or auction the tile
                Player.EBuyDecision decision = players[turn].DecideBuy(index);

                if (decision == Player.EBuyDecision.BUY) // Chose to buy
                {
                    if (players[turn].funds < COSTS[index]) // Can't afford it
                    {
                        Auction(index); // Auction instead
                    }
                    else
                    {
                        Payment(turn, COSTS[index]); // Pay the cost
                        owners[index] = turn; // Assign ownership

                        if (original[index] == -1) // If never owned
                        {
                            original[index] = turn; // Mark original owner
                        }

                        players[turn].items.Add(index); // Add to player's owned items
                        adapter.SetOwner(index, turn); // Update ownership in NN adapter
                    }
                }
                else if (owner == turn) // Landed on own train tile
                {
                    // No action needed
                }
                else if (decision == Player.EBuyDecision.AUCTION) // If chose to auction
                {
                    Auction(index); // Auction the tile
                }
            }
            else if (!mortgaged[index]) // If owned and not mortgaged
            {
                int trains = CountTrains(owner); // Count number of trains owned by owner

                if (trains >= 1 && trains <= 4) // Valid number of train tiles
                {
                    int fine = TRAIN_PENALTIES[trains - 1]; // Lookup penalty value
                    PaymentToPlayer(turn, owner, fine); // Pay the rent to owner
                }
            }
        }

        // ----------- UTILITY TILE LOGIC -----------
        else if (tile == ETile.UTILITY)
        {
            int owner = Owner(index); // Determine the owner

            if (owner == BANK_INDEX) // Utility is unowned
            {
                adapter.SetTurn(turn); // Set current turn for adapter
                adapter.SetSelectionState(index, 1); // Highlight utility tile

                Player.EBuyDecision decision = players[turn].DecideBuy(index); // Ask neural net for decision

                adapter.SetSelectionState(index, 0); // Unhighlight tile

                if (decision == Player.EBuyDecision.BUY) // Player chooses to buy
                {
                    if (players[turn].funds < COSTS[index]) // Not enough money
                    {
                        Auction(index); // Auction the utility
                    }
                    else
                    {
                        Payment(turn, COSTS[index]); // Deduct money
                        owners[index] = turn; // Set player as owner

                        if (original[index] == -1) // First time owned
                        {
                            original[index] = turn;
                        }

                        players[turn].items.Add(index); // Add to inventory
                        adapter.SetOwner(index, turn); // Update neural network state
                    }
                }
                else if (decision == Player.EBuyDecision.AUCTION) // Player chooses auction
                {
                    Auction(index);
                }
            }
            else if (owner == turn) // Player owns the utility
            {
                // No action needed
            }
            else if (!mortgaged[index]) // Utility is active and not mortgaged
            {
                int utilities = CountUtilities(owner); // Count number of utilities owned

                if (utilities >= 1 && utilities <= 2)
                {
                    int fine = UTILITY_PENALTIES[utilities - 1] * last_roll; // Calculate rent
                    PaymentToPlayer(turn, owner, fine); // Pay the rent
                }
            }
        }

        // ----------- TAX TILE -----------
        else if (tile == ETile.TAX)
        {
            Payment(turn, COSTS[index]); // Deduct tax from player
        }

        // ----------- CHANCE TILE -----------
        else if (tile == ETile.CHANCE)
        {
            DrawChance(); // Draw a chance card and execute effect
        }

        // ----------- CHEST TILE -----------
        else if (tile == ETile.CHEST)
        {
            DrawChest(); // Draw a community chest card and execute effect
        }

        // ----------- JAIL TILE (GO TO JAIL) -----------
        else if (tile == ETile.JAIL)
        {
            players[turn].position = JAIL_INDEX; // Move player directly to jail tile
            players[turn].doub = 0; // Reset any double roll count
            players[turn].state = Player.EState.JAIL; // Change state to jail

            adapter.SetJail(turn, 1); // Update adapter to reflect jail status
        }
    }

        public void Payment(int owner, int fine)
        {
            // Deduct the fine amount from the owner's funds
            players[owner].funds -= fine;

            // Update the owner's money in the neural network adapter
            adapter.SetMoney(owner, players[owner].funds);

            // Save the original funds value before any recovery attempts
            int original = players[owner].funds;

            // If the player is in debt, attempt to sell houses to cover the fine
            if (players[owner].funds < 0)
            {
                // Get all full sets the player owns
                int[] sets = FindSets(turn);
                int setCount = sets.GetLength(0);

                // Iterate through each owned set
                for (int j = 0; j < setCount; j++)
                {
                    // Count the number of houses in the set
                    int houseTotal = houses[SETS[sets[j], 0]] + houses[SETS[sets[j], 1]];

                    if (sets[j] != 0 && sets[j] != 7) // Sets with 3 properties
                    {
                        houseTotal += houses[SETS[sets[j], 2]];
                    }

                    int sellMax = houseTotal; // Max number of houses that can be sold

                    adapter.SetTurn(turn); // Set turn for neural network
                    adapter.SetSelectionState(SETS[sets[j], 0], 1); // Highlight the property set

                    // Ask the neural network how many houses to sell
                    int decision = players[turn].DecideSellHouse(sets[j]);

                    adapter.SetSelectionState(SETS[sets[j], 0], 0); // Clear highlight

                    decision = Math.Min(decision, sellMax); // Cap the sale amount at max

                    if (decision > 0)
                    {
                        SellHouses(sets[j], decision); // Sell the houses

                        // Add money from sale (half the build cost per house)
                        players[owner].funds += (int)(decision * BUILD[property[SETS[sets[j], 0]]] * 0.5f);
                        adapter.SetMoney(owner, players[owner].funds); // Update neural net
                    }
                }
            }

            // If still in debt, attempt to mortgage properties
            if (players[owner].funds < 0)
            {
                int itemCount = players[owner].items.Count;

                for (int i = 0; i < itemCount; i++)
                {
                    int item = players[owner].items[i];
                    adapter.SetTurn(owner); // Set adapter turn

                    adapter.SetSelectionState(players[owner].items[i], 1); // Highlight property

                    // Ask the neural network whether to mortgage the property
                    Player.EDecision decision = players[owner].DecideMortgage(players[owner].items[i]);

                    adapter.SetSelectionState(players[owner].items[i], 0); // Clear highlight

                    if (decision == Player.EDecision.YES)
                    {
                        Mortgage(item); // Mortgage the property
                    }
                }
            }

            // If still in debt after all recovery options, declare bankruptcy
            if (players[owner].funds < 0)
            {
                // Calculate how much money was recovered during the process
                int regained = players[owner].funds - original;

                int itemCount = players[owner].items.Count;
                int housemoney = 0; // Total value recovered from house liquidation

                // For each item owned by the player
                for (int i = 0; i < itemCount; i++)
                {
                    int item = players[owner].items[i];

                    // Return ownership of the property to the bank
                    owners[item] = BANK_INDEX;
                    adapter.SetOwner(item, BANK_INDEX); // Update NN

                    // If houses exist on the property, liquidate them
                    if (houses[item] > 0)
                    {
                        int liquidated = houses[item];
                        int sell = (liquidated * BUILD[property[item]]) / 2;
                        housemoney += sell;

                        houses[item] = 0; // Clear house count
                    }
                }

                // Remove all items from player's inventory
                players[owner].items.Clear();

                // Mark player as retired
                players[owner].state = Player.EState.RETIRED;

                // Decrease the number of active players
                remaining--;
            }
        }

               public void PaymentToPlayer(int owner, int recipient, int fine)
        {
            // Deduct the fine from the paying player's funds
            players[owner].funds -= fine;
            adapter.SetMoney(owner, players[owner].funds); // Update adapter with new funds

            // Add the fine to the receiving player's funds
            players[recipient].funds += fine;
            adapter.SetMoney(recipient, players[recipient].funds); // Update adapter with new funds

            // Save the original amount for later comparison
            int original = players[owner].funds;

            // ---------- Attempt to sell houses if player is in debt ----------
            if (players[owner].funds < 0)
            {
                int[] sets = FindSets(turn); // Find complete sets the player owns
                int setCount = sets.GetLength(0);

                for (int j = 0; j < setCount; j++)
                {
                    // Count total houses in this set
                    int houseTotal = houses[SETS[sets[j], 0]] + houses[SETS[sets[j], 1]];

                    if (sets[j] != 0 && sets[j] != 7) // If 3-property set
                    {
                        houseTotal += houses[SETS[sets[j], 2]];
                    }

                    int sellMax = houseTotal;

                    adapter.SetTurn(turn); // Set turn in adapter
                    adapter.SetSelectionState(SETS[sets[j], 0], 1); // Highlight selection

                    // Ask how many houses to sell
                    int decision = players[turn].DecideSellHouse(sets[j]);

                    adapter.SetSelectionState(SETS[sets[j], 0], 0); // Clear selection
                    decision = Math.Min(decision, sellMax); // Ensure within bounds

                    if (decision > 0)
                    {
                        SellHouses(sets[j], decision); // Perform house sale

                        // Add sale proceeds to funds
                        players[owner].funds += (int)(decision * BUILD[property[SETS[sets[j], 0]]] * 0.5f);
                        adapter.SetMoney(owner, players[owner].funds); // Update adapter
                    }
                }
            }

            // ---------- Attempt to mortgage properties if still in debt ----------
            if (players[owner].funds < 0)
            {
                int itemCount = players[owner].items.Count;

                for (int i = 0; i < itemCount; i++)
                {
                    int item = players[owner].items[i];
                    adapter.SetTurn(owner); // Set adapter turn

                    adapter.SetSelectionState(players[owner].items[i], 0); // Highlight item

                    // Ask if the item should be mortgaged
                    Player.EDecision decision = players[owner].DecideMortgage(players[owner].items[i]);

                    adapter.SetSelectionState(players[owner].items[i], 1); // Clear highlight

                    if (decision == Player.EDecision.YES)
                    {
                        Mortgage(item); // Mortgage the property
                    }
                }
            }

            // ---------- Bankruptcy handling if still in debt ----------
            if (players[owner].funds < 0)
            {
                // Transfer remaining negative funds to recipient
                players[recipient].funds += players[owner].funds;
                adapter.SetMoney(recipient, players[recipient].funds);

                int itemCount = players[owner].items.Count;
                int housemoney = 0; // Track liquidated house value

                for (int i = 0; i < itemCount; i++)
                {
                    // Transfer property to recipient
                    players[recipient].items.Add(players[owner].items[i]);
                    adapter.SetOwner(players[owner].items[i], recipient);

                    int item = players[owner].items[i];
                    owners[item] = recipient;

                    // Liquidate houses on the property, if any
                    if (houses[item] > 0)
                    {
                        int liquidated = houses[item];
                        int sell = (liquidated * BUILD[property[item]]) / 2;
                        housemoney += sell;

                        houses[item] = 0; // Clear house count
                    }
                }

                // Add house liquidation money to recipient
                players[recipient].funds += housemoney;
                adapter.SetMoney(recipient, players[recipient].funds);

                // Clear the original owner's properties
                players[owner].items.Clear();

                // Mark the player as retired
                players[owner].state = Player.EState.RETIRED;
                remaining--; // Decrease player count
            }
        }


        public int Owner(int index)
        {
            // Return the current owner of the tile at the given index
            return owners[index];
        }

        public void Mortgage(int index)
        {
            // Mark the property as mortgaged
            mortgaged[index] = true;
            adapter.SetMortgage(index, 1); // Update adapter to reflect mortgage state

            // Give the player half the cost of the property as mortgage value
            players[owners[index]].funds += COSTS[index] / 2;
            adapter.SetMoney(owners[index], players[owners[index]].funds); // Update funds in adapter
        }

        public void Advance(int index)
        {
            // Unmortgage a property
            mortgaged[index] = false;
            adapter.SetMortgage(index, 0); // Update adapter to reflect unmortgaged state

            // Calculate repayment cost with interest and deduct it from player
            int cost = (int)(COSTS[index] * MORTGAGE_INTEREST);
            Payment(owners[index], cost);
        }

        public int CountTrains(int player)
        {
            int itemCount = players[player].items.Count;
            int count = 0;

            // Count how many train stations the player owns
            for (int i = 0; i < itemCount; i++)
            {
                if (TRAIN_POSITIONS.Contains(players[player].items[i]))
                {
                    count++;
                }
            }

            return count;
        }

        public int CountUtilities(int player)
        {
            int itemCount = players[player].items.Count;
            int count = 0;

            // Count how many utility properties the player owns
            for (int i = 0; i < itemCount; i++)
            {
                if (UTILITY_POSIIONS.Contains(players[player].items[i]))
                {
                    count++;
                }
            }

            return count;
        }

        public void DrawChance()
        {
            // Get the first card from the chance deck
            CardEntry card = chance[0];

            // Remove it from the top and place it at the bottom (rotating deck)
            chance.RemoveAt(0);
            chance.Add(card);

            // ----------- Card: Advance to specific tile -----------
            if (card.card == ECard.ADVANCE)
            {
                // If the advance passes GO, collect bonus
                if (players[turn].position > card.val)
                {
                    players[turn].funds += GO_BONUS;
                    adapter.SetMoney(turn, players[turn].funds);
                }

                // Move player to destination tile
                players[turn].position = card.val;
                adapter.SetPosition(turn, players[turn].position);

                // Activate the new tile
                ActivateTile();
            }
            // ----------- Card: Gain money -----------
            else if (card.card == ECard.REWARD)
            {
                players[turn].funds += card.val;
                adapter.SetMoney(turn, players[turn].funds);
            }
            // ----------- Card: Pay fine -----------
            else if (card.card == ECard.FINE)
            {
                Payment(turn, card.val);
            }
            // ----------- Card: Move back 3 spaces -----------
            else if (card.card == ECard.BACK3)
            {
                players[turn].position -= 3;
                adapter.SetPosition(turn, players[turn].position);

                ActivateTile();
            }
            // ----------- Card: Receive Get Out of Jail Free card -----------
            else if (card.card == ECard.CARD)
            {
                players[turn].card++;
                adapter.SetCard(turn, players[turn].card);
            }
            // ----------- Card: Go to Jail -----------
            else if (card.card == ECard.JAIL)
            {
                players[turn].position = JAIL_INDEX;
                players[turn].doub = 0;
                players[turn].state = Player.EState.JAIL;

                adapter.SetPosition(turn, players[turn].position);
                adapter.SetJail(turn, 1);
            }
            // ----------- Card: Advance to nearest train and pay double rent -----------
            else if (card.card == ECard.RAILROAD2)
            {
                AdvanceToTrain2();
            }
            // ----------- Card: Advance to nearest utility and pay 10x dice roll -----------
            else if (card.card == ECard.UTILITY10)
            {
                AdvanceToUtility10();
            }
            // ----------- Card: Pay all other players -----------
            else if (card.card == ECard.CHAIRMAN)
            {
                for (int i = 0; i < PLAYER_COUNT; i++)
                {
                    if (i == turn)
                    {
                        continue;
                    }

                    // Only pay active (non-retired) players
                    if (players[i].state != Player.EState.RETIRED)
                    {
                        PaymentToPlayer(turn, i, 50);
                    }
                }
            }
            // ----------- Card: Pay for each house and hotel -----------
            else if (card.card == ECard.REPAIRS)
            {
                int houseCount = 0;
                int hotelCount = 0;
                int itemCount = players[turn].items.Count;

                // Count how many houses and hotels the player owns
                for (int i = 0; i < itemCount; i++)
                {
                    int index = players[turn].items[i];

                    if (houses[index] <= 4)
                    {
                        houseCount += houses[index];
                    }
                    else
                    {
                        hotelCount++;
                    }
                }

                // Pay repair fees based on number of houses and hotels
                Payment(turn, houseCount * 25 + hotelCount * 100);
            }
        }

              public void DrawChest()
        {
            // Get the top card from the Community Chest deck
            CardEntry card = chest[0];
            chest.RemoveAt(0);              // Remove the drawn card from the top
            chest.Add(card);                // Add the card to the bottom (deck rotation)

            // ----------- Card: Advance to a specific tile -----------
            if (card.card == ECard.ADVANCE)
            {
                // If the advance passes GO, grant bonus
                if (players[turn].position > card.val)
                {
                    players[turn].funds += GO_BONUS;
                    adapter.SetMoney(turn, players[turn].funds);
                }

                // Move player to destination tile
                players[turn].position = card.val;
                adapter.SetPosition(turn, players[turn].position);

                // Trigger the new tile's effect
                ActivateTile();
            }
            // ----------- Card: Gain money -----------
            else if (card.card == ECard.REWARD)
            {
                players[turn].funds += card.val;
                adapter.SetMoney(turn, players[turn].funds);
            }
            // ----------- Card: Pay fine -----------
            else if (card.card == ECard.FINE)
            {
                Payment(turn, card.val);
            }
            // ----------- Card: Get Out of Jail Free -----------
            else if (card.card == ECard.CARD)
            {
                players[turn].card++;
                adapter.SetCard(turn, players[turn].card);
            }
            // ----------- Card: Go to Jail -----------
            else if (card.card == ECard.JAIL)
            {
                players[turn].position = JAIL_INDEX;
                players[turn].doub = 0;
                players[turn].state = Player.EState.JAIL;

                adapter.SetPosition(turn, players[turn].position);
                adapter.SetJail(turn, 1);
            }
            // ----------- Card: Birthday (other players give money to this player) -----------
            else if (card.card == ECard.BIRTHDAY)
            {
                for (int i = 0; i < PLAYER_COUNT; i++)
                {
                    if (i == turn)
                    {
                        continue; // Skip the current player
                    }

                    // Only active (non-retired) players pay
                    if (players[i].state != Player.EState.RETIRED)
                    {
                        PaymentToPlayer(i, turn, 10);
                    }
                }
            }
            // ----------- Card: Street repairs (pay per house/hotel) -----------
            else if (card.card == ECard.STREET)
            {
                int houseCount = 0;
                int hotelCount = 0;
                int itemCount = players[turn].items.Count;

                // Count houses and hotels owned
                for (int i = 0; i < itemCount; i++)
                {
                    int index = players[turn].items[i];

                    if (houses[index] <= 4)
                    {
                        houseCount += houses[index];
                    }
                    else
                    {
                        hotelCount++;
                    }
                }

                // Pay repair cost based on number of houses/hotels
                Payment(turn, houseCount * 40 + hotelCount * 115);
            }
        }

             // Handles logic for Chance card: Advance to nearest train, pay double if owned
        public void AdvanceToTrain2()
        {
            // Get current player position
            int index = players[turn].position;

            // Advance to the nearest train tile in board order
            if (index < TRAIN_POSITIONS[0])
            {
                players[turn].position = TRAIN_POSITIONS[0];
            }
            else if (index < TRAIN_POSITIONS[1])
            {
                players[turn].position = TRAIN_POSITIONS[1];
            }
            else if (index < TRAIN_POSITIONS[2])
            {
                players[turn].position = TRAIN_POSITIONS[2];
            }
            else if (index < TRAIN_POSITIONS[3])
            {
                players[turn].position = TRAIN_POSITIONS[3];
            }
            else
            {
                // If past the last train, go to first and receive GO bonus
                players[turn].position = TRAIN_POSITIONS[0];
                players[turn].funds += GO_BONUS;
                adapter.SetMoney(turn, players[turn].funds);
            }

            // Update player position in the adapter
            adapter.SetPosition(turn, players[turn].position);

            // Refresh index now that player has moved
            index = players[turn].position;

            // Determine current owner of the tile
            int owner = Owner(index);

            // If tile is unowned
            if (owner == BANK_INDEX)
            {
                adapter.SetTurn(turn); // Set adapter for current turn
                adapter.SetSelectionState(index, 0); // Select tile visually

                Player.EBuyDecision decision = players[turn].DecideBuy(index); // AI decides whether to buy

                adapter.SetSelectionState(index, 1); // Clear visual selection

                // If player decides to buy
                if (decision == Player.EBuyDecision.BUY)
                {
                    if (players[turn].funds < COSTS[index]) // Can't afford
                    {
                        Auction(index); // Trigger auction
                    }
                    else
                    {
                        Payment(turn, COSTS[index]); // Pay bank
                        owners[index] = turn; // Assign ownership

                        if (original[index] == -1)
                        {
                            original[index] = turn; // Mark original owner
                        }

                        players[turn].items.Add(index); // Add to owned properties
                        adapter.SetOwner(index, turn); // Update adapter
                    }


                }
                else if (decision == Player.EBuyDecision.AUCTION)
                {
                    Auction(index); // Trigger auction
                }
            }
            else if (owner == turn)
            {
                // Do nothing if player owns the property
            }
            else if (!mortgaged[index])
            {
                // If owned by another and not mortgaged, pay double rent
                int trains = CountTrains(owner); // Count how many trains owner has

                if (trains >= 1 && trains <= 4)
                {
                    int fine = TRAIN_PENALTIES[trains - 1]; // Look up rent value
                    PaymentToPlayer(turn, owner, fine * 2); // Pay double
                }
            }
        }

        // Handles logic for Chance card: Advance to nearest utility and pay rent (10x roll)
        public void AdvanceToUtility10()
        {
            // Get current player position
            int index = players[turn].position;

            // Move to nearest utility
            if (index < UTILITY_POSIIONS[0])
            {
                players[turn].position = UTILITY_POSIIONS[0];
            }
            else if (index < UTILITY_POSIIONS[1])
            {
                players[turn].position = UTILITY_POSIIONS[1];
            }
            else
            {
                players[turn].position = UTILITY_POSIIONS[0]; // Wrap around board
                players[turn].funds += GO_BONUS; // Collect GO bonus

                adapter.SetMoney(turn, players[turn].funds); // Update funds in adapter
            }

            // Update player position
            adapter.SetPosition(turn, players[turn].position);

            // Refresh index now that position has changed
            index = players[turn].position;

            // Get the current owner of the utility tile
            int owner = Owner(index);

            // If unowned, handle potential purchase or auction
            if (owner == BANK_INDEX)
            {
                adapter.SetTurn(turn); // Set turn context in adapter
                Player.EBuyDecision decision = players[turn].DecideBuy(index); // Ask AI to decide

                // If player decides to buy
                if (decision == Player.EBuyDecision.BUY)
                {
                    if (players[turn].funds < COSTS[index])
                    {
                        Auction(index); // Trigger auction if can't afford
                    }
                    else
                    {
                        Payment(turn, COSTS[index]); // Pay bank

                        owners[index] = turn; // Assign ownership

                        if (original[index] == -1)
                        {
                            original[index] = turn; // Store original owner
                        }

                        players[turn].items.Add(index); // Add to inventory
                        adapter.SetOwner(index, turn); // Update adapter
                    }   
                }
                else if (decision == Player.EBuyDecision.AUCTION)
                {
                    Auction(index); // Go to auction
                }
            }
            else if (owner == turn)
            {
                // Do nothing if player owns it
            }
            else if (!mortgaged[index])
            {
                // If owned by someone else, pay rent based on dice roll
                int fine = 10 * last_roll;

                PaymentToPlayer(turn, owner, fine);
            }
        }

       public int[] FindSets(int owner)
{
    // Create a list to store indices of completed property sets
    List<int> sets = new List<int>();

    // Get the list of properties the current player owns
    List<int> items = players[owner].items;

    // Iterate over all defined sets (8 total)
    for (int i = 0; i < 8; i++)
    {
        // Check two-property sets (Brown and Dark Blue)
        if (i == 0 || i == 7)
        {
            // If player owns both properties in the set, add set index
            if (items.Contains(SETS[i,0]) && items.Contains(SETS[i,1]))
            {
                sets.Add(i);
            }

            continue; // Skip checking the third entry (always -1 for two-property sets)
        }

        // Check three-property sets (e.g., Light Blue, Pink, etc.)
        if (items.Contains(SETS[i, 0]) && items.Contains(SETS[i, 1]) && items.Contains(SETS[i, 2]))
        {
            sets.Add(i);
        }
    }

    // Convert the list of set indices to an array and return it
    return sets.ToArray();
}

public void BuildHouses(int set, int amount)
{
    // By default assume a 3-property set
    int last = 2;

    // Adjust to 2-property set if applicable
    if (set == 0 || set == 7)
    {
        last = 1;
    }

    // Build houses one at a time
    for (int i = 0; i < amount; i++)
    {
        // Find the property with the least number of houses (backwards search)
        int bj = last;

        for (int j = last - 1; j >= 0; j--)
        {
            // Track the property with fewer houses to maintain building balance
            if (houses[SETS[set, bj]] > houses[SETS[set, j]])
            {
                bj = j;
            }
        }

        // Increment house count on the selected property
        houses[SETS[set, bj]]++;

        // Inform the adapter about the updated house count
        adapter.SetHouse(SETS[set, bj], houses[SETS[set, bj]]);
    }
}

public void SellHouses(int set, int amount)
{
    // By default assume a 3-property set
    int last = 2;

    // Adjust to 2-property set if applicable
    if (set == 0 || set == 7)
    {
        last = 1;
    }

    // Sell houses one at a time
    for (int i = 0; i < amount; i++)
    {
        // Find the property with the most houses (forward search)
        int bj = 0;

        for (int j = 0; j <= last; j++)
        {
            // Track the property with more houses to maintain selling balance
            if (houses[SETS[set, bj]] < houses[SETS[set, j]])
            {
                bj = j;
            }
        }

        // Decrement house count on the selected property
        houses[SETS[set, bj]]--;

        // Inform the adapter about the updated house count
        adapter.SetHouse(SETS[set, bj], houses[SETS[set, bj]]);
    }
}
}