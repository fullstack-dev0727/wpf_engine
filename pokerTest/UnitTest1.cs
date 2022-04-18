using System;
using poker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Media.Imaging;

// Testerna ej uppdaterade för lab 3!

namespace pokerTest
{
    [TestClass]
    public class UnitTest1
    {
        Game game;

        private void resetEnvironment()
        {
            game = new Game();
            game.newGame();
        }

        [TestMethod]
        public void InitialGameStateTest()
        {
            resetEnvironment();
            Assert.IsFalse(game.subsFinished());
            Assert.IsFalse(game.roundOver());
        }

        [TestMethod]
        public void RoundOverTest()
        {
            resetEnvironment();
            game.playCard(1);
            game.playCard(2);
            game.playCard(3);
            game.playCard(4);
            Assert.IsFalse(game.roundOver());
            game.playCard(5);
            Assert.IsTrue(game.roundOver());
        }

        [TestMethod]
        public void SubsOverTest()
        {
            resetEnvironment();
            game.doSub();
            Assert.IsFalse(game.subsFinished());
            game.doSub();
            Assert.IsTrue(game.subsFinished());
        }

        [TestMethod]
        public void MarkForSubstitutionTest()
        {
            resetEnvironment();
            int[] toSub;

            game.markCardForSub(1);
            game.markCardForSub(5);
            toSub = game.getCardsToSub();

            // Ensure card 1 and 5 have been marked for sub
            Assert.AreEqual(1, toSub[0]);
            Assert.AreEqual(5, toSub[1]);

            game.doSub();
            toSub = game.getCardsToSub();

            // Ensure no cards are marked for sub
            Assert.AreEqual(0, toSub.Length);
        }

        [TestMethod]
        public void DoSubstitutionTest()
        {
            resetEnvironment();
            var card1 = game.P1_Card1;
            var card2 = game.P1_Card5;

            game.markCardForSub(1);
            game.markCardForSub(5);
            game.doSub();

            // Ensure card 1 and 5 have been replaced
            Assert.AreNotEqual(card1, game.P1_Card1);
            Assert.AreNotEqual(card2, game.P1_Card5);
        }

        [TestMethod]
        public void PlayedCardUpdateTest()
        {
            resetEnvironment();
            var card1 = game.P1_Card1;
            game.playCard(1);
            Assert.AreEqual(card1, game.P1_Played);
        }

        [TestMethod]
        public void CorrectCardPlayedTest()
        {
            resetEnvironment();
            game.playCard(1);
            Assert.AreEqual(1, game.getPlayedCard(game.PLAYER1));
        }

        [TestMethod]
        public void PlayCardAllowedTest()
        {
            resetEnvironment();
            game.setStickTimer(1);
            for (int i = 0; i < 50; i++)
            {
                game.doSub();
                game.doSub();
                game.playCard(1);

                // Sleep to allow computer player to play before proceeding
                System.Threading.Thread.Sleep(40);

                char followThisSuit = game.P2_Played.ToString()[6];
                if (game.P1_Card5.ToString()[6] != followThisSuit &
                    (game.P1_Card2.ToString()[6] == followThisSuit ||
                     game.P1_Card3.ToString()[6] == followThisSuit ||
                     game.P1_Card4.ToString()[6] == followThisSuit))
                     // Trying to play wrong suit when having a card of proper suit is not allowed
                     Assert.IsFalse(game.mayPlayCard(5));
                else
                    Assert.IsTrue(game.mayPlayCard(5));
                game.newRound();
            }
        }
    }
}
