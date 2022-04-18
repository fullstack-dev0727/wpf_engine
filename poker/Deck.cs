using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poker
{
    class Deck
    {
        private Card[] cards;
        private int top;
        private readonly string[] suits = { "h", "s", "d", "c" };

        public Deck()
        {
            cards = new Card[52];
            top = 0;
            makeDeck();
        }

        private void makeDeck()
        {
            int i = 0;
            foreach (string suit in suits)
                for (int j = 1; j <= 13; i++, j++)
                {
                    cards[i] = new Card(suit, j);
                }
        }

        public Card getCardByIndex(int index)
        {
            return cards[index];
        }

        public Card getCardByString(string str)
        {
            int i=0;
            while (cards[i].ToString() != str)
                i++;
            return cards[i];
        }

        // Move cards in hands h1, h2 to front of deck array and set top to draw from rest
        public void adjustDeck(Card[] h1, Card[] h2)
        {
            top = 0;
            adjustForHand(h1);
            adjustForHand(h2);
        }

        private void adjustForHand(Card[] hand)
        {
            foreach (Card card in hand)
            {
                int i = 0;
                while (cards[i] != card)
                    i++;

                swap(top, i);
                top++;
            }
        }

        // Returns 1 card
        public Card draw()
        {
            Card topCard = cards[top];
            top++;
            return topCard; 
        }

        public void shuffle()
        {
            Card tmp;
            Random rng = new Random();
            for (int i=0; i < 1000; i++)
                swap(rng.Next(0, 51), rng.Next(0, 51));
            top = 0;
        }

        private void swap(int c1, int c2)
        {
            Card tmp = cards[c1];
            cards[c1] = cards[c2];
            cards[c2] = tmp;
        }
    }
}
