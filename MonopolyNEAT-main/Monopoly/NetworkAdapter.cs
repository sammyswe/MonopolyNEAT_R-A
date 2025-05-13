// NetworkAdapter.cs
// ------------------
// This class serves as a translator between the game state of Monopoly
// and the neural network's input format. It encodes various aspects of the
// game (player turns, money, positions, ownership, etc.) into a float array (`pack`)
// that the neural network can understand and use to make decisions.

/*

TO-DO

It defines what information the neural network sees.
To train for optimal bidding or jail rent dynamics, you may want to:
Add new input slots in pack[] (e.g., canCollectRent).
Include data like recent auction results, property desirability, etc.
Normalize and convert any new metrics you create in Analytics.cs.
Let me know when you're ready to move to the next file (e.g. Program.cs, Tournament.cs, or the Monopoly game logic), or if you'd like help extending this class to support your new rules.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NetworkAdapter
{
    // The float array that will store all encoded inputs for the neural network
    public float[] pack;

    // Indices that represent where certain categories start in the 'pack' array
    public int turn = 0;
    public int pos = 4;
    public int mon = 8;
    public int card = 12;
    public int jail = 16;
    public int own = 20;
    public int mort = 48;
    public int house = 76;
    public int select = 98;
    public int select_money = 126;

    // Maps board index to the index used for property-based arrays (ownership, house, etc.)
    public int[] PROPS = new int[40] { -1, 0, -1, 1, -1, 2, 3, -1, 4, 5, -1, 6, 7, 8, 9, 10, 11, -1, 12, 13, -1, 14, -1, 15, 16, 17, 18, 19, 20, 21, -1, 22, 23, -1, 24, 25, -1, 26, -1, 27 };

    // Similar mapping for house positions (only properties that can have houses)
    public int[] HOUSES = new int[40] { -1, 0, -1, 1, -1, -1, 2, -1, 3, 4, -1, 5, -1, 6, 7, -1, 8, -1, 9, 10, -1, 11, -1, 12, 13, -1, 14, 15, -1, 16, -1, 17, 18, -1, 19, -1, -1, 20, -1, 21 };

    // Constructor initializes a new pack with 127 float inputs
    public NetworkAdapter()
    {
        pack = new float[127];
    }

    // Resets the pack to all zeros
    public void Reset()
    {
        pack = new float[127];
    }

    // Normalizes money to a 0-1 float scale based on max $4000
    public float ConvertMoney(int money)
    {
        float norm = (float)money / 4000.0f;
        float clamp = Math.Clamp(norm, 0.0f, 1.0f);

        return clamp;
    }

    // Converts a neural network float output back to money
    public float ConvertMoneyValue(float value)
    {
        return value * 4000.0f;
    }

    // Converts a house-related float value back to real house units (max 15)
    public float ConvertHouseValue(float value)
    {
        if (value <= 0.5f)
        {
            value = 0.0f;
        }

        return value * 15.0f;
    }

    // Normalizes a board position (0-39) to a 0-1 scale
    public float ConvertPosition(int position)
    {
        float norm = (float)position / 39.0f;
        float clamp = Math.Clamp(norm, 0.0f, 1.0f);

        return clamp;
    }

    // Clamps a card count (e.g., get-out-of-jail cards) to between 0-1
    public float ConvertCard(int cards)
    {
        float clamp = Math.Clamp(card, 0.0f, 1.0f);
        return clamp;
    }

    // Normalizes house count (max 5) to a float between 0-1
    public float ConvertHouse(int houses)
    {
        float norm = (float)houses / 5.0f;
        float clamp = Math.Clamp(norm, 0.0f, 1.0f);

        return clamp;
    }

    // Sets which player's turn it is (one-hot encoded over 4 values)
    public void SetTurn(int index)
    {
        for (int i = 0; i < 4; i++)
        {
            pack[i] = 0.0f;
        }

        pack[index] = 1.0f;
    }

    // Sets a one-hot encoded property selection flag
    public void SetSelection(int index)
    {
        for (int i = select; i < select + 29; i++)
        {
            pack[i] = 0.0f;
        }

        pack[select + PROPS[index]] = 1.0f;
    }

    // Manually sets a selection slot to a given state value
    public void SetSelectionState(int index, int state)
    {
        pack[select + PROPS[index]] = state;
    }

    // Sets a flag for contextual decision making about money
    public void SetMoneyContext(int state)
    {
        pack[select_money] = state;
    }

    // Resets the entire selection region to zero
    public void ClearSelectionState()
    {
        for (int i = select; i < select + 29; i++)
        {
            pack[i] = 0.0f;
        }
    }

    // Sets the encoded board position for a given player
    public void SetPosition(int index, int position)
    {
        pack[pos + index] = ConvertPosition(position);
    }

    // Sets the encoded money value for a given player
    public void SetMoney(int index, int money)
    {
        pack[mon + index] = ConvertMoney(money);
    }

    // Sets the encoded card count for a given player
    public void SetCard(int index, int cards)
    {
        pack[card + index] = ConvertCard(cards);
    }

    // Sets jail status for a given player (1 = in jail, 0 = free)
    public void SetJail(int index, int state)
    {
        pack[jail + index] = state;
    }

    // Encodes which player owns a given property
    public void SetOwner(int property, int state)
    {
        float convert = (state + 1) / 4.0f; // maps -1 (unowned) to 0, 0-3 to 0.25-1

        pack[own + PROPS[property]] = convert;
    }

    // Sets whether a property is mortgaged (0 or 1)
    public void SetMortgage(int property, int state)
    {
        pack[mort + PROPS[property]] = state;
    }

    // Sets the encoded house count on a property
    public void SetHouse(int property, int houses)
    {
        pack[house + HOUSES[property]] = ConvertHouse(houses);
    }
}
