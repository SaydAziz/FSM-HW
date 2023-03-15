using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VendingMachine_FSM
{
    public partial class MainWindow : Window
    {
        private enum States
        {
            Locked,
            PayNone = 25,
            PayGum = 50,
            PayGranola = 75,
            Vend = 1001
        }

        List<string> Actions = new List<string>(new string[] {"Pay", "Cancel", "Buy"});
        private List<string> Outputs = new List<string>(new string[] {"Nothing", "Gum", "Granola", "Quarter", "Fifty Cents", "Seventy Five Cents"});

        private States CurrentState;
        private string Output;

        private List<Snack> inventory;


        private Dictionary<KeyValuePair<States, string>, KeyValuePair<States, string>> StateTransitions;

        public MainWindow()
        {
            InitializeComponent();
            UpdateScreen();

            //List to itterate through singletons based on state
            inventory = new List<Snack>();
            inventory.Add(GranolaStock.Instance());
            inventory.Add(GumStock.Instance());

            StateTransitions = PopulateTransitions();
        }


        //Dictionary of Transitions to reference for state changes based on input
        Dictionary<KeyValuePair<States, string>, KeyValuePair<States, string>> PopulateTransitions()
        {
            Dictionary<KeyValuePair<States, string>, KeyValuePair<States, string>> TransDict = new Dictionary<KeyValuePair<States, string>, KeyValuePair<States, string>>();

            TransDict.Add(new KeyValuePair<States, string>(States.Locked, Actions[0]), new KeyValuePair<States, string>(States.PayNone, Outputs[0]));
            TransDict.Add(new KeyValuePair<States, string>(States.Locked, Actions[1]), new KeyValuePair<States, string>(States.Locked, Outputs[0]));
            TransDict.Add(new KeyValuePair<States, string>(States.Locked, Actions[2]), new KeyValuePair<States, string>(States.Locked, Outputs[0]));

            TransDict.Add(new KeyValuePair<States, string>(States.PayNone, Actions[0]), new KeyValuePair<States, string>(States.PayGum, Outputs[0]));
            TransDict.Add(new KeyValuePair<States, string>(States.PayNone, Actions[1]), new KeyValuePair<States, string>(States.Locked, Outputs[3]));
            TransDict.Add(new KeyValuePair<States, string>(States.PayNone, Actions[2]), new KeyValuePair<States, string>(States.PayNone, Outputs[0]));

            TransDict.Add(new KeyValuePair<States, string>(States.PayGum, Actions[0]), new KeyValuePair<States, string>(States.PayGranola, Outputs[0]));
            TransDict.Add(new KeyValuePair<States, string>(States.PayGum, Actions[1]), new KeyValuePair<States, string>(States.Locked, Outputs[4]));
            TransDict.Add(new KeyValuePair<States, string>(States.PayGum, Actions[2]), new KeyValuePair<States, string>(States.Vend, Outputs[1]));

            TransDict.Add(new KeyValuePair<States, string>(States.PayGranola, Actions[0]), new KeyValuePair<States, string>(States.PayGranola, Outputs[3]));
            TransDict.Add(new KeyValuePair<States, string>(States.PayGranola, Actions[1]), new KeyValuePair<States, string>(States.Locked, Outputs[5]));
            TransDict.Add(new KeyValuePair<States, string>(States.PayGranola, Actions[2]), new KeyValuePair<States, string>(States.Vend, Outputs[2]));

            TransDict.Add(new KeyValuePair<States, string>(States.Vend, Actions[0]), new KeyValuePair<States, string>(States.Vend, Outputs[3]));
            TransDict.Add(new KeyValuePair<States, string>(States.Vend, Actions[1]), new KeyValuePair<States, string>(States.Locked, Outputs[0]));
            TransDict.Add(new KeyValuePair<States, string>(States.Vend, Actions[2]), new KeyValuePair<States, string>(States.Locked, Outputs[0]));

            return TransDict;
        }



        //Change state based on input
        void ChangeState(string Action)
        {
            //Cache current state before changing to compare
            States prevState = CurrentState;

            KeyValuePair<States, string> Transition;            

            //Check to see if item or money is available should the inputs be buy or pay
            if ((int)prevState < 1000)
            {
                if (Action == "Buy")
                {
                    foreach (Snack item in inventory)
                    {
                        if (item.id == (int)prevState)
                        {
                            if (item.stock == 0)
                            {
                                Action = "Cancel";  //Substitute the buy action to cancel since there is nothing in stock
                            }
                        }
                    }
                }
                else if (Action == "Pay")
                {
                    if (Quarter.Instance().count == 0)
                    {
                        return;
                    }
                    else
                    {
                        Quarter.Instance().ChangeCount(-1);
                    }
                }
                
            }


            //Calling actual state change
            StateTransitions.TryGetValue(new KeyValuePair<States, string>(CurrentState, Action), out Transition);

            CurrentState = Transition.Key;
            Output = Transition.Value;


            //If the item is vending check which item it is based on previous state
            if ((int)CurrentState == 1001)
            {
                foreach (Snack item in inventory)
                {
                    if (item.id == (int)prevState)
                    {
                        item.TakeSnack();
                    }
                }
            }


            //Alter money count based on state output
            if (Output == "Quarter")
            {
                Quarter.Instance().ChangeCount(1);
            }
            else if (Output == "Fifty Cents")
            {
                Quarter.Instance().ChangeCount(2);
            }
            else if (Output == "Seventy Five Cents")
            {
                Quarter.Instance().ChangeCount(3);
            }

            UpdateScreen();
        }

        void UpdateScreen()
        {
            this.lblState.Content = "Current State: " + CurrentState.ToString();
            this.lblOutput.Content = "Output: " + Output;
            this.lblGum.Content = "Gum (" + GumStock.Instance().stock + ")";
            this.lblGranola.Content = "Granola (" + GranolaStock.Instance().stock + ")";
            this.lblWallet.Content = "$ (" + Quarter.Instance().count + " Q's)";


            if ((int)CurrentState < 1000)
            {
                this.lblMoney.Content = "0." + (int)CurrentState;
            }
            else if ((int)CurrentState > 1000)
            {
                this.lblMoney.Content = "0.0";
            }


        }

        private void btnBuy_Click(object sender, RoutedEventArgs e)
        {
            if ((int)CurrentState != 1001) //if the vending machine has not vended allow button click
            {
                ChangeState((e.Source as Button).Content.ToString());
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ChangeState((e.Source as Button).Content.ToString());
        }

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            if ((int)CurrentState != 1001)
            {
                ChangeState((e.Source as Button).Content.ToString());
            }
        }
    }  



    class Quarter
    {
        static Quarter instance;
        public int count { get; protected set; }
        protected Quarter()
        {
            count = 8;
        }

        public static Quarter Instance()
        {
            if (instance == null)
            {
                instance = new Quarter();
            }

            return instance;
        }

        public void ChangeCount(int amount)
        {
            count += amount;
        }
    }

    abstract class Snack
    {
        public int id { get; protected set; }
        public int stock { get; protected set; }

        public void TakeSnack()
        {
            stock--;
        }
    }

    class GranolaStock : Snack
    {
        static GranolaStock instance;
        protected GranolaStock()
        {
            stock = 2;
            id = 75;
        }

        public static GranolaStock Instance()
        {
            if (instance == null)
            {
                instance = new GranolaStock();
            }

            return instance;
        }

       
    }

    class GumStock : Snack
    {
        static GumStock instance;
        protected GumStock()
        {
            stock = 2;
            id = 50;
        }

        public static GumStock Instance()
        {
            if (instance == null)
            {
                instance = new GumStock();
            }

            return instance;
        }
    }

}
