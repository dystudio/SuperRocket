using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;
using SuperRocket.Core.Model;
using SuperRocket.Core.Services;
using SuperRocket.ModuleOne.Services;
using CefSharp.Wpf;
using System.Windows.Input;
using FirstFloor.ModernUI.Presentation;
using System;

namespace SuperRocket.ModuleOne.ViewModels
{
    public class ChromiumViewModel : BindableBase
    {
        private ObservableCollection<Customer> customers;
        private string address;
        public string Address
        {
            get { return address; }
            set { SetProperty(ref address, value); }
        }


        private string title;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        private IWpfWebBrowser webBrowser;
        public IWpfWebBrowser WebBrowser
        {
            get { return webBrowser; }
            set { SetProperty(ref webBrowser, value); }
        }
       
        public ObservableCollection<Customer> Customers
        {
            get { return customers; }
            set { SetProperty(ref customers, value); }
        }

        public ChromiumViewModel(
            ICustomerService service,
            IBrowserManager manager)
        {
            Customers = new ObservableCollection<Customer>();
            Customers.AddRange(service.GetAllCustomers().OrderBy(c => c.FirstName));

            WebBrowser = manager.CreateBrowser();

            Address = "local://Resource/Modules/Example/Default.html";
            Title = "This is a module for Super Rocket";
        }


        private void Go()
        {
            Address = "";
            // Part of the Focus hack further described in the OnPropertyChanged() method...
            Keyboard.ClearFocus();
        }
    }
}
