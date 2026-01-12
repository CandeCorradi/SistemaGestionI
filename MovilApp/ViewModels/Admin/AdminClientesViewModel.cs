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

namespace MovilApp.ViewModels.Admin
{
    partial class AdminClientesViewModel : ObservableObject
    {
        GenericService<Cliente> _clienteService = new();

        [ObservableProperty]
        private ObservableCollection<Cliente> clientes;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private Cliente clienteCurrent;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string filterText;

        public IRelayCommand AddCommand { get; }
        public IRelayCommand EditCommand { get; }
        public IRelayCommand DeleteCommand { get; }
        public IRelayCommand RefreshCommand { get; }

        public AdminClientesViewModel() //constructor, me trae la lista de clientes
        {
            _ = LoadClientes();
            AddCommand = new AsyncRelayCommand(AddCliente);
            EditCommand = new AsyncRelayCommand(EditCliente, CanEditCliente);
            DeleteCommand = new AsyncRelayCommand(DeleteCliente, CanDeleteCliente);
            RefreshCommand = new AsyncRelayCommand(LoadClientes);
        }

        private bool CanDeleteCliente()
        {
            return clienteCurrent != null;
        }

        private async Task DeleteCliente()
        {
            throw new NotImplementedException();
        }

        private bool CanEditCliente()
        {
            return clienteCurrent != null;
        }

        private async Task EditCliente()
        {
            throw new NotImplementedException();
        }

        private async Task AddCliente()
        {
            throw new NotImplementedException();
        }

        private async Task LoadClientes()
        {
            IsRefreshing = true;
            var clientes = await _clienteService.GetAllAsync();
            Clientes = clientes != null ? new ObservableCollection<Cliente>(clientes) : new ObservableCollection<Cliente>();
            IsRefreshing = false;
        }
    }
}
