using Syncfusion.TreeView.Engine;
using Syncfusion.XForms.TreeView;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace DiagnosticNP.Behaviors
{
    public class TreeViewBehavior : Behavior<SfTreeView>
    {
        public static readonly BindableProperty ExpandAllProperty =
            BindableProperty.Create(nameof(ExpandAll), typeof(bool), typeof(TreeViewBehavior), false,
                propertyChanged: OnExpandAllChanged);

        public static readonly BindableProperty CollapseAllProperty =
            BindableProperty.Create(nameof(CollapseAll), typeof(bool), typeof(TreeViewBehavior), false,
                propertyChanged: OnCollapseAllChanged);

        public static readonly BindableProperty ExpandToNodeProperty =
            BindableProperty.Create(nameof(ExpandToNode), typeof(object), typeof(TreeViewBehavior), null,
                propertyChanged: OnExpandToNodeChanged);

        public bool ExpandAll
        {
            get => (bool)GetValue(ExpandAllProperty);
            set => SetValue(ExpandAllProperty, value);
        }

        public bool CollapseAll
        {
            get => (bool)GetValue(CollapseAllProperty);
            set => SetValue(CollapseAllProperty, value);
        }

        public object ExpandToNode
        {
            get => GetValue(ExpandToNodeProperty);
            set => SetValue(ExpandToNodeProperty, value);
        }

        public SfTreeView TreeView { get; private set; }

        protected override void OnAttachedTo(SfTreeView treeView)
        {
            base.OnAttachedTo(treeView);
            TreeView = treeView;

            if (treeView.BindingContext != null)
            {
                BindingContext = treeView.BindingContext;
            }

            treeView.BindingContextChanged += OnTreeViewBindingContextChanged;
        }

        protected override void OnDetachingFrom(SfTreeView treeView)
        {
            treeView.BindingContextChanged -= OnTreeViewBindingContextChanged;
            TreeView = null;
            base.OnDetachingFrom(treeView);
        }

        private void OnTreeViewBindingContextChanged(object sender, System.EventArgs e)
        {
            BindingContext = TreeView?.BindingContext;
        }

        private static void OnExpandAllChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TreeViewBehavior behavior && behavior.TreeView != null && (bool)newValue)
            {
                behavior.TreeView.ExpandAll();
                Device.BeginInvokeOnMainThread(() => behavior.ExpandAll = false);
            }
        }

        private static void OnCollapseAllChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TreeViewBehavior behavior && behavior.TreeView != null && (bool)newValue)
            {
                behavior.TreeView.CollapseAll();
                Device.BeginInvokeOnMainThread(() => behavior.CollapseAll = false);
            }
        }

        private static void OnExpandToNodeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TreeViewBehavior behavior && behavior.TreeView != null && newValue != null)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    behavior.ExpandToSpecificNode(newValue);
                });
            }
        }

        private void ExpandToSpecificNode(object node)
        {
            if (TreeView == null || node == null) return;

            try
            {
                // Сначала сворачиваем все узлы
                TreeView.CollapseAll();

                // Находим узел в дереве
                var allNodes = GetAllTreeViewNodes(TreeView.Nodes);
                var treeViewNode = allNodes.FirstOrDefault(n => n.Content == node);

                if (treeViewNode != null)
                {
                    // Разворачиваем только путь к этому узлу (всех родителей)
                    ExpandNodePath(treeViewNode);

                    // Прокручиваем к узлу
                    TreeView.BringIntoView(treeViewNode);
                }

                // Сбрасываем значение
                Device.BeginInvokeOnMainThread(() => ExpandToNode = null);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error expanding node: {ex.Message}");
            }
        }

        private void ExpandNodePath(TreeViewNode node)
        {
            if (node == null) return;

            // Разворачиваем всех родителей
            var parent = node.ParentNode;
            while (parent != null)
            {
                parent.IsExpanded = true;
                parent = parent.ParentNode;
            }
        }

        private List<TreeViewNode> GetAllTreeViewNodes(TreeViewNodeCollection nodes)
        {
            var result = new List<TreeViewNode>();

            foreach (TreeViewNode node in nodes)
            {
                result.Add(node);
                if (node.ChildNodes != null && node.ChildNodes.Count > 0)
                {
                    result.AddRange(GetAllTreeViewNodes(node.ChildNodes));
                }
            }

            return result;
        }
    }
}