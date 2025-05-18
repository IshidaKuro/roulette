using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



///<To-Do>
///
/// ensure that bets on 12 do not pay out when we roll a 1 - 1 is contained in the string "12"
/// 
/// disable buttons that are not adjacent to the first button that was clicked for a split bet - right now we can bet on 13 and 36 in a split bet
///     
/// add functionality for:
/// 
///         Betting on 3 numbers
///         Betting on 4 Numbers
///         Betting on 6 Numbers
///         
///</To-Do>

namespace roulette
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        bool reroll = false; //flag to determine if we're rolling again with the same bets 
        //the number of chips that the player has
        int chips = 0;
        string winningBets = "";

        //random number generator
        Random rng = new Random();

        //store the bets and the values that have been bet
        Dictionary<string, int> Bets = new Dictionary<string, int>();

        //numbers on a roulette board that are red -- if the number is greater than 0 and not on this list then we can assume that it is black
        static int[] reds = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

        //bets that are 50/50 shots - payout 1-1, so £1 of winnings and £1 for the initial ante
        static string[] doublepayoutbets = { "RED", "BLACK", "EVEN", "ODD", "1 to 18" , "19 to 36" };

        //bets that payout 2-1, so return triple when you win
        static string[] triplepayoutbets = { "1st Column", "2nd Column","3rd Column", "1st 12", "2nd 12","3rd 12"};
                
        //flags and variables for split bets
        bool splitBetBeingPlaced = false;       
        bool waitingForSecondClick = false;

        int firstButton = -2;
        int winningNumber;
        //we want to have a list of all the buttons, this is so we can disable buttons


        public MainWindow()
        {
            InitializeComponent();
            //initialize the player's chips when the main window is created
            chips = 1500;

            List<Button> hoverButtons = new List<Button> { Btn1, Btn2, Btn3, Btn4, Btn5, Btn6, Btn7, Btn8, Btn9, Btn10, Btn11, Btn12, Btn13, Btn14, Btn15, Btn16, Btn17, Btn18, Btn19, Btn20, Btn21, Btn22, Btn23, Btn24, Btn25, Btn26, Btn27, Btn28, Btn29, Btn30, Btn31, Btn32, Btn33, Btn34, Btn35, Btn36 };

            //set the player's chip balance in the UI
            lblBalance.Content = "Player Balance: " + chips;       
        }


        /// <summary>
        /// function that is called when the "Spin" button has been clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Spin_Click(object sender, RoutedEventArgs e)
        {
            //randomly generate the winning number
            winningNumber = rng.Next(-1,36);
            
            //reformat -1 to 00
            if(winningNumber == -1)
            {
                txtNumberOutput.Text = "00";
            }
            else
            {
                //display the appropriate number in the winning number box
                txtNumberOutput.Text = winningNumber.ToString();
            }

            //set appropritate background and text colours
            if (winningNumber > 0)
            {
                if (reds.Contains(winningNumber))
                {
                    txtNumberOutput.Foreground = new SolidColorBrush(Colors.Black);
                    txtNumberOutput.Background = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    txtNumberOutput.Foreground = new SolidColorBrush(Colors.White);
                    txtNumberOutput.Background = new SolidColorBrush(Colors.Black);
                }

            }
            else
            {
                txtNumberOutput.Foreground = new SolidColorBrush(Colors.White);
                txtNumberOutput.Background = new SolidColorBrush(Colors.ForestGreen);
            }
            



            winningBets = CalculateWinningBets(winningNumber);
            PayoutBets();

            //update the UI
            Bets.Clear();
            refreshUI();
        }

        /// <summary>
        /// This method returns a string of the bets that have won separated by a comma eg. "21, RED, ODD...etc."
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
       public string CalculateWinningBets(int input)
        {
            //number
            string result = "";
            
            //format -1 as 00
            if (input ==-1)
            {
                result = "00";
            }
            else
            {
                result = input.ToString();
            }        
            
            //first five, the first five values are 00, 0, 1, 2 and 3
            if(input < 4)
            {
                result += ", First Five";
            }

            //if the number is not 0 or 00
            if (input > 0)
            {
                //colour
                if (reds.Contains(input))
                {
                    result += ", RED";
                }
                else
                { 
                    result += ", BLACK";
                }

                //even or odd
                if (input % 2 == 0) { result += ", EVEN"; } else { result += ", ODD"; }

                //top or bottom half
                if(input > 18) { result += ", 19 to 36"; } else { result += ", 1 to 18"; }
                
                //column
                int s = input %3 ;

                
                if (s == 1) //first column
                {
                    result += ", 1st Column";
                }                              
                else if (s == 2) //second column
                {
                    result += ", 2nd Column";
                }                
                else if (s == 0) //third column
                {
                    result += ", 3rd Column";
                }


                //dozens
                if (input > 23) { result += ", 3rd 12"; } else if (input < 13 && input > 0) { result += ", 1st 12"; } else { result += ", 2nd 12"; }
            }

            Debug.WriteLine(result);
            

            return result;

        }

        //function that returns which column and dozen that the result is contained within
        //returns a string formatted "Nth column, Nth Dozen" if values are not zeroes
        //returns an empty string if they are
        public string GetPositions (short input)
        {
            string result = "";

            //if the number is 0 or 00 it is not in any columnns or dozens
            if (input < 0) { return result; }

            //determine which column the number is in
            if (input % 3 == 1) { result += "First Column"; }
            else if (input % 3 == 2) { result += "Second Column"; }
            else if (input % 3 == 0) { result += "Third Column"; }

            

            //determine which dozen the number is is
            
                result += ", ";

                if (input <= 12) { result += "First Dozen"; }
                else if (input <= 24) { result += "Second Dozen"; }
                else if (input <= 36) { result += "Third Dozen"; }
            
                
                return result;
        }

        /// <summary>
        /// function that cycles through the bets made and pays out if the bet has won
        /// </summary>
        public void PayoutBets()
        {
            int winnings = 0;
            
            //for each bet on the list
            foreach(KeyValuePair<string, int> d in Bets)
            {
                //d.Key = the bet placed in the Bets Dict
                if (winningBets.Contains(d.Key))
                {
                    //if we hit a number - pays 35 to 1 - £1 bet returns £36 including initial bet
                    //parse to int so that we don't pay out 36x to any area bets
                    if (int.TryParse(d.Key, out _))
                    {
                        winnings += d.Value * 36;
                    }                    
                    //first five - 00, 0, 1, 2, 3 - payout 7x
                    else if (d.Key.Equals("First Five"))
                    {
                        winnings += d.Value * 7;
                    }

                    //if we hit a double payout bet
                    else if (doublepayoutbets.Contains(d.Key))
                    {
                        winnings += d.Value * 2;
                    }

                    //if we hit a triple payout bet
                    else if (triplepayoutbets.Contains(d.Key))
                    {
                        winnings += d.Value * 3;
                    }

                }

                ///<String.Contains_Bug_Explaination>
                ///
                /// for numeric matches, we can't use "contains" as "1" is contained in "21"
                /// this would mean that we are paying out for bets that didn't actually win
                /// we need to make sure we have exact numeric matches
                /// to do so we will need to grab the actual integer values inside the bets             
                ///
                /// </String.Contains_Bug_Explaination>
                



                //split bets - two numbers - payout 18x
                //if the bet contains both the "SPLIT" keyword and the winning number
                else if (d.Key.Contains("SPLIT") && extractNumericValues(d.Key).Contains(winningNumber))
                {
                    winnings += d.Value * 18;
                }
                //three numbers - street bets - payout 12x
                else if (d.Key.Contains("STREET") && extractNumericValues(d.Key).Contains(winningNumber))
                {
                    winnings += d.Value * 12;
                }

                //four numbers - corner bet - payout 9x
                else if (d.Key.Contains("CORNER") && extractNumericValues(d.Key).Contains(winningNumber))
                {
                    winnings += d.Value * 9;
                }

                //six number bets - line bets - payout 6x
                else if (d.Key.Contains("LINE") && extractNumericValues(d.Key).Contains(winningNumber))
                {
                    winnings += d.Value * 6;
                }

            }

            chips += winnings;

            //let the user know how much they have won, encourage them to try again if they lose
            if (winnings > 0)
            {
                //if we want to play again
                if (MessageBox.Show("Congratulations, you won " + winnings + " chips!! \n Would you like to play the same bets and spin again?", "YIPPEE!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Respin();
                }

            }
            else
            {
                //ask the user if they want to place the same bets, if they do:
                if (MessageBox.Show("You didn't win anything, better luck next time. \n Would you like to place the same bets and spin again?", "oh hecc", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {

              Respin();
                }
                
            }
            

        }

        //------------BETTING FUNCTIONS------------

        //betting on an individual number -- we can just grab the number from the button that was pressed -- this function can also be used for the EVEN, ODD, RED and BLACK bets
        public void NumberButtonClicked(object sender, RoutedEventArgs e)
        {
            //get the button that was pressed
            Button btn = sender as Button;

            //if we're placing a split bet, we need to capture the numbers for the bet
            if (splitBetBeingPlaced)
            {
                //if we can parse the number to int (ignore any buttons that are not numeric)
                if (int.TryParse(btn.Content.ToString(), out int number))
                {
                    //if this is the first number in the split bet
                    if (!waitingForSecondClick)
                    {
                        firstButton = number;
                        waitingForSecondClick = true;
                        //disable any buttons that are not adjacent to the one we have just clicked

                    }
                    //if this is the second number in the split bet, we can construct the bet and close out the split bet
                    else
                    {
                        txtBet.Text = "SPLIT(" + firstButton + "," + number + ")";
                        splitBetBeingPlaced = false;
                    }
                }

            }
            //if we're not placing a split bet
            else
            {
                //get the number that the button corresponds to
                txtBet.Text = btn.Content.ToString();
            }
        }

        //line bets are 6 numbers -- select the top left number and highlight the other 5, if we are on the last column we need to only highlight the last 6 numbers
       
        private void Line_Bet_Button_Click(object sender, RoutedEventArgs e)
        {

            //if the user clicks the line bet button again, exit the mode


            //after this button is clicked, highlight the column that the user is hovering over and the column to the right
            //if the user is hovering over the rightmost column, only highlight the two rightmost columns

            //when the user clicks, create a line bet with the highlighted numbers


        }

        //function is called when the place bet button is pressed
        private void btnPlaceBet_Click(object sender, RoutedEventArgs e)
        {            
            //make sure we're actually betting on something
            if(txtBet.Text == "")
            {
                MessageBox.Show("You must choose something to place a bet on first");
                return;
            }

            //ensure that a bet is being placed
            if (txtBetAmount.Text == "")
            {
                MessageBox.Show("You must bet at least 1 chip");
                return;
            }

            int betAmount = int.Parse(txtBetAmount.Text);

            //if the player doesn't have enough chips to place the bet, display a message and don't add the bet
            if (betAmount > chips) 
            {
                MessageBox.Show("You don't have enough chips to make this bet");
                return;

            }           
            
            //add the bet to the list, if we can't add it, update the value for the appropriate bet
            if (!Bets.TryAdd(txtBet.Text, betAmount)) {Bets[txtBet.Text] += betAmount;}

            //subtract the chips from the player's wallet
            chips-=betAmount;

            //refresh the UI
            refreshUI();
        }

        //function to update the UI of the game when we either place a bet, remove a bet, or complete a round
        public void refreshUI()
        {
            lstBet.Items.Clear();
            lblBalance.Content = "Player Balance:    " + chips;
            foreach (var bet in Bets)
            {
                lstBet.Items.Add(bet.Key + "    :   " + bet.Value + " chips");
            }
        }

        //deletes any non numeric characters from the betting text box as they are added
        private void txtBetAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (!string.IsNullOrEmpty(textBox.Text) && !Regex.IsMatch(textBox.Text, "^[0-9]+$"))
            {
                // Remove invalid characters
                textBox.Text = new string(textBox.Text.Where(char.IsDigit).ToArray());
                textBox.CaretIndex = textBox.Text.Length;

            }
        }

        //function that suppports removing bets from the bet list, this function is called whenever the user presses the delete button to the side of each entry
        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the item associated with this button
            if (sender is Button btn && btn.DataContext is string item)
            {
                //clean up the list item so we can find the bet in the dictionary
                item = item.Substring(0, item.IndexOf(":")).Trim(); //the key is before the colon in the list box string, trim is called to remove whitespace from the key we are looking for
                

                Debug.WriteLine("Removing "+ item + " from the bet list.");
                Bets.TryGetValue(item, out int refund); //get the amount of chips in the bet
                chips += refund; //add them to the number of chips that the player has
                Bets.Remove(item); //remove the item from the list
            }
            refreshUI();
        }

        private void Split_Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            //flip the state of the split bet flag
            splitBetBeingPlaced = !splitBetBeingPlaced;
            waitingForSecondClick = false;
            //create the string that we are going to use to place the bet



            //if the user clicks the split bet button again, cancel the bet

            //highlight the valid numbers that a user can bet on when they hover over their first number selection

            //when the user selects their first number, disable all the invalid bets

            //add the first number to the bet

            //wait for them to click on the second number before adding it to the bet string


            //close the bet string, and return the bet we are placing

        }



        //function that spins the wheel again with the same bets as the last round
        private void Respin()
        {           //if we have less chips than the total number of the bets, we can't spin again
            if (chips < Bets.Values.Sum())
            {
                MessageBox.Show("you cannot respin as you don't have enough chips to place your bets");
                return;
            }

            //place the bets again
            chips -= Bets.Values.Sum();

            //update the UI to show the deducted chips
            refreshUI();
            //spin the wheel again
            Spin_Click(null, null);
        }

        private int[] extractNumericValues(string input)
        {
            int[] values;

            //remove the closing bracket, trim everything upto and including the first bracket, and trim the whitespace
            input = input.Replace(")", "").Substring(input.IndexOf("(")+1).Trim(); 

            //split the string by the commas and parse it to an array of integers
            values = Array.ConvertAll(input.Split(","), int.Parse);

            return values;

        }

        //    //add a hover handler to each button

        //    foreach (var btn in hoverButtons)
        //    {
        //        // Detach first to avoid duplicate handlers if clicked multiple times
        //        btn.MouseEnter -= Button_MouseEnter;
        //        btn.MouseLeave -= Button_MouseLeave;

        //        // Attach hover event handlers
        //        btn.MouseEnter += Button_MouseEnter;
        //        btn.MouseLeave += Button_MouseLeave;
        //    }
        //}

        //private void Button_MouseEnter(object sender, EventArgs e)
        //{
        //    if (sender is Button btn)
        //    {
        //        btn.Background = Brushes.LightSkyBlue; // Highlight color

        //    }
        //}

        //private void Button_MouseLeave(object sender, EventArgs e)
        //{
        //    if (sender is Button btn)
        //    {
        //        int number =int.Parse(btn.Content.ToString());
        //        if(reds.Contains(number))
        //        {
        //            btn.Background = Brushes.Red;
        //        }
        //        else
        //        {
        //            btn.Background = Brushes.Black;
        //        }
        //    }
        //}
    }
}