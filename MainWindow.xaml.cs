using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
/// 
/// 
/// 
/// 
/// 
/// optimisation:
/// 
///     pass bets as enumerated values - determine type of bet by length
///     enumerate non numeric bets
///     winning bets to array of ints - run a contains check to determine if a bet has been won
///     
///</To-Do>


///<Numeric-Values-For-Bets>
///
///   
///  Numeric Bets -- -1 to 36
///  
///  Black -- 40
///  Red -- 41
///  Even -- 42
///  Odd -- 43
///  First Half -- 44 
///  Second Half -- 45
///  
///  1st Column -- 46
///  2nd Column -- 47
///  3rd Column -- 48
///  
///  1st 12 -- 49 
///  2nd 12 -- 50
///  3rd 12 -- 51
/// 
///  Split -- byte[2]
///  Street -- byte[3]
///  Corner --byte[4]
///  Line -- byte[6]
/// 
/// </Numeric-Values-For-Bets>


namespace roulette
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        bool reroll = false; //flag to determine if we're rolling again with the same bets        
        int chips = 0; //the number of chips that the player has
        int winningNumber; // the number that the roulette wheel has landed on
        string winningBets = ""; //string to contain the bets that have won. eg: 16, RED, EVEN, 1 to 18, 1st Column, 2nd 12 

        //random number generator
        Random rng = new Random();

        //store the bets and the values that have been bet
        Dictionary<string, int> Bets = new Dictionary<string, int>();

        //----------------static gameplay variables-----------------------------------------------------------------------------------------------------------------------

        //numbers on a roulette board that are red -- if the number is greater than 0 and not on this list then we can assume that it is black
        static int[] reds = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

        //bets that are 50/50 shots - payout 1-1, so £1 of winnings and £1 for the initial ante
        static string[] doublepayoutbets = { "RED", "BLACK", "EVEN", "ODD", "1 to 18", "19 to 36" };

        //bets that payout 2-1, so return triple when you win
        static string[] triplepayoutbets = { "1st Column", "2nd Column", "3rd Column", "1st 12", "2nd 12", "3rd 12" };

        //-----------------------MODE FLAGS-------------------------------------------------------------------------------------------------------------------------------

        bool placingStreetBet = false;
        bool placingLineBet = false;
        bool placingCornerBet = false;
        bool placingSplitBet = false; // flag to determine if a split bet is being placed

        //-----------------------VARIABLES FOR SPLIT BETS--------------------------------------------------------------------------------------------------------

        bool waitingForSecondClick = false; // if we are waiting for the user to click again while making a split bet
        int firstButton; // the first button that was clicked while making a split bet

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

        Button[] AllButtons; //create an array to store all the buttons
        Button[] NumericButtons;
        List<int> acceptableNumbers = new List<int>(); //this is used to store the numbers that we can place a bet on if we are placing a split bet

        //---------------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();
            //initialize the player's chips when the main window is created
            chips = 1500;
                                             
            //set the player's chip balance in the UI
            lblBalance.Content = "Player Balance: " + chips;       
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e) 
        {
            //grab all the buttons on the form so we can enable and disable them later
            AllButtons = MainWindowCanvas.Children.OfType<Button>().ToArray();
            
            List<Button>nb = new List<Button>();
            foreach (Button b in AllButtons) 
            {
                if (int.TryParse(b.Content.ToString(), out _))
                {
                    nb.Add(b);
                }
            }
            NumericButtons = nb.ToArray();
        }

        //------------BUTTON CLICKS------------

        /// <summary>
        /// function that is called when the "Spin" button has been clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Spin_Click(object sender, RoutedEventArgs e)
        {
            //randomly generate the winning number
            winningNumber = (sbyte)rng.Next(-1, 36);

            //reformat -1 to 00
            if (winningNumber == -1)
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

        //function is called when the place bet button is pressed
        private void btnPlaceBet_Click(object sender, RoutedEventArgs e)
        {
            //make sure we're actually betting on something
            if (txtBet.Text == "")
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
            if (!Bets.TryAdd(txtBet.Text, betAmount)) { Bets[txtBet.Text] += betAmount; }

            //subtract the chips from the player's wallet
            chips -= betAmount;

            //refresh the UI
            refreshUI();
        }

        //betting on an individual number -- we can just grab the number from the button that was pressed -- this function can also be used for the EVEN, ODD, RED and BLACK bets
        public void NumberButtonClicked(object sender, RoutedEventArgs e)
        {
            //get the button that was pressed
            Button btn = sender as Button;

            //if we're placing a split bet, we need to capture the numbers for the bet
            if (placingSplitBet)
            {
                //if we can parse the number to int (ignore any buttons that are not numeric)
                if (int.TryParse(btn.Content.ToString(), out int number))
                {
                    //if this is the first number in the split bet
                    if (!waitingForSecondClick)
                    {
                        firstButton = number;
                        waitingForSecondClick = true;

                        //determine the numbers that are adjacent to the one we have just clicked
                        DetermineAcceptableNumbers(number);

                        //only allow the user to click buttons that are appropriate bets
                        foreach (Button b in AllButtons)
                        {
                            //enable the split button so we can cancel the bet
                            if (b.Content.ToString().Contains("Split")) { continue; }
                            //disable the button
                            b.IsEnabled = false;
                            //unless it is on the list

                            //if the button is numeric
                            if (int.TryParse(b.Content.ToString(), out int r))
                            {
                                //if the button is on the list of acceptable numbers
                                if (!acceptableNumbers.Contains(r)) { continue; }

                                //enable the button
                                b.IsEnabled = true;
                            }
                        }


                    }
                    //if this is the second number in the split bet, we can construct the bet and close out the split bet
                    else
                    {
                        txtBet.Text = "SPLIT(" + firstButton + "," + number + ")";
                        placingSplitBet = false;
                        //enable the buttons again
                        EnableNonNumericButtons();
                    }
                }

            }

            //if we are placing a street bet
            else if (placingStreetBet)
            {
                //determine which number has been clicked -- only numbers can be clicked in this mode
                int n = int.Parse(btn.Content.ToString());

                switch (DetermineRow(n))
                {
                    //if the button is [1, 4, 7...]
                    case 1:
                        txtBet.Text = "STREET(" + n + ", " + (n + 1) + ", " + (n + 2) + ")";
                        break;

                    //if the button is [2, 5, 8...]
                    case 2:
                        txtBet.Text = "STREET(" + (n - 1) + ", " + n + ", " + (n + 1) + ")";
                        break;

                    //if the button is [3, 6, 9...]
                    case 3:
                        txtBet.Text = "STREET(" + (n - 2) + ", " + (n - 1) + ", " + n + ")";

                        break;
                }

                //exit the mode
                placingStreetBet = false;
                EnableNonNumericButtons();

            }

            //if we are placing a corner bet
            else if (placingCornerBet)
            {
                //get the number of the button that has been clicked
                int n = int.Parse(btn.Content.ToString());

                //for this bet, the default behaviour will be to grab the number immediately below and the numbers to the right of the number 
                //  eg. [x] [y] []
                //      [y] [y] []
                //      [ ] [ ] []
                int xOffset = -1, yOffset = 3;

                //if the selected number is in the bottom row, grab the number above and the corresponding numbers to the right
                //  eg. [ ] [ ] []
                //      [y] [y] []
                //      [x] [y] []
                if (DetermineRow(n) == 1) { xOffset = 1;}
                
                //if the button is in the last column, grab the numbers to the left
                //  eg. [ ] [y] [x]
                //      [ ] [y] [y]
                //      [ ] [ ] [ ]
                if(n > 33) { yOffset = -3;}

                int[] betnumbers = { n, n + xOffset, n + yOffset, n + xOffset + yOffset };

                txtBet.Text = ConstructString(betnumbers);

                //exit the mode
                placingCornerBet = false;
                EnableNonNumericButtons();
            }

            //if we are placing a line bet
            else if (placingLineBet)
            {
                //get the number of the button that has been clicked
                int n = int.Parse(btn.Content.ToString());
                int xa = -1, xb = 1, yOffset = 3;
                //determine which row we are in and grab all the relevent numbers
                switch(DetermineRow(n))
                {
                    case 1:  xa = 2; break;                    
                    case 3:  xb = -2; break;
                }
                
                //if we are in the last column, grab the numbers to the left
                if (n > 33) { yOffset = -3; }

                int[] betnumbers = { n, n + xa, n+xb, n + yOffset, n + xa + yOffset, n+xb+yOffset };

                txtBet.Text = ConstructString(betnumbers);
                
                //exit the mode
                placingLineBet = false;
                EnableNonNumericButtons();
            }
            //if we're not in a special betting mode
            else
            {
                //get the number that the button corresponds to
                txtBet.Text = btn.Content.ToString();
            }
        }

        //function that suppports removing bets from the bet list, this function is called whenever the user presses the delete button beside their bet entry
        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the item associated with this button
            if (sender is Button btn && btn.DataContext is string item)
            {
                //clean up the list item so we can find the bet in the dictionary
                item = item.Substring(0, item.IndexOf(":")).Trim(); //the key is before the colon in the list box string, trim is called to remove whitespace from the key we are looking for


                Debug.WriteLine("Removing " + item + " from the bet list.");
                Bets.TryGetValue(item, out int refund); //get the amount of chips in the bet
                chips += refund; //add them to the number of chips that the player has
                Bets.Remove(item); //remove the item from the list
            }
            refreshUI();
        }


        //-------------MOUSE HOVER EVENTS---------------------

        List<Button> affectedNumbers = new();

        //run when mouse hovers over a numeric button
        void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Button btn = null;
            if (sender != null)
            {
                //get the button that is being hovered over
                btn = sender as Button;
            }

            if (btn != null)
            {
                if (placingStreetBet) //if the user is placing a street bet
                {
                    //add the button we are hovering over to the list
                    affectedNumbers.Add(btn);

                    //determine the other buttons that we need to highlight 
                    int offset1 = 1, offset2 = -1;
                    int n = int.Parse(btn.Content.ToString());
                    switch (DetermineRow(n))
                    {                        
                        //if the button is [1, 4, 7...]
                        case 1:
                             offset2=2;
                            break;

                        //if the button is [3, 6, 9...]
                        case 3:

                            offset1=-2;
                            break;
                    }

                    //add the appropriate buttons to the list
                    foreach(Button b in NumericButtons)
                    {
                        if (b.Content.Equals((n + offset1).ToString()) || b.Content.Equals((n + offset2).ToString()))
                        {
                            affectedNumbers.Add(b);

                        }
                        //break the loop if we have all 3 buttons
                        if(affectedNumbers.Count ==3)
                        {
                            break;
                        }
                    }
                }
                else if (placingLineBet) //if the user is placing a line bet
                {
                    //get the number of the button that has been clicked
                    int n = int.Parse(btn.Content.ToString());
                    int xa = -1, xb = 1, yOffset = 3;
                    //determine which row we are in and grab all the relevent numbers
                    switch (DetermineRow(n))
                    {
                        case 1: xa = 2; break;
                        case 3: xb = -2; break;
                    }

                    //if we are in the last column, grab the numbers to the left
                    if (n > 33) { yOffset = -3; }

                    int[] betnumbers = { n, n + xa, n + xb, n + yOffset, n + xa + yOffset, n + xb + yOffset };

                    //add the appropriate buttons to the list
                    foreach (Button b in NumericButtons)
                    {
                        if (betnumbers.Contains(int.Parse(b.Content.ToString())))
                        {
                            affectedNumbers.Add(b);
                        }
                        //break the loop if we have all 6 buttons
                        if (affectedNumbers.Count == 6)
                        {
                            break;
                        }
                    }
                }
                else if (placingCornerBet) //if the user is placing a corner bet
                {
                    int n = int.Parse(btn.Content.ToString());
                    
                    //determine the numbers that we need to add to the list

                    //for this bet, the default behaviour will be to grab the number immediately below and the numbers to the right of the number 
                    //  eg. [x] [y] []
                    //      [y] [y] []
                    //      [ ] [ ] []
                    int xOffset = -1, yOffset = 3;

                    //if the selected number is in the bottom row, grab the number above and the corresponding numbers to the right
                    //  eg. [ ] [ ] []
                    //      [y] [y] []
                    //      [x] [y] []
                    if (DetermineRow(n) == 1) { xOffset = 1; }

                    //if the button is in the last column, grab the numbers to the left
                    //  eg. [ ] [y] [x]
                    //      [ ] [y] [y]
                    //      [ ] [ ] [ ]
                    if (n > 33) { yOffset = -3; }

                    int[] betnumbers = { n, n + xOffset, n + yOffset, n + xOffset + yOffset };

                    //add the appropriate buttons to the list
                    foreach (Button b in NumericButtons)
                    {
                        if (betnumbers.Contains(int.Parse(b.Content.ToString())))
                        {
                            affectedNumbers.Add(b);
                        }
                        //break the loop if we have all 4 buttons
                        if (affectedNumbers.Count == 4)
                        {
                            break;
                        }
                    }

                }
                else //if we only need to highlight one number
                {
                    affectedNumbers.Add(btn);
                }

                //highlight the appropriate numbers
                foreach(Button b in affectedNumbers)
                {
                    b.Background = Brushes.Gold;
                }

            }

        }

        //runs when mouse stops hovering over a numeric button
        void  Button_MouseLeave(object sender, MouseEventArgs e)
        {
            //reset the buttons to their original background colours
            foreach (Button btn in affectedNumbers)
            {
                //the buttons only need to be reset to red or black as those are the only numbers that a player can place multiple bets on
                if (reds.Contains(sbyte.Parse(btn.Content.ToString())))
                {
                    btn.Background = Brushes.Red;
                }
                else
                {
                    btn.Background = Brushes.Black;
                }
            }
            affectedNumbers.Clear();
        }

        //-------------MODE SWITCHES-------------------------  

        //function that is called whenever the split bet button is clicked, split bets contain 2 numbers
        private void Split_Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            //flip the state of the split bet flag
            placingSplitBet = !placingSplitBet;
            waitingForSecondClick = false;    
            
            DisableNonNumericButtons(sender, placingSplitBet);

        }

        //funciton that is called when the Street Bet button is clicked, Street bets contain 3 numbers
        private void Street_Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            //flip the placing street bet tag, this allows the player to back out of the street bet mode if they click the button again
            placingStreetBet = !placingStreetBet;

            DisableNonNumericButtons(sender, placingStreetBet);

        }

        //corner bets consist of 4 numbers
        private void Corner_Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            //flip the placing corner bet tag, this allows the player to back out of the mode if they click the button again
           placingCornerBet = !placingCornerBet;

            DisableNonNumericButtons(sender, placingCornerBet);
        }

        //line bets are 6 numbers -- select the top left number and highlight the other 5, if we are on the last column we need to only highlight the last 6 numbers
        private void Line_Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            //flip the placing street bet tag, this allows the player to back out of the street bet mode if they click the button again
            placingLineBet = !placingLineBet;

            DisableNonNumericButtons(sender,placingLineBet);
        }

        //-------------HELPER FUNCTIONS-------------------------------------

        public string ConstructString(int[] input)
        {
            string result = "";

            if (input.Length == 4) { result += "CORNER("; }
            else if (input.Length == 6) { result += "LINE("; }
            
            foreach (int i in input.OrderDescending().Reverse())
            {
                result += i;
                if (i != input.OrderDescending().Reverse().Last()) result += ", ";
            }
            result += ")";

            return result;
        }

        /// <summary>
        /// Disables non numeric numbers if the player is placing a bet on multiple numbers
        /// </summary>
        /// <param name="sender"> The button that is being pressed - this is to ensure we keep this button active so we can back out of the alternative betting mode</param>
        public void DisableNonNumericButtons(object sender, bool flag)
                {
                    foreach (Button b in AllButtons)
                    {
                        //skip over the clicked button as we want to be able to exit this mode
                        if (b == sender) { continue; }

                        //if the content of the button is non numeric
                        if (!int.TryParse(b.Content.ToString(), out _))
                        {
                            //disable the button
                            b.IsEnabled = !flag;
                        }
                    }
                }

        public void EnableNonNumericButtons()
        {
            foreach (Button b in AllButtons)
            {
                if (!b.IsEnabled) { b.IsEnabled = true; }
            }
        }

        /// This method returns a string of the bets that have won separated by a comma eg. "21, RED, ODD...etc."
        public string CalculateWinningBets(int input)
        {
            //number
            string result = "";

            //format -1 as 00
            if (input == -1)
            {
                result = "00";
            }
            else
            {
                result = input.ToString();
            }

            //first five, the first five values are 00, 0, 1, 2 and 3
            if (input < 4)
            {
                result += ", First Five";
            }

            //if the number is not 0 or 00, add the other bets that can be paid out
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
                if (input > 18) { result += ", 19 to 36"; } else { result += ", 1 to 18"; }

                //row    
                switch (DetermineRow(input))
                {
                    case 1: //first column

                        result += ", 1st Column";
                        break;

                    case 2: //second column

                        result += ", 2nd Column";
                        break;

                    case 3://third column

                        result += ", 3rd Column";
                        break;
                }

                //dozens
                if (input > 23) { result += ", 3rd 12"; } else if (input < 13 && input > 0) { result += ", 1st 12"; } else { result += ", 2nd 12"; }
            }

            return result;

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

        //helper function to pull numeric values out of multi number bets
        private int[] extractNumericValues(string input)
        {
            int[] values;

            //remove the closing bracket, trim everything upto and including the first bracket, and trim the whitespace
            input = input.Replace(")", "").Substring(input.IndexOf("(")+1).Trim(); 

            //split the string by the commas and parse it to an array of integers
            values = Array.ConvertAll(input.Split(","), int.Parse);

            return values;

        }

        //this function returns the valid second numbers when making a split bet
        private void DetermineAcceptableNumbers(int number)
        {
            acceptableNumbers.Clear();
            //if the number is 0 or 00
            if (number <= 0)
            {
                acceptableNumbers.Append(1).Append(2).Append(3);
            }

            switch (DetermineRow(number))
                {
                case 1:     //if the number is in the bottom row
            
                    //add the number above
                    acceptableNumbers.Add(number + 1);
                    //if the number is not in the first column, add the number from the preceding column
                    if (number > 3) { acceptableNumbers.Add(number - 3); }

                    //if we are not in the last column, add the number from the following column
                    if (number < 34) { acceptableNumbers.Add(number + 3); }
            
                break;
   
                
                case 2:     //if the number is on the middle row
            
                  //add the numbers above and below the current number
                     acceptableNumbers.Add(number - 1);
                    acceptableNumbers.Add(number + 1);

                 //if the number is not in the first column, add the number from the preceding column
                    if (number > 3) { acceptableNumbers.Add(number - 3); }

                 //if we are not in the last column, add the number from the following column
                 if (number < 34) { acceptableNumbers.Add(number + 3); }

                break;
      
                case 3:     //if the number is in the top row
            
                   //add the number below it
                   acceptableNumbers.Add(number - 1);

                    //if the number is not in the first column, add the number from the preceding column
                    if (number > 3) { acceptableNumbers.Add(number - 3); }

                    //if we are not in the last column, add the number from the following column
                     if (number < 34) { acceptableNumbers.Add(number + 3); }

                break;
          }


        }

        //function that returns the row that the number is situated in
        private int DetermineRow(int number)
        {
            int row = 0;

            switch(number % 3)
            {
                case 0:
                    row = 3;
                    break;
                
                case 1:
                    row = 1;
                    break;
                
                case 2:
                    row = 2;
                    break;
            }

            return row;
        }

        /// <summary>
        /// function that cycles through the bets made and pays out if the bet has won
        /// </summary>
        public void PayoutBets()
        {
            int winnings = 0;

            //for each bet on the list
            foreach (KeyValuePair<string, int> d in Bets)
            {
                //d.Key = the bet placed in the Bets Dict
                if (winningBets.Contains(d.Key))
                {
                    //if we hit the winning number - pays 35 to 1 - $1 bet returns $36 including initial bet
                    if (d.Key == winningNumber.ToString())
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

            chips += winnings; //add the player's winnings to their chips
            refreshUI(); //refresh the UI so the player can see how much money they have before spinning again

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
                //game over state for when the player runs out of chips
                if (chips == 0)
                {
                    MessageBox.Show("You have run out of chips", "GAME OVER");
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


        }
        
        public string MakeBetHumanReadable(sbyte[] input)
        {
            string result = "";

            //determine the kind of bet by the length
            switch (input.Length)
            {
                case 2: result += "Split ("; break;
                case 3: result += "Street ("; break;
                case 4: result += "Corner ("; break;
                case 6: result += "Line ("; break;
            }
            
            foreach (sbyte b in input)
            {
                result += b.ToString();
                if(b!= input.Last<sbyte>())
                {
                    result += ", ";
                }
            }

            result += ")";
            return result;    
        }
        public string MakeBetHumanReadable(sbyte input)
        {
            string result = "";
            
            switch (input)
            {
                case -1: result = "00";  break;
                case <38: result = input.ToString(); break;
                case 40: result = "Black"; break;
                case 41: result = "Red"; break;
                case 42: result = "Even"; break;
                case 43: result = "Odd"; break;
                case 44: result = "1 to 18"; break;
                case 45: result = "19 to 36"; break;
                case 46: result = "1st Column"; break;
                case 47: result = "2nd Column"; break;
                case 48: result = "3rd Column"; break;
                case 49: result = "1st 12"; break;
                case 50: result = "2nd 12"; break;
                case 51: result = "3rd 12"; break;
            }   
            
            return result;
        
        }

    }
}