using Lively.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Lively.Views
{
    public partial class DiagnosticMenu : Window
    {
        public DiagnosticMenu()
        {
            InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<DiagnosticViewModel>();
        }
    }
}
