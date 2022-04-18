using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Timers;
using System.Data.Entity;

// In this code the word "stick" is used to indicate 1 round of all players playing 1 card

namespace poker
{
    public class Game : INotifyPropertyChanged
    {
        public readonly int CARDS_PER_HAND = 5,
                  ALLOWED_SUBST = 2,
                  STICK_ROUNDS = 5,
                  NUM_OF_PLAYERS = 2,
                  PLAYER1 = 0,
                  PLAYER2 = 1,
                  WAIT_TIME = 500, // Time between stick-rounds
                  NONE = -1,
                  STICK_POINTS = 3, // Points awarded for winning 1 stick
                  PLAYERID = 1; // Used as id for interacting with the database

        private Deck deck;
        private Card[] p1Hand, p2Hand;
        private Card[] p1RemainingCards, p2RemainingCards;
        private Queue<int> cardsToSub;
        private BitmapImage[] cardImages;
        private int subRound, stickRound,
            onPlayer, // Who's turn it is
            firstPlayer; // Player who played the first card in a stick
        private int[] playerScore, playerPlayed;
        private System.Timers.Timer stickRoundPauseTimer;
        private Card followCard; // todo remove
        // This array is used when converting a string into the cards it represents
        private static readonly char[] cardBeginnings = { 'h', 's', 'd', 'c', '-', ' ' };

        public Game()
        {
            deck = new Deck();
            p1Hand = new Card[CARDS_PER_HAND];
            p2Hand = new Card[CARDS_PER_HAND];
            p1RemainingCards = new Card[CARDS_PER_HAND];
            p2RemainingCards = new Card[CARDS_PER_HAND];
            cardsToSub = new Queue<int>();
            playerScore = new int[NUM_OF_PLAYERS];
            playerPlayed = new int[CARDS_PER_HAND];

            // Card backside at index 0
            cardImages = new BitmapImage[53];
            loadImages();
            setStickTimer(WAIT_TIME);
        }

        // Load game if there exists a save
        public void loadGame()
        {
            using (var db = new PokerDbEntities())
            {
                var query = from e in db.GameSession
                            where e.id == PLAYERID
                            select e;

                if (query.Any())
                {
                    GameSession session = query.First();
                    loadData(session);
                    notifyCardsChanged();
                    cardsToSub.Clear();
                    if (onPlayer != firstPlayer)
                        if (onPlayer == PLAYER1)
                            followCard = p2Hand[playerPlayed[PLAYER2]];
                        else
                            followCard = p1Hand[playerPlayed[PLAYER1]];

                    if (!subsFinished())
                        // Substitutions possible so prevent drawing card already drawn
                        deck.adjustDeck(p1Hand, p2Hand);

                    if (firstPlayer == PLAYER2 && onPlayer == PLAYER2)
                        stickRoundPauseTimer.Start();
                }
            }
        }

        public bool cardHasBeenPlayed(int player, int card)
        {
            if (player == 1)
                return p1RemainingCards[card - 1] == null;
            else
                return p2RemainingCards[card - 1] == null;
        }

        // Save current game state to the database. Will replace save if one exists.
        public void saveGame()
        {
            using (var db = new PokerDbEntities())
            {
                var query = from e in db.GameSession
                            where e.id == PLAYERID
                            select e;
                if (query.Any())
                {
                    GameSession session = query.First();
                    setSaveData(session);
                }
                else
                {
                    GameSession session = new GameSession();
                    setSaveData(session);
                    db.GameSession.Add(session);
                }
                db.SaveChanges();
            }
        }

        private void setSaveData(GameSession session)
        {
            session.id = PLAYERID;
            session.p1score = P1_Score;
            session.p2score = P2_Score;
            session.p1hand = cardsToString(p1Hand);
            session.p2hand = cardsToString(p2Hand);
            session.p1remaining = cardsToString(p1RemainingCards);
            session.p2remaining = cardsToString(p2RemainingCards);
            session.p1played = playerPlayed[PLAYER1];
            session.p2played = playerPlayed[PLAYER2];
            session.first_player = firstPlayer;
            session.on_player = onPlayer;
            session.stick_round = stickRound;
            session.sub_round = subRound;
        }

        private void loadData(GameSession session)
        {
            P1_Score = (int)session.p1score;
            P2_Score = (int)session.p2score;
            stringToCards(p1Hand, session.p1hand);
            stringToCards(p2Hand, session.p2hand);
            stringToCards(p1RemainingCards, session.p1remaining);
            stringToCards(p2RemainingCards, session.p2remaining);
            playerPlayed[PLAYER1] = (int)session.p1played;
            playerPlayed[PLAYER2] = (int)session.p2played;
            firstPlayer = (int)session.first_player;
            onPlayer = (int)session.on_player;
            stickRound = (int)session.stick_round;
            subRound = (int)session.sub_round;
        }

        // Set up card references in param cards for hand represented in param cardStr
        private void stringToCards(Card[] cards, string cardStr)
        {
            int begin = 0;
            for (int i = 0; i < CARDS_PER_HAND; i++)
            {
                if (cardStr[begin] == '-')
                {
                    cards[i] = null;
                    begin++;
                }
                else
                {
                    int len = 1;
                    for (int j = begin + 1; j < cardStr.Length &&
                        !cardBeginnings.Contains(cardStr[j]); j++)
                        len++;

                    string nextCard = cardStr.Substring(begin, len);
                    cards[i] = deck.getCardByString(nextCard);
                    begin += len;
                }
            }
        }

        // Represent a hand of cards as a string
        private string cardsToString(Card[] cards)
        {
            string retVal = "";
            foreach (Card card in cards)
            {
                if (card != null)
                    retVal += card.ToString();
                else
                    retVal += "-";
            }
            return retVal;
        }

        public void newGame()
        {
            for (int i = 0; i < NUM_OF_PLAYERS; i++)
                playerScore[i] = 0;
            newRound();
        }

        public void newRound()
        {
            deck.shuffle();
            subRound = stickRound = 1;
            firstPlayer = PLAYER1;
            onPlayer = firstPlayer;

            for (int j = 0; j < NUM_OF_PLAYERS; j++)
                playerPlayed[j] = NONE;

            for (int i = 0; i < CARDS_PER_HAND; i++)
            {
                p1Hand[i] = deck.draw();
                p2Hand[i] = deck.draw();
            }
            notifyCardsChanged();
        }

        private void notifyCardsChanged()
        {
            for (int j = 1; j <= NUM_OF_PLAYERS; j++)
            {
                for (int i = 1; i <= CARDS_PER_HAND; i++)
                {
                    string card = "P" + j.ToString() + "_Card" + i.ToString();
                    OnPropertyChanged(card);
                }
                string s = "P" + j.ToString() + "_Played";
                OnPropertyChanged(s);
            }
        }

        public void setStickTimer(int time)
        {
            stickRoundPauseTimer = new System.Timers.Timer(time);
            stickRoundPauseTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }

        public void loadImages()
        {
            cardImages[0] = new BitmapImage(new Uri("Media/Back.png", UriKind.Relative));
            for (int i = 1; i < 53; i++)
            {
                Card card = deck.getCardByIndex(i - 1);
                cardImages[i] = new BitmapImage(new Uri("Media/" + card.ToString() + ".png", UriKind.Relative));
            }
        }

        // Called for computer players. If the round is not over, the cards in their hands will
        // be dispayed with the backsides up
        private BitmapImage getImageCompPlayer(Card card)
        {
            if (!roundOver())
            {
                // Return card backside
                return cardImages[0];
            }
            return getImage(card);
        }

        // Return the image resource for param card
        private BitmapImage getImage(Card card)
        {
            int suitOffset = 0;
            switch (card.getSuit())
            {
                //For case 'h' the offset = 0

                case "s":
                    suitOffset = 13;
                    break;

                case "d":
                    suitOffset = 26;
                    break;

                case "c":
                    suitOffset = 39;
                    break;
            }

            suitOffset += card.getNumber();
            return cardImages[suitOffset];
        }

        public void markCardForSub(int cardNum)
        {
            cardsToSub.Enqueue(cardNum);
        }

        // Return an array containing all cards the player want to substitute
        public int[] getCardsToSub()
        {
            return cardsToSub.ToArray();
        }

        // Replace all substituted cards by drawing new ones and ask gui to update
        public void doSub()
        {
            while (cardsToSub.Any())
            {
                int cardIndex = cardsToSub.Dequeue() - 1;
                p1Hand[cardIndex] = deck.draw();

                string s = "P1_Card" + (cardIndex + 1).ToString();
                OnPropertyChanged(s);
            }
            subRound++;

            if (subsFinished())
            {
                for (int i = 0; i < CARDS_PER_HAND; i++)
                {
                    p1RemainingCards[i] = p1Hand[i];
                    p2RemainingCards[i] = p2Hand[i];
                }
            }
        }

        public int getPlayedCard(int player)
        {
            return playerPlayed[player] + 1;
        }

        private void showAllCompCards()
        {
            for (int j = 1; j <= CARDS_PER_HAND; j++)
            {
                string s = "P2_Card" + j.ToString();
                OnPropertyChanged(s);
            }
        }

        private int decideStickWinner()
        {
            Card p1 = p1Hand[playerPlayed[PLAYER1]],
                 p2 = p2Hand[playerPlayed[PLAYER2]];

            if (firstPlayer == PLAYER1)
            {
                if (p1 < p2)
                    return PLAYER2;
                return PLAYER1;
            }
            if (p2 < p1)
                return PLAYER1;
            return PLAYER2;
        }

        private void awardRoundPoints(int winner)
        {
            if (winner == PLAYER1)
                P1_Score = P1_Score + STICK_POINTS;
            else
                P2_Score = P2_Score + STICK_POINTS;
        }

        public void playCard(int cardNum)
        {
            playerPlayed[PLAYER1] = cardNum - 1;
            p1RemainingCards[cardNum - 1] = null;
            OnPropertyChanged("P1_Played");
            onPlayer++;

            // New stickround has begun
            if (firstPlayer == PLAYER1)
            {
                followCard = p1Hand[cardNum - 1];
                compPlaySticks();
            }
            stickRound++;

            if (roundOver())
            {
                // Flip comps cards over1
                showAllCompCards();
                int roundWinner = decideStickWinner();
                awardRoundPoints(roundWinner);
            }
            else
                stickRoundPauseTimer.Start();
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            stickRoundPauseTimer.Stop();

            firstPlayer = decideStickWinner();
            onPlayer = firstPlayer;

            if (firstPlayer == PLAYER2)
            {
                compPlaySticks();
                followCard = p2Hand[playerPlayed[PLAYER2]];
            }
        }

        private void compPlaySticks()
        {
            int toPlay = 1;
            foreach (Card card in p2RemainingCards)
            {
                if (card != null && (firstPlayer == PLAYER2 || card.getSuit() == followCard.getSuit()))
                    break;
                toPlay++;
            }

            if (toPlay > CARDS_PER_HAND)
            {
                toPlay = 1;
                foreach (Card card in p2RemainingCards)
                {
                    if (card != null)
                        break;
                    toPlay++;
                }
            }
            p2RemainingCards[toPlay - 1] = null;
            playerPlayed[PLAYER2] = toPlay - 1;
            OnPropertyChanged("P2_Played");

            if (firstPlayer == PLAYER2)
                onPlayer = PLAYER1;
        }

        // Return true if no more substitutions are allowed
        public bool subsFinished()
        {
            return subRound > ALLOWED_SUBST;
        }

        public bool roundOver()
        {
            return stickRound > STICK_ROUNDS;
        }

        // Return true if it is the human players turn
        private bool playersTurn()
        {
            return onPlayer == PLAYER1;
        }

        private bool cardFollowsSuit(int cardNum)
        {
            String followThisSuit = followCard.getSuit();

            if (p1Hand[cardNum - 1].getSuit() == followThisSuit)
                return true; // Want to play card of proper suit

            foreach (Card card in p1RemainingCards)
                if (card != null && card.getSuit() == followThisSuit)
                    return false;

            // Have no card of proper suit, allow play
            return true;
        }

        // Return true if player is allowed to play card
        public bool mayPlayCard(int cardNum)
        {
            if (playersTurn())
                if (firstPlayer == PLAYER1 || cardFollowsSuit(cardNum))
                    return true;
            return false;
        }

        public BitmapImage P1_Played
        {
            get
            {
                if (playerPlayed[PLAYER1] > NONE)
                    return getImage(p1Hand[playerPlayed[PLAYER1]]);
                return null;
            }
        }
        public BitmapImage P2_Played
        {
            get
            {
                if (playerPlayed[PLAYER2] > NONE)
                    return getImage(p2Hand[playerPlayed[PLAYER2]]);
                return null;
            }
        }
        public int P1_Score
        {
            get { return playerScore[PLAYER1]; }
            set { playerScore[PLAYER1] = value; OnPropertyChanged("P1_Score"); }
        }
        public int P2_Score
        {
            get { return playerScore[PLAYER2]; }
            set { playerScore[1] = value; OnPropertyChanged("P2_Score"); }
        }
        public BitmapImage P1_Card1
        {
            get { return getImage(p1Hand[0]); }
        }
        public BitmapImage P1_Card2
        {
            get { return getImage(p1Hand[1]); }
        }
        public BitmapImage P1_Card3
        {
            get { return getImage(p1Hand[2]); }
        }
        public BitmapImage P1_Card4
        {
            get { return getImage(p1Hand[3]); }
        }
        public BitmapImage P1_Card5
        {
            get { return getImage(p1Hand[4]); }
        }
        public BitmapImage P2_Card1
        {
            get { return getImageCompPlayer(p2Hand[0]); }
        }
        public BitmapImage P2_Card2
        {
            get { return getImageCompPlayer(p2Hand[1]); }
        }
        public BitmapImage P2_Card3
        {
            get { return getImageCompPlayer(p2Hand[2]); }
        }
        public BitmapImage P2_Card4
        {
            get { return getImageCompPlayer(p2Hand[3]); }
        }
        public BitmapImage P2_Card5
        {
            get { return getImageCompPlayer(p2Hand[4]); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
