using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LizardClient.Launcher
{
    public partial class App : Application
    {
        /// <summary>
        /// 在应用启动时，自动根据已定义的 Color.* 生成对应的 Brush.* 资源，
        /// 保证 XAML 中使用 {StaticResource Brush.*} 时不会因遗漏而抛出 XamlParseException。
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            // 先调用基类以加载 Application.Resources 和合并字典
            base.OnStartup(e);

            // 为每个资源字典生成 Brush 映射
            GenerateBrushMappingsFromResources(this.Resources);

            // 手动创建并显示主窗口（App.xaml 不再使用 StartupUri）
            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }

        private void GenerateBrushMappingsFromResources(ResourceDictionary root)
        {
            if (root == null) return;

            // 先在当前字典生成，然后递归处理合并字典，确保局部字典优先
            CreateBrushesInDictionary(root);

            foreach (var md in root.MergedDictionaries.ToList())
            {
                GenerateBrushMappingsFromResources(md);
            }
        }

        private void CreateBrushesInDictionary(ResourceDictionary rd)
        {
            if (rd == null) return;

            var keys = rd.Keys.Cast<object>().ToList();

            foreach (var rawKey in keys)
            {
                if (rawKey is not string key) continue;

                if (!key.StartsWith("Color.", StringComparison.Ordinal)) continue;

                var brushKey = "Brush." + key.Substring("Color.".Length);

                // 如果当前字典或应用资源已经包含对应的 Brush，则跳过生成
                if (rd.Contains(brushKey) || Application.Current.Resources.Contains(brushKey)) continue;

                var value = rd[key];

                if (value is Color colorValue)
                {
                    var brush = new SolidColorBrush(colorValue);
                    if (brush.CanFreeze) brush.Freeze();
                    rd[brushKey] = brush;
                }
                else if (value is SolidColorBrush scb)
                {
                    var brush = scb;
                    if (!brush.IsFrozen && brush.CanFreeze) brush.Freeze();
                    rd[brushKey] = brush;
                }
            }
        }
    }
}

