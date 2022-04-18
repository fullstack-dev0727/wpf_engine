using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poker
{
    public class Card
    {
        private string suit;
        private int number;

        public Card(string _suit, int _number)
        {
            suit = _suit;
            number = _number;
        }

        public override String ToString()
        {
            return suit + number.ToString();
        }

        private bool sameSuit(Card comp)
        {
            return this.suit == comp.suit;
        }

        private bool isLower(Card comp)
        {
            if (this.number == 1)
                return false;
            if (comp.number == 1)
                return true;
            return this.number < comp.number;
        }

        public static bool operator<(Card o1, Card o2)
        {
            return o1.sameSuit(o2) && o1.isLower(o2);
        }

        public static bool operator>(Card o1, Card o2)
        {
            return !(o1 < o2);
        }

        public string getSuit() { return suit; }
        public int getNumber() { return number; }
    }
}
