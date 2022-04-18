using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace poker
{
    /// <summary>
    /// Interaction logic for Play.xaml
    /// </summary>
    public partial class Play : Page
    {
        private Game game;

        public Play()
        {
            game = new Game();
            game.newGame();
            InitializeComponent();

            // Make the cards look better
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);
            DataContext = game;
        }

        //Hide cards played by computer opponent
        private void hideCompPlayedCards()
        {
            Image playedCardImg;
            string s = "p2card" + game.getPlayedCard(game.PLAYER2).ToString();
            playedCardImg = FindName(s) as Image;
            playedCardImg.Visibility = Visibility.Hidden;
        }

        // Make images containing cards in hands visible for showdown at the end of a round
        private void makeCardsVisible()
        {
            Image playedCardImg;
            for (int i = 1; i <= game.NUM_OF_PLAYERS; i++)
                for (int j = 1; j <= game.CARDS_PER_HAND; j++)
                {
                    playedCardImg = FindName("p" + i.ToString() +
                        "card" + j.ToString()) as Image;
                    playedCardImg.Visibility = Visibility.Visible;
                }
        }

        // Mark card to be substituted or play a card depending on game stage
        private void selectCard(object sender, RoutedEventArgs e)
        {
            if (!game.roundOver())
            {
                Image selectedCard = (Image)sender;

                //Card number is stored at end of card tag
                char lastChar = selectedCard.Tag.ToString().Last();
                int cardNumber = lastChar - '0';

                if (game.subsFinished())
                {
                    if (game.mayPlayCard(cardNumber))
                    {
                        game.playCard(cardNumber);
                        selectedCard.Visibility = Visibility.Hidden;
                        hideCompPlayedCards();

                        if (game.roundOver())
                        {
                            makeCardsVisible();
                            Button btn = FindName("controlBtn") as Button;
                            btn.Content = "Next";
                            btn.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    game.markCardForSub(cardNumber);
                    selectedCard.Opacity = 0.5;
                }
            }
        }

        // Initiate cards substitution or start next round depending on game stage
        private void pressBtn(object sender, RoutedEventArgs e)
        {
            setOpaqueCards();
            game.doSub();
            if (game.subsFinished())
            {
                Button btn = FindName("controlBtn") as Button;
                if (game.roundOver())
                {
                    game.newRound();
                    btn.Content = "Sub";
                    btn.Visibility = Visibility.Visible;
                }
                else
                    btn.Visibility = Visibility.Hidden;
            }
        }

        private void setOpaqueCards()
        {
            int[] toSubCards = game.getCardsToSub();
            foreach (int subbedCard in toSubCards)
            {
                Image subbedImage = FindName("p1card" + subbedCard.ToString()) as Image;
                subbedImage.Opacity = 1;
            }
        }

        private void pressSaveBtn(object sender, RoutedEventArgs e)
        {
            game.saveGame();
        }

        private void pressLoadBtn(object sender, RoutedEventArgs e)
        {
            setOpaqueCards();
            game.loadGame();

            if (game.subsFinished())
            {
                Image cardImg;
                for (int i = 1; i <= game.NUM_OF_PLAYERS; i++)
                    for (int j = 1; j <= game.CARDS_PER_HAND; j++)
                    {
                        cardImg = FindName("p" + i.ToString() +
                            "card" + j.ToString()) as Image;
                        if (game.cardHasBeenPlayed(i, j))
                            cardImg.Visibility = Visibility.Hidden;
                        else
                            cardImg.Visibility = Visibility.Visible;
                    }

                Button btn = FindName("controlBtn") as Button;
                if (game.roundOver())
                {
                    makeCardsVisible();
                    btn.Content = "Next";
                    btn.Visibility = Visibility.Visible;
                }
                else
                    btn.Visibility = Visibility.Hidden;
            }
            else
            {
                makeCardsVisible();
                Button btn = FindName("controlBtn") as Button;
                btn.Content = "Sub";
                btn.Visibility = Visibility.Visible;
            }
        }
    }
}
