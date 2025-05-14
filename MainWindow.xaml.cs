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



///<Todo>
///      
///     add functionality for:
///         Betting on 2 numbers
///         Betting on 3 numbers
///         Betting on 4 Numbers
///         Betting on 6 Numbers
///         
///</Todo>

namespace roulette
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

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

        
        //the number of chips that the player has
        int chips = 0;

        string winningBets = "";
        

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
            int winningNumber = rng.Next(-1,36);
            

            //display the winning number

            //reformat -1 to 00
            if(winningNumber == -1)
            {
                txtNumberOutput.Text = "00";
            }
            else
            {
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
                //if there is a bet on and it is contained in the winning string combination
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

                    //split bets - two numbers - payout 18x
                    else if (d.Key.Contains("SPLIT") && d.Key.Contains(winningBets.Substring(0, 2)))
                    {
                        winnings += d.Value * 18;
                    }
                    
                    //three numbers - street bets - payout 12x
                    else if (d.Key.Contains("STREET"))
                    {
                        winnings+=d.Value * 12;
                    }
                    
                    //four numbers - corner bet - payout 9x
                    else if (d.Key.Contains("CORNER"))
                    {
                        winnings += d.Value * 9;
                    }
                
                    //six number bets - line bets - payout 6x
                    else if (d.Key.Contains("LINE"))
                    {
                        winnings += d.Value * 6;
                    }
                }
            }

            //let the user know how much they have won, encourage them to try again if they lose
            if (winnings > 0)
            { MessageBox.Show("Congratulations, you won " + winnings + " chips!!"); }
            else
            { MessageBox.Show("You didn't win anything, better luck next time."); }

            chips += winnings;
        }


        //------------BETTING FUNCTIONS------------

        //betting on an individual number -- we can just grab the number from the button that was pressed -- this function can also be used for the EVEN, ODD, RED and BLACK bets
        public void NumberButtonClicked(object sender, RoutedEventArgs e)
        {
            //get the button that was pressed
            Button btn = sender as Button;

            //get the number that the button corresponds to
            txtBet.Text = btn.Content.ToString();

        }

        //split bets -- allow the user to click on the first number, and then only allow them to click on adjacent numbers
        //also being worked on in desktop build
        public void SplitBetClicked(object sender, RoutedEventArgs e)
        {
            //create the string that we are going to use to place the bet
            string splitBet = "SPLIT (";


            //if the user clicks the split bet button again, cancel the bet

            //highlight the valid numbers that a user can bet on when they hover over their first number selection

            //when the user selects their first number, disable all the invalid bets

            //add the first number to the bet

            //wait for them to click on the second number before adding it to the bet string
            

            //close the bet string, and return the bet we are placing
            splitBet += ")";
            txtBet.Text = splitBet;
            


        }

        //line bets are 6 numbers -- select the top left number and highlight the other 5, if we are on the last column we need to only highlight the last 6 numbers
        //this function is being created on desktop
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


        //delete any non numeric characters from the betting text box as they are added
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