using System.Collections;
using System.Text;
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
///     create a window for the betting UI
///     
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
        static string[] doublepayoutbets = { "Red", "Black", "Even", "Odd", "First Half" ,"Second Half" };

        //bets that payout 2-1, so return triple when you win
        static string[] triplepayoutbets = { "First Column", "Second Column","Third Column", "First Dozen", "Second Dozen","Third Dozen"};


        
        //the number of chips that the player has
        int chips = 0;

        string winningBets = "";
        //  dozens
        //  rows
        //  colours
        //  odd / even
        //  columns


        public MainWindow()
        {
            InitializeComponent();
            chips = 1500;
            List<Button> hoverButtons = new List<Button> { Btn1, Btn2, Btn3, Btn4, Btn5, Btn6, Btn7, Btn8, Btn9, Btn10, Btn11, Btn12, Btn13, Btn14, Btn15, Btn16, Btn17, Btn18, Btn19, Btn20, Btn21, Btn22, Btn23, Btn24, Btn25, Btn26, Btn27, Btn28, Btn29, Btn30, Btn31, Btn32, Btn33, Btn34, Btn35, Btn36 };
          

        }



        private void Spin_Click(object sender, RoutedEventArgs e)
        {
            winningBets = "";
            //figure out what the result is going to be
            short result = (short)rng.Next(-1,36);
            
           

        }


        public string GetRouletteColour(int input)
        {
            if (input <= 0) { return "Green"; }
            else if (reds.Contains(input)) { return "Red"; }
            else { return "Black"; }      
            
        }

       public string CalculateWinningBets(short input)
        {
            bool even = input % 2 == 0;


            string result = input.ToString();

            result  += ", " + GetRouletteColour(input);



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

            

            //determine which dozen the number is in
            
            
                result += ", ";

                if (input <= 12) { result += "First Dozen"; }
                else if (input <= 24) { result += "Second Dozen"; }
                else if (input <= 36) { result += "Third Dozen"; }
            
                
                return result;
        }

        public void PayoutBets()
        {
            int winnings = 0;
         
         

            foreach(KeyValuePair<string, int> d in Bets)
            {
                //if there is a bet on and it is contained in the winning string combination
                if (winningBets.Contains(d.Key))
                {


                    //if we hit a number - pays 35 to 1 - £1 bet returns £36 including initial bet
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
                    else if (d.Key.Contains("Split") && d.Key.Contains(winningBets.Substring(0, 2)))
                    {
                        winnings += d.Value * 18;
                    }
                    //three numbers - street bets - payout 12x
                    else if (d.Key.Contains("Street"))
                    {
                        winnings+=d.Value *12;
                    }


                    //four numbers - corner bet - payout 9x
                    else if (d.Key.Contains("Corner"))
                    {
                        winnings += d.Value * 9;
                    }
                
                    //six number bets - line bets - payout 6x
                    else if (d.Key.Contains("Line"))
                    {
                        winnings += d.Value * 6;
                    }
                }
            }

            chips=+ winnings;
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
        public void SplitBetClicked(object sender, RoutedEventArgs e)
        {


        }

        

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {

        }

        //line bets are 6 numbers -- select the top left number and highlight the other 5, if we are on the last column we need to only highlight the last 6 numbers
        private void Line_Bet_Button_Click(object sender, RoutedEventArgs e)
        {
            //add a hover handler to each button
          
            foreach (var btn in hoverButtons)
            {
                // Detach first to avoid duplicate handlers if clicked multiple times
                btn.MouseEnter -= Button_MouseEnter;
                btn.MouseLeave -= Button_MouseLeave;

                // Attach hover event handlers
                btn.MouseEnter += Button_MouseEnter;
                btn.MouseLeave += Button_MouseLeave;
            }
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = Brushes.LightSkyBlue; // Highlight color
                
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                int number =int.Parse(btn.Content.ToString());
                if(reds.Contains(number))
                {
                    btn.Background = Brushes.Red;
                }
                else
                {
                    btn.Background = Brushes.Black;
                }
            }
        }



    }
}