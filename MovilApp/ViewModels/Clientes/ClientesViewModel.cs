using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovilApp.ViewModels.Clientes
{
    partial class ClientesViewModel : ObservableObject
    {
        GenericService<Cliente> _clienteService = new();

        [ObservableProperty]
        private string textoBusqueda = string.Empty;

        [ObservableProperty]
        private bool estaDescargando;

        [ObservableProperty]
        private ObservableCollection<Cliente> clientes = new();

        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand LimpiarCommand { get; }

        public ClientesViewModel()
        {
            BuscarCommand = new RelayCommand(OnBuscar);
            LimpiarCommand = new RelayCommand(OnLimpiar);
            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            OnBuscar();
        }

        private async void OnBuscar()
        {
            if (EstaDescargando) return;

            try
            {
                EstaDescargando = true;

                // Obtener todos los clientes si no los tenemos
                var clientes = await _clienteService.GetAllAsync(TextoBusqueda);

                Clientes = clientes != null ?
                        new ObservableCollection<Cliente>(clientes)
                        : new ObservableCollection<Cliente>();
            }
            finally
            {
                EstaDescargando = false;
            }
        }


        private void OnLimpiar()
        {
            TextoBusqueda = string.Empty;
            OnBuscar();
        }
    }
}
