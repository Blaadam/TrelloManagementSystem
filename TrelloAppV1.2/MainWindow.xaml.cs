using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Security.Principal;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace TrelloAppV1._2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TrelloAppEntities1 context = new TrelloAppEntities1();
        CollectionViewSource accViewSource;

        AccountTable CurrentAccount;

        public MainWindow()
        {
            InitializeComponent();
            accViewSource = ((CollectionViewSource)(FindResource("accountTableViewSource1")));
            DataContext = this;

            AccountMenu.Visibility = Visibility.Visible;
            NewCardMenu.Visibility = Visibility.Hidden;
        }

        private void LoadAccountsIntoView()
        {
            AccountsList.Children.Clear();
            context.AccountTables.Load();
            accViewSource.Source = context.AccountTables.Local;

            var items = context.AccountTables.Local as IEnumerable<AccountTable>;

            if (items != null)
            {
                foreach (var item in items)
                {
                    var button = new Button
                    {
                        Content = item.Name,
                        Height = 30,
                        ToolTip = item.ToolTip,
                    };
                    button.Click += Button_Click;

                    AccountsList.Children.Add(button);
                }
            }
        }

        private void LoadAccount(string accountName)
        {
            // LINQ query to find the account by name
            CurrentAccount = context.AccountTables.FirstOrDefault(a => a.Name == accountName);

            if (CurrentAccount != null)
            {
                // Assuming SelectedAccount is a UI element that displays the account name
                SelectedAccount.Content = "Current Account: " + CurrentAccount.Name;

                EditAccount_Name.Text = CurrentAccount.Name;
                EditAccount_AppKey.Text = CurrentAccount.AppKey;
                EditAccount_Token.Password = CurrentAccount.Token;
                EditAccount_ToolTip.Text = CurrentAccount.ToolTip;
            }
            else
            {
                // Handle the case where the account is not found
                SelectedAccount.Content = "Account not found";
            }
        }

        private void EditAccount(object sender, RoutedEventArgs e)
        {
            if (CurrentAccount == null) { return; }

            EditAccount_Name.Text = EditAccount_Name.Text.Trim();
            EditAccount_AppKey.Text = EditAccount_AppKey.Text.Trim();
            EditAccount_Token.Password = EditAccount_Token.Password.Trim();
            EditAccount_ToolTip.Text = EditAccount_ToolTip.Text.Trim();

            if (EditAccount_Name.Text == string.Empty || EditAccount_AppKey.Text == string.Empty || EditAccount_Token.Password == string.Empty || EditAccount_ToolTip.Text == string.Empty)
            {
                MessageBox.Show("You cannot leave a field blank.", "Error conducting action");
                return;
            }

            CurrentAccount.Name = EditAccount_Name.Text;
            CurrentAccount.AppKey = EditAccount_AppKey.Text;
            CurrentAccount.Token = EditAccount_Token.Password;
            CurrentAccount.ToolTip = EditAccount_ToolTip.Text;


            LoadAccountsIntoView();
        }

        private void NewAccount(object sender, RoutedEventArgs e)
        {
            NewAccount_Name.Text = NewAccount_Name.Text.Trim();
            NewAccount_AppKey.Text = NewAccount_AppKey.Text.Trim();
            NewAccount_Token.Password = NewAccount_Token.Password.Trim();
            NewAccount_ToolTip.Text = NewAccount_ToolTip.Text.Trim();

            // LINQ query to find the account by name
            CurrentAccount = context.AccountTables.FirstOrDefault(a => a.Name == NewAccount_Name.Text);

            if (CurrentAccount != null)
            {
                MessageBox.Show("An account with this name already exists.");
                return;
            }

            if (NewAccount_Name.Text == string.Empty || NewAccount_AppKey.Text == string.Empty || NewAccount_Token.Password == string.Empty || NewAccount_ToolTip.Text == string.Empty)
            {
                MessageBox.Show("You cannot leave a field blank.");
                return;
            }

            // Create a new instance of the Account entity
            var newAccount = new AccountTable
            {
                Name = NewAccount_Name.Text,
                AppKey = NewAccount_AppKey.Text,
                Token = NewAccount_Token.Password,
                ToolTip = NewAccount_ToolTip.Text
            };

            context.AccountTables.Add(newAccount);
            context.SaveChanges();

            LoadAccountsIntoView();
        }

        private void DeleteAccount(object sender, RoutedEventArgs e)
        {
            if (CurrentAccount == null) { return; }

            MessageBoxResult result = MessageBox.Show(
                "Save changes before closing?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);


            if (result == MessageBoxResult.Yes)
            {
                var account = context.AccountTables.FirstOrDefault(a => a.ID == CurrentAccount.ID);

                if (account != null)
                {
                    context.AccountTables.Remove(account);

                    try
                    {
                        context.SaveChanges();
                        LoadAccountsIntoView();
                        MessageBox.Show("Account Deleted!");
                        SelectedAccount.Content = "Current Account: N/A";
                        CurrentAccount = null;
                    }
                    catch (DbUpdateException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Could not find the account");
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAccountsIntoView();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            LoadAccount((string)button.Content);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string buttonContext = (string)button.Content;
                if (String.Equals(buttonContext, "Account", StringComparison.OrdinalIgnoreCase))
                {
                    AccountMenu.Visibility = Visibility.Visible;
                    NewCardMenu.Visibility = Visibility.Hidden;
                }
                else if (String.Equals(buttonContext, "New Card", StringComparison.OrdinalIgnoreCase))
                {
                    AccountMenu.Visibility = Visibility.Hidden;
                    NewCardMenu.Visibility = Visibility.Visible;
                }
            }
        }

        //// New Card
        private async void NC_CheckID(object sender, RoutedEventArgs e)
        {
            if (CurrentAccount == null)
            {
                MessageBox.Show("You need to select an account before you can interact!");
                return;
            }

            string apiKey = CurrentAccount.AppKey;
            string apiToken = CurrentAccount.Token;
            string listId = NC_ListID.Text.Trim();

            string URL = $"https://api.trello.com/1/lists/{listId}?key={apiKey}&token={apiToken}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(URL);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        // Process the response to check if the account has access
                        // For example, parse the JSON response to check for specific fields or values
                        MessageBox.Show("Access granted to the list.");
                    }
                    else
                    {
                        // Handle error response
                        MessageBox.Show("Error accessing the list.");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private async void NC_CreateCard(object sender, RoutedEventArgs e)
        {
            if (CurrentAccount == null)
            {
                MessageBox.Show("You need to select an account before you can interact!");
                return;
            }

            string apiKey = CurrentAccount.AppKey;
            string apiToken = CurrentAccount.Token;
            string listId = NC_ListID.Text.Trim();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.trello.com/1/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var card = new { name = NC_Name.Text, idList = listId, desc = NC_Desc.Text };
                var jsonContent = JsonConvert.SerializeObject(card);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"cards?key={apiKey}&token={apiToken}", content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Card created successfully!");
                }
                else
                {
                    MessageBox.Show($"Failed to create card: {response.StatusCode}");
                }
            }
        }
    }
}
