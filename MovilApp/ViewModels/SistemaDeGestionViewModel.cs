using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovilApp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovilApp.ViewModels
{
    public partial class SistemaDeGestionShellViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool userIsLogged = false;

        public IRelayCommand LogoutCommand { get; }
            
        public SistemaDeGestionShellViewModel()
        {
            LogoutCommand = new RelayCommand(OnLogout); //metodo que se llama igual que la clase que no devuelve valores y se ejecuta siempre que la clase se instancia
            SetLoginState(false);//inicialmente el usuario no esta logueado
        }

        public void SetLoginState(bool isLoggedIn)
        {
            if (Application.Current?.MainPage is SistemaDeGestionShell shell)
            {
                if (isLoggedIn)
                    shell.FlyoutBehavior = FlyoutBehavior.Flyout;
                else
                    shell.FlyoutBehavior = FlyoutBehavior.Disabled;

                UserIsLogged = isLoggedIn;
                if (isLoggedIn)
                    shell.GoToAsync("//MainPage");  // Cambio a MainPage (pantalla de inicio)
                else
                    shell.GoToAsync("//Login");
            }

        }

        private void OnLogout()
        {
            SetLoginState(false);
        }
    }
}

