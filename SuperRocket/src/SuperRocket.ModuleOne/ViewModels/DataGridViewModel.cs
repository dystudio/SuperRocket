﻿using System.Collections.ObjectModel;
using System.Linq;
using Prism.Mvvm;
using SuperRocket.Core.Model;
using SuperRocket.Core.Services;
using SuperRocket.ModuleOne.Services;
using CefSharp.Wpf;

namespace SuperRocket.ModuleOne.ViewModels
{
    public class DataGridViewModel : BindableBase
    {
        private ObservableCollection<Customer> customers;
        public ChromiumWebBrowser Browser { get; set; }
        public ObservableCollection<Customer> Customers
        {
            get { return customers; }
            set { SetProperty(ref customers, value); }
        }

        public DataGridViewModel(
            ICustomerService service,
            IBrowserManager manager)
        {
            Customers = new ObservableCollection<Customer>();
            Customers.AddRange(service.GetAllCustomers().OrderBy(c => c.FirstName));

            Browser = manager.CreateBrowser();
        }
    }
}
