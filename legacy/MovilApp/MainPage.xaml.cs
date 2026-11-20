namespace MovilApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
                
        private async void OnVerClientesClicked(object sender, EventArgs e)
        {
            try
            {                
                await Shell.Current.GoToAsync("//ListaClientes");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo navegar a la lista: " + ex.Message, "Ok");
            }
        }
    }
}
