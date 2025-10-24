using MovilApp.ViewModels.Login;
namespace MovilApp.Views.Login;

public partial class SigInView : ContentPage
{
	public SigInView()
	{
		InitializeComponent();
	}
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is SigInViewModel vm && vm.VolverCommand.CanExecute(null))
        {
            vm.VolverCommand.Execute(null);
            return true; // Indica que ya manejaste el evento
        }
        return base.OnBackButtonPressed();
    }
}