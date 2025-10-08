using DiagnosticNP.Models.Equipment;
using DiagnosticNP.ViewModels;
using Syncfusion.XForms.TreeView;
using Xamarin.Forms;
using System;

namespace DiagnosticNP.Views
{
    public partial class MainPage : TabbedPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is MainViewModel viewModel)
            {
                // Автоматически загружаем структуру при открытии приложения
                await viewModel.InitializeAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (BindingContext is MainViewModel viewModel)
            {
                viewModel.OnDisappearing();
            }
        }

        private void OnFullTreeViewItemTapped(object sender, Syncfusion.XForms.TreeView.ItemTappedEventArgs e)
        {
            if (e.Node?.Content is EquipmentNode node)
            {
                System.Diagnostics.Debug.WriteLine($"Выбран узел: {node.Name}, тип: {node.NodeType}, уровень: {node.Level}");

                // Переключаем состояние раскрытия для узлов с детьми
                if (node.HasChildren)
                {
                    node.IsExpanded = !node.IsExpanded;
                    System.Diagnostics.Debug.WriteLine($"Узел {node.Name} раскрыт: {node.IsExpanded}");
                }

                // Сбрасываем выделение у всех узлов
                ResetSelection(BindingContext as MainViewModel);

                // Устанавливаем выделение для выбранного узла
                node.IsSelected = true;

                if (BindingContext is MainViewModel viewModel)
                {
                    viewModel.SelectedNode = node;
                    System.Diagnostics.Debug.WriteLine($"SelectedNode установлен: {viewModel.SelectedNode?.Name}");
                }
            }
        }

        private void OnFilteredTreeViewItemTapped(object sender, Syncfusion.XForms.TreeView.ItemTappedEventArgs e)
        {
            if (e.Node?.Content is EquipmentNode node)
            {
                System.Diagnostics.Debug.WriteLine($"Выбран отфильтрованный узел: {node.Name}");

                // В отфильтрованном дереве также позволяем переключать раскрытие
                if (node.HasChildren)
                {
                    node.IsExpanded = !node.IsExpanded;
                }

                // Сбрасываем выделение у всех узлов
                ResetSelection(BindingContext as MainViewModel);

                // Устанавливаем выделение для выбранного узла
                node.IsSelected = true;

                if (BindingContext is MainViewModel viewModel)
                {
                    viewModel.SelectedNode = node;
                }
            }
        }

        private void ResetSelection(MainViewModel viewModel)
        {
            if (viewModel == null) return;

            // Сбрасываем выделение во всем дереве оборудования
            if (viewModel.EquipmentTree != null)
            {
                foreach (var node in viewModel.EquipmentTree)
                {
                    ResetSelectionRecursive(node);
                }
            }

            // Сбрасываем выделение в отфильтрованном дереве
            if (viewModel.FilteredEquipmentTree != null)
            {
                foreach (var node in viewModel.FilteredEquipmentTree)
                {
                    ResetSelectionRecursive(node);
                }
            }
        }

        private void ResetSelectionRecursive(EquipmentNode node)
        {
            node.IsSelected = false;
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    ResetSelectionRecursive(child);
                }
            }
        }
    }
}