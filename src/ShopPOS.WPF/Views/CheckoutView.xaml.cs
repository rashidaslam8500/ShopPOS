using System.Windows.Controls;

using System.Windows.Input;

using ShopPOS.WPF.ViewModels;



namespace ShopPOS.WPF.Views;



public partial class CheckoutView : UserControl

{

    public CheckoutView()

    {

        InitializeComponent();

        Loaded += (_, _) => FocusLookupBox();

        IsVisibleChanged += (_, e) =>

        {

            if (e.NewValue is true)

                FocusLookupBox();

        };

    }



    private CheckoutViewModel? Vm => DataContext as CheckoutViewModel;



    private void FocusLookupBox()

    {

        ItemLookupBox.Focus();

        if (ItemLookupBox.Template?.FindName("PART_EditableTextBox", ItemLookupBox) is TextBox inner)

            inner.SelectAll();

    }



    private void ItemLookupBox_PreviewKeyDown(object sender, KeyEventArgs e)

    {

        if (Vm is null)

            return;



        if (e.Key == Key.Enter)

        {

            Vm.SubmitSearchCommand.Execute(null);

            FocusLookupBox();

            e.Handled = true;

        }

        else if (e.Key == Key.Escape)

        {

            Vm.ClearSearch();

            e.Handled = true;

        }

    }



    private void ItemLookupBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)

    {

        if (Vm is null)

            return;



        if (!string.IsNullOrWhiteSpace(Vm.SearchQuery) && Vm.SearchSuggestions.Count > 0)

            Vm.IsDropDownOpen = true;

    }

}

