using MovilApp.ViewModels;
using MovilApp.Views.Login;

namespace MovilApp.Views;

public partial class SistemaDeGestionShell : Shell
{
    public SistemaDeGestionShellViewModel ViewModel => (SistemaDeGestionShellViewModel)BindingContext;

    public SistemaDeGestionShell()
    {
        InitializeComponent();
    }

    public void SetLoginState(bool isLoggedIn)
    {
        ViewModel.SetLoginState(isLoggedIn);
    }

}