using Microsoft.Win32;
using NullTweaker.Core;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ellipse = System.Windows.Shapes.Ellipse;
using Shape = System.Windows.Shapes.Shape;
using System.Windows.Threading;

namespace NullTweaker;

public partial class MainWindow : Window
{
    private const double EditorWidth = 280;

    private readonly List<ConfigDocument> _documents = new();
    private ConfigDocument? _current;
    private DocumentKind _category;
    private bool _listing;
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private readonly DispatcherTimer _copiedTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private bool _showPreview = true;

    public MainWindow()
    {
        InitializeComponent();
        _previewTimer.Tick += (_, _) => { _previewTimer.Stop(); Refresh(); };
        _copiedTimer.Tick += (_, _) => { _copiedTimer.Stop(); CopyText.Text = "Copy"; };
        Loaded += (_, _) =>
        {
            string[] paths = Environment.GetCommandLineArgs().Skip(1).ToArray();
            if (paths.Length == 0)
                Add(new(DocumentKind.Weapon));
            else
            foreach (string path in paths)
                Try(() => Add(ConfigDocument.Open(path)));
        };
    }

    private static DocumentKind Category(DocumentKind kind) => kind is DocumentKind.Waves or DocumentKind.Triggers ? DocumentKind.Map : kind;

    private static string CategoryName(DocumentKind category) => category switch
    {
        DocumentKind.Weapon => "Weapons",
        DocumentKind.NPC => "NPCs",
        DocumentKind.Customization => "Customization",
        _ => "Maps"
    };

    private static string KindName(DocumentKind kind) => kind switch
    {
        DocumentKind.Map => "Map settings",
        DocumentKind.Waves => "Survival waves",
        DocumentKind.Triggers => "Trigger volumes",
        _ => kind.ToString()
    };

    private static string Glyph(DocumentKind category) => category switch
    {
        DocumentKind.Weapon => "",
        DocumentKind.NPC => "",
        DocumentKind.Customization => "",
        _ => ""
    };

    private static T Themed<T>(T element, DependencyProperty property, string key) where T : FrameworkElement
    {
        element.SetResourceReference(property, key);
        return element;
    }

    private static TextBlock Secondary(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current.FindResource("Secondary")
    };

    private static TextBlock Symbol(string glyph, double size = 16) => new()
    {
        Text = glyph,
        FontSize = size,
        Style = (Style)Application.Current.FindResource("Icon")
    };

    private void Add(ConfigDocument document)
    {
        ConfigDocument? existing = document.FilePath == null ? null : _documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, document.FilePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            document = existing;
        else
            _documents.Add(document);
        _current = document;
        _category = Category(document.Kind);
        RefreshList();
        Render();
    }

    private void RefreshList()
    {
        _listing = true;
        WeaponsNav.IsChecked = _category == DocumentKind.Weapon;
        MapsNav.IsChecked = _category == DocumentKind.Map;
        NPCNav.IsChecked = _category == DocumentKind.NPC;
        CustomizationNav.IsChecked = _category == DocumentKind.Customization;
        NewText.Text = _category switch
        {
            DocumentKind.Weapon => "New weapon",
            DocumentKind.NPC => "New NPC",
            DocumentKind.Customization => "New customization",
            _ => "New map file..."
        };
        Files.Items.Clear();
        foreach (ConfigDocument doc in _documents.Where(d => Category(d.Kind) == _category))
        {
            ListBoxItem item = new()
            {
                Tag = doc,
                Content = Entry(doc),
                ToolTip = doc.FilePath ?? "Not saved yet"
            };
            Files.Items.Add(item);
            if (doc == _current)
                item.IsSelected = true;
        }
        FilesHeader.Visibility = Files.Items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        _listing = false;
    }

    private static Grid Entry(ConfigDocument doc)
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new());
        grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        StackPanel text = new()
        {
            Margin = new(0, 2, 0, 2)
        };
        text.Children.Add(new TextBlock { Text = doc.Title, TextTrimming = TextTrimming.CharacterEllipsis });
        text.Children.Add(Secondary(doc.FilePath == null ? Schema.FileName(doc.Kind) : Path.GetFileName(Path.GetDirectoryName(doc.FilePath)) + "\\" + Path.GetFileName(doc.FilePath)));
        grid.Children.Add(text);
        if (doc.Dirty)
        {
            Ellipse dot = Themed(new Ellipse
            {
                Width = 7,
                Height = 7,
                Margin = new(8, 0, 2, 0),
                ToolTip = "Unsaved changes"
            }, Shape.FillProperty, "AccentFillColorDefaultBrush");
            Grid.SetColumn(dot, 1);
            grid.Children.Add(dot);
        }
        return grid;
    }

    private void NavChecked(object s, RoutedEventArgs e)
    {
        if (_listing)
            return;
        _category = s == WeaponsNav ? DocumentKind.Weapon : s == MapsNav ? DocumentKind.Map :
            s == CustomizationNav ? DocumentKind.Customization : DocumentKind.NPC;
        _current = _documents.FirstOrDefault(d => Category(d.Kind) == _category);
        RefreshList();
        Render();
    }

    private void FileSelected(object s, SelectionChangedEventArgs e)
    {
        if (_listing || Files.SelectedItem is not ListBoxItem { Tag: ConfigDocument doc })
            return;
        _current = doc;
        Render();
    }

    private void NewClicked(object s, RoutedEventArgs e)
    {
        if (_category != DocumentKind.Map)
        {
            Add(new(_category));
            return;
        }
        ContextMenu menu = new()
        {
            PlacementTarget = (UIElement)s,
            Placement = PlacementMode.Top
        };
        foreach (DocumentKind kind in new[] { DocumentKind.Map, DocumentKind.Waves, DocumentKind.Triggers })
            menu.Items.Add(MenuEntry(KindName(kind) + "  (" + Schema.FileName(kind) + ")", () => Add(new(kind))));
        menu.IsOpen = true;
    }

    private static MenuItem MenuEntry(string header, Action action, string gesture = "")
    {
        MenuItem item = new()
        {
            Header = header,
            InputGestureText = gesture
        };
        item.Click += (_, _) => action();
        return item;
    }

    private void MoreClicked(object s, RoutedEventArgs e)
    {
        ContextMenu menu = new()
        {
            PlacementTarget = (UIElement)s,
            Placement = PlacementMode.Bottom
        };
        menu.Items.Add(MenuEntry("Save to pack folder...", Export));
        menu.Items.Add(MenuEntry("Copy JSON", CopyJson));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuEntry("Close file", CloseCurrent, "Ctrl+W"));
        menu.IsOpen = true;
    }

    private void OpenClicked(object s, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Framework configurations|*.json;*.ini|JSON|*.json|Legacy map INI|*.ini",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true)
            foreach (string path in dialog.FileNames)
                Try(() => Add(ConfigDocument.Open(path, _category == DocumentKind.Map ? DocumentKind.Triggers : null)));
    }

    private void FilesDropped(object s, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return;
        foreach (string file in files)
            Try(() => Add(ConfigDocument.Open(file)));
    }

    private void SaveClicked(object s, RoutedEventArgs e) => Save(false);
    private void SaveAsClicked(object s, RoutedEventArgs e) => Save(true);

    private bool Save(bool saveAs)
    {
        if (_current == null)
            return false;
        ConfigDocument doc = _current;
        string? path = saveAs ? null : doc.FilePath;
        if (path == null)
        {
            SaveFileDialog dialog = new()
            {
                Filter = "JSON configuration|*.json",
                FileName = doc.FilePath == null ? Schema.FileName(doc.Kind) : Path.GetFileName(doc.FilePath),
                DefaultExt = ".json"
            };
            if (dialog.ShowDialog(this) != true)
                return false;
            path = dialog.FileName;
        }
        bool success = Try(() => doc.Save(path));
        if (success)
        {
            RefreshList();
            Refresh();
        }
        return success;
    }

    private void Export()
    {
        if (_current == null)
            return;
        OpenFolderDialog dialog = new()
        {
            Title = "Select the pack folder (only " + Schema.FileName(_current.Kind) + " is written)"
        };
        if (dialog.ShowDialog(this) != true)
            return;
        string path = Path.Combine(dialog.FolderName, Schema.FileName(_current.Kind));
        if (File.Exists(path) && MessageBox.Show(this, path + " already exists.\nReplace it?", "Save to pack folder", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        if (Try(() => _current.Save(path)))
        {
            RefreshList();
            Refresh();
        }
    }

    private void CopyClicked(object s, RoutedEventArgs e) => CopyJson();

    private void CopyJson()
    {
        if (_current == null)
            return;
        if (Try(() => Clipboard.SetText(_current.Json())))
        {
            CopyText.Text = "Copied";
            _copiedTimer.Stop();
            _copiedTimer.Start();
        }
    }

    private void PreviewToggled(object s, RoutedEventArgs e)
    {
        _showPreview = !_showPreview;
        PreviewToggleText.Text = _showPreview ? "Hide JSON" : "Show JSON";
        UpdatePreviewPane();
    }

    private void UpdatePreviewPane() =>
        PreviewPane.Visibility = _current != null && _showPreview ? Visibility.Visible : Visibility.Collapsed;

    private void CloseCurrent()
    {
        if (_current == null || !CanClose(_current))
            return;
        _documents.Remove(_current);
        _current = _documents.FirstOrDefault(d => Category(d.Kind) == _category);
        RefreshList();
        Render();
    }

    private bool CanClose(ConfigDocument doc)
    {
        if (!doc.Dirty)
            return true;
        MessageBoxResult answer = MessageBox.Show(this, "Save changes to " + doc.Title + "?", "NullTweaker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel)
            return false;
        if (answer == MessageBoxResult.No)
            return true;
        _current = doc;
        _category = Category(doc.Kind);
        RefreshList();
        Render();
        return Save(false);
    }

    private void WindowClosing(object? s, CancelEventArgs e)
    {
        foreach (ConfigDocument doc in _documents.ToArray())
            if (!CanClose(doc))
            {
                e.Cancel = true;
                return;
            }
        _previewTimer.Stop();
    }

    private void KeyPressed(object s, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;
        switch (e.Key)
        {
            case Key.S:
                Save((Keyboard.Modifiers & ModifierKeys.Shift) != 0);
                break;
            case Key.O:
                OpenClicked(s, e);
                break;
            case Key.N:
                Add(new(_category));
                break;
            case Key.W:
                CloseCurrent();
                break;
            case Key.F:
                Search.Focus();
                Search.SelectAll();
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or FormatException or System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(this, ex.Message, "NullTweaker", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void Changed()
    {
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void Refresh()
    {
        Banners.Children.Clear();
        UpdatePreviewPane();
        if (_current == null)
        {
            Title = "NullTweaker";
            Preview.Text = "";
            return;
        }
        DocumentTitle.Text = _current.Title;
        DocumentInfo.Text = (_current.FilePath ?? Schema.FileName(_current.Kind) + ", not saved yet") + (_current.Dirty && _current.FilePath != null ? "  ·  unsaved changes" : "");
        Title = _current.Title + (_current.Dirty ? "*" : "") + " - NullTweaker";
        Preview.Text = _current.Json();
        PreviewFile.Text = Schema.FileName(_current.Kind);
        foreach (ListBoxItem item in Files.Items)
            if (item.Tag is ConfigDocument doc)
                item.Content = Entry(doc);
        IReadOnlyList<string> errors = Validator.Check(_current);
        if (errors.Count > 0)
            Banners.Children.Add(Banner(true, errors.Count == 1 ? "Fix this before saving" : $"Fix {errors.Count} problems before saving", errors));
        if (_current.Notices.Count > 0)
            Banners.Children.Add(Banner(false, "Converted from an older format. The original file stays untouched until you save.", _current.Notices));
    }

    private static Border Banner(bool error, string title, IEnumerable<string> lines)
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new());
        TextBlock icon = Themed(Symbol(error ? "" : ""), TextBlock.ForegroundProperty, error ? "SystemFillColorCriticalBrush" : "AccentFillColorDefaultBrush");
        icon.VerticalAlignment = VerticalAlignment.Top;
        icon.Margin = new(0, 2, 12, 0);
        grid.Children.Add(icon);
        StackPanel text = new();
        text.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        foreach (string line in lines)
            text.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 0) });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        Border banner = new()
        {
            CornerRadius = new(4),
            BorderThickness = new(1),
            Padding = new(16, 12, 16, 12),
            Margin = new(0, 0, 0, 8),
            Child = grid
        };
        banner.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
        banner.SetResourceReference(Border.BackgroundProperty, error ? "InfoBarErrorSeverityBackgroundBrush" : "InfoBarInformationalSeverityBackgroundBrush");
        return banner;
    }

    private void SearchChanged(object s, TextChangedEventArgs e)
    {
        SearchHint.Visibility = Search.Text == "" ? Visibility.Visible : Visibility.Collapsed;
        if (IsLoaded)
            Render();
    }

    private void Render()
    {
        Form.Children.Clear();
        bool open = _current != null;
        HeaderPanel.Visibility = SearchRow.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        if (_current == null)
        {
            Form.Children.Add(EmptyState());
            Refresh();
            return;
        }
        if (_current.Root is JsonObject obj)
        {
            ObjectFields(obj, Schema.Fields(_current.Kind), Form);
            if (_current.Kind == DocumentKind.NPC)
            {
                string[] active = Schema.NPCTypes.Where(t => obj[t] != null).ToArray();
                ComboBox combo = new()
                {
                    ItemsSource = Schema.NPCTypes,
                    SelectedItem = active.Length == 1 ? active[0] : null,
                    Width = EditorWidth
                };
                combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is string type) { _current.SelectNPCType(type); Render(); } };
                AddRow(Section(Form, "Behaviour"), "Behaviour type", "One native behaviour per NPC. Bot uses the SUR/Newborn_Bot template, other types need a matching prefab path.", "", combo);
                if (active.Length == 1 && obj[active[0]] is JsonObject data)
                    ObjectFields(data, Schema.NPCData(active[0]), Form);
            }
        }
        else if (_current.Root is JsonArray array)
            Collection(array, Form, _current.Kind == DocumentKind.Waves);
        if (Form.Children.Count == 0 && Search.Text.Trim() != "")
            Form.Children.Add(new TextBlock { Text = "No settings match \"" + Search.Text.Trim() + "\".", Style = (Style)FindResource("Secondary"), FontSize = 14, Margin = new(2, 16, 0, 0) });
        Refresh();
        EditorScroll.ScrollToHome();
    }

    private StackPanel EmptyState()
    {
        string name = CategoryName(_category);
        StackPanel panel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 440,
            Margin = new(0, 120, 0, 0)
        };
        TextBlock icon = Themed(Symbol(Glyph(_category), 48), TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(icon);
        panel.Children.Add(new TextBlock { Text = "No " + name.ToLowerInvariant() + " open", FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(0, 18, 0, 6) });
        TextBlock hint = Secondary(_category == DocumentKind.Map
            ? "A map is made of up to three files: map settings, survival waves and trigger volumes. Start from a template or open existing files. Legacy [CustomMap] .ini files are imported too."
            : $"Start from a template or open an existing {Schema.FileName(_category)}. You can also drop files onto this window.");
        hint.FontSize = 14;
        hint.TextAlignment = TextAlignment.Center;
        panel.Children.Add(hint);
        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new(0, 22, 0, 0)
        };
        Button create = new()
        {
            Content = NewText.Text,
            Style = (Style)FindResource("AccentButtonStyle")
        };
        create.Click += NewClicked;
        Button open = new()
        {
            Content = "Open file...",
            Margin = new(8, 0, 0, 0)
        };
        open.Click += OpenClicked;
        buttons.Children.Add(create);
        buttons.Children.Add(open);
        panel.Children.Add(buttons);
        return panel;
    }

    // Section title followed by a card of setting rows. A section with the same name as the previous one is reused,
    // so wave + WaveInfo and the NPC type picker + behaviour data end up in one card.
    private StackPanel Section(Panel target, string name)
    {
        if (target.Children.Count > 0 && target.Children[target.Children.Count - 1] is Border { Tag: string last, Child: StackPanel rows } && last == name)
            return rows;
        TextBlock title = new()
        {
            Text = name,
            Style = (Style)FindResource("SectionTitle")
        };
        if (target.Children.Count == 0)
            title.Margin = new(2, 6, 0, 8);
        target.Children.Add(title);
        StackPanel card = new();
        target.Children.Add(new Border
        {
            Style = (Style)FindResource("Card"),
            Tag = name,
            Child = card
        });
        return card;
    }

    private static void AddRow(StackPanel card, string label, string hint, string key, FrameworkElement editor, bool below = false)
    {
        if (card.Children.Count > 0)
            card.Children.Add(Themed(new Border { Height = 1 }, Border.BackgroundProperty, "DividerStrokeColorDefaultBrush"));
        Grid row = new()
        {
            Margin = new(16, 7, 16, 7),
            MinHeight = 34
        };
        row.ColumnDefinitions.Add(new());
        row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        StackPanel text = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(0, 0, 24, 0),
            ToolTip = key == "" ? null : "JSON key: " + key
        };
        text.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap });
        if (hint != "")
            text.Children.Add(Secondary(hint));
        row.Children.Add(text);
        if (below)
        {
            row.RowDefinitions.Add(new() { Height = GridLength.Auto });
            row.RowDefinitions.Add(new() { Height = GridLength.Auto });
            Grid.SetRow(editor, 1);
            Grid.SetColumnSpan(editor, 2);
            editor.Margin = new(0, 10, 0, 0);
        }
        else
        {
            Grid.SetColumn(editor, 1);
            editor.VerticalAlignment = VerticalAlignment.Center;
        }
        row.Children.Add(editor);
        card.Children.Add(row);
    }

    private bool Matches(Field field, string scope)
    {
        string query = Search.Text.Trim();
        string text = scope + " " + field.Label + " " + field.Key + " " + field.Group;
        return query == "" || text.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            field.Kind == FieldKind.Object && field.Children!.Any(child => Matches(child, text));
    }

    private void ObjectFields(JsonObject obj, Field[] fields, Panel target, string scope = "")
    {
        foreach (var group in fields.Where(f => Matches(f, scope)).GroupBy(f => f.Group))
        {
            StackPanel? card = null;
            foreach (Field field in group)
            {
                if (field.Kind == FieldKind.Object)
                {
                    if (obj[field.Key] is JsonObject nested)
                    {
                        Field[] children = field.Children!.Select(child => child with { Group = field.Label }).ToArray();
                        ObjectFields(nested, children, target, scope + " " + field.Key + " " + field.Label);
                    }
                    else
                    {
                        Button create = new()
                        {
                            Content = obj.ContainsKey(field.Key) ? "Use default transform" : "Add transform",
                            Width = EditorWidth
                        };
                        create.Click += (_, _) => { obj[field.Key] = Schema.Object(field.Children!); Render(); };
                        AddRow(Section(target, field.Label), field.Label,
                            obj.ContainsKey(field.Key) ? "Replace the invalid transform with default values." : "Not set. The framework uses zero position and rotation, and unit scale.",
                            field.Key, create);
                    }
                    card = null;
                    continue;
                }
                if (field.Kind == FieldKind.Objects)
                {
                    // Nested lists get their own section without a card around them.
                    target.Children.Add(new TextBlock { Text = field.Label, Style = (Style)FindResource("SectionTitle") });
                    target.Children.Add(ListEditor(obj, field));
                    card = null;
                    continue;
                }
                card ??= Section(target, group.Key);
                AddRow(card, field.Label, field.Hint, field.Key, FieldEditor(obj, field), field.Kind == FieldKind.Strings);
            }
        }
    }

    private StackPanel ListEditor(JsonObject obj, Field field)
    {
        StackPanel nested = new();
        if (obj[field.Key] is JsonArray objects)
            Collection(objects, nested, false, field.Children);
        else
        {
            Button create = new()
            {
                Content = "Create list"
            };
            create.Click += (_, _) => { obj[field.Key] = new JsonArray(); Render(); };
            nested.Children.Add(create);
        }
        return nested;
    }

    private FrameworkElement FieldEditor(JsonObject obj, Field field)
    {
        JsonNode? value = obj[field.Key];
        Control control;
        FrameworkElement editor;
        switch (field.Kind)
        {
            case FieldKind.Toggle:
                bool effective = value is JsonValue bv && bv.TryGetValue<bool>(out bool b) ? b : field.Key is "CanAim" or "SandboxAvailable" or "BulletParticles" or "Blur" or "Fog" or "IsAvailable";
                CheckBox check = new()
                {
                    IsChecked = effective,
                    Style = (Style)FindResource("Switch"),
                    Cursor = Cursors.Hand
                };
                check.Checked += (_, _) => { obj[field.Key] = true; Changed(); };
                check.Unchecked += (_, _) => { obj[field.Key] = false; Changed(); };
                editor = control = check;
                break;
            case FieldKind.Choice:
                ComboBox choice = new()
                {
                    ItemsSource = field.Options,
                    DisplayMemberPath = "Label",
                    Width = EditorWidth
                };
                choice.SelectedItem = field.Options!.FirstOrDefault(o => JsonNode.DeepEquals(value, JsonSerializer.SerializeToNode(o.Value)));
                choice.SelectionChanged += (_, _) => { if (choice.SelectedItem is Option option) { obj[field.Key] = JsonSerializer.SerializeToNode(option.Value); Changed(); } };
                editor = control = choice;
                break;
            case FieldKind.Vector:
                Grid vector = new()
                {
                    Width = EditorWidth + 60
                };
                JsonArray? data = value as JsonArray;
                TextBox? first = null;
                for (int i = 0; i < 3; i++)
                {
                    int axis = i;
                    vector.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                    vector.ColumnDefinitions.Add(new());
                    TextBlock name = Secondary("XYZ"[i].ToString());
                    name.VerticalAlignment = VerticalAlignment.Center;
                    name.Margin = new(i == 0 ? 0 : 10, 0, 6, 0);
                    TextBox box = new()
                    {
                        Text = data is { Count: 3 } ? data[i]?.ToString() ?? "" : "",
                        MinWidth = 0
                    };
                    box.TextChanged += (_, _) =>
                    {
                        if (obj[field.Key] is not JsonArray { Count: 3 } current)
                        {
                            current = (JsonArray)JsonSerializer.SerializeToNode(field.Default)!;
                            obj[field.Key] = current;
                        }
                        current[axis] = NumericValue(box.Text, field);
                        Changed();
                    };
                    Grid.SetColumn(name, i * 2);
                    Grid.SetColumn(box, i * 2 + 1);
                    vector.Children.Add(name);
                    vector.Children.Add(box);
                    first ??= box;
                }
                control = first!;
                editor = vector;
                break;
            default:
                TextBox text = new()
                {
                    Text = field.Kind == FieldKind.Strings && value is JsonArray list ? string.Join(Environment.NewLine, list.Select(v => v?.ToString())) : value?.ToString() ?? "",
                    Width = EditorWidth
                };
                if (field.Kind == FieldKind.Strings)
                {
                    text.Width = double.NaN;
                    text.AcceptsReturn = true;
                    text.MinHeight = 90;
                    text.VerticalContentAlignment = VerticalAlignment.Top;
                    text.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                }
                text.TextChanged += (_, _) =>
                {
                    obj[field.Key] = field.Kind switch
                    {
                        FieldKind.Integer or FieldKind.Number => NumericValue(text.Text, field),
                        FieldKind.Strings => new JsonArray(text.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(v => (JsonNode?)JsonValue.Create(v)).ToArray()),
                        _ => JsonValue.Create(text.Text)
                    };
                    Changed();
                };
                control = text;
                editor = obj.ContainsKey(field.Key) ? text : NotSet(text, obj, field.Key);
                break;
        }
        System.Windows.Automation.AutomationProperties.SetName(control, field.Label);
        System.Windows.Automation.AutomationProperties.SetAutomationId(control, field.Key);
        return editor;
    }

    // Placeholder over an empty box for keys missing from the file.
    private static Grid NotSet(TextBox text, JsonObject obj, string key)
    {
        TextBlock hint = Themed(new TextBlock
        {
            Text = "Not set, game default",
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new(12, 0, 0, 0),
            Visibility = text.Text == "" ? Visibility.Visible : Visibility.Collapsed
        }, TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");
        text.TextChanged += (_, _) => hint.Visibility = text.Text == "" && !obj.ContainsKey(key) ? Visibility.Visible : Visibility.Collapsed;
        Grid grid = new();
        grid.Children.Add(text);
        grid.Children.Add(hint);
        return grid;
    }

    private static JsonNode NumericValue(string text, Field field)
    {
        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value))
        {
            if (field.Kind == FieldKind.Integer)
            {
                if (value >= int.MinValue && value <= int.MaxValue && value == Math.Truncate(value))
                    return JsonValue.Create((int)value)!;
            }
            else
                return JsonValue.Create(value)!;
        }
        return JsonValue.Create(text)!;
    }

    private Button IconButton(string glyph, string tip, Action action)
    {
        Button button = new()
        {
            Content = Symbol(glyph, 14),
            ToolTip = tip,
            Style = (Style)FindResource("SubtleButton"),
            Padding = new(9, 8, 9, 8)
        };
        button.Click += (_, _) => { action(); Render(); };
        return button;
    }

    private void Collection(JsonArray array, Panel target, bool waves, Field[]? fields = null)
    {
        fields ??= waves ? Schema.Wave : Schema.Trigger;
        string noun = waves ? "wave" : fields == Schema.Trigger ? "trigger" : "special NPC";
        for (int i = 0; i < array.Count; i++)
        {
            int index = i;
            JsonObject? item = array[i] as JsonObject;
            StackPanel actions = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new(0, 8, 52, 0)
            };
            if (i > 0)
                actions.Children.Add(IconButton("", "Move up", () => { JsonNode? node = array[index]; array.RemoveAt(index); array.Insert(index - 1, node); }));
            if (i < array.Count - 1)
                actions.Children.Add(IconButton("", "Move down", () => { JsonNode? node = array[index]; array.RemoveAt(index); array.Insert(index + 1, node); }));
            actions.Children.Add(IconButton("", "Duplicate", () =>
            {
                JsonNode? clone = array[index]?.DeepClone();
                if (clone is JsonObject copy && !waves && fields == Schema.Trigger)
                    copy["Id"] = Validator.String(copy, "Id") + "-copy-" + Guid.NewGuid().ToString("N")[..4];
                array.Insert(index + 1, clone);
            }));
            actions.Children.Add(IconButton("", "Remove", () => array.RemoveAt(index)));

            string name = waves ? $"Wave {i + 1}" : item == null ? "" : Validator.String(item, fields == Schema.Trigger ? "Id" : "NPCName");
            StackPanel header = new()
            {
                Margin = new(0, 4, 0, 4)
            };
            header.Children.Add(new TextBlock { Text = name == "" ? $"Unnamed {noun}" : name });
            string summary = item == null ? "Invalid entry" : waves
                ? $"{Validator.Number(item["WaveInfo"] as JsonObject ?? new(), "TotalCount")} NPCs, up to {Validator.Number(item, "MaxNPC")} at once"
                : fields == Schema.Trigger ? Validator.String(item, "Type") : $"{Validator.Number(item, "SpawnChance")}% spawn chance";
            header.Children.Add(Secondary(summary));
            Expander expand = new()
            {
                Header = header,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            expand.Expanded += (_, _) =>
            {
                if (expand.Content != null)
                    return;
                StackPanel content = new();
                if (item != null)
                {
                    ObjectFields(item, fields, content);
                    if (waves)
                    {
                        if (item["WaveInfo"] is JsonObject info)
                            ObjectFields(info, Schema.WaveInfo, content);
                        else
                        {
                            Button repair = new()
                            {
                                Content = "Create WaveInfo",
                                Margin = new(0, 8, 0, 0)
                            };
                            repair.Click += (_, _) => { item["WaveInfo"] = Schema.Object(Schema.WaveInfo); Render(); };
                            content.Children.Add(repair);
                        }
                    }
                }
                else
                    content.Children.Add(Secondary("This entry is not a JSON object. Remove it and add a new one."));
                expand.Content = content;
            };
            Grid cell = new()
            {
                Margin = new(0, 0, 0, 6)
            };
            cell.Children.Add(expand);
            cell.Children.Add(actions);
            target.Children.Add(cell);
            if (array.Count <= 2)
                expand.IsExpanded = true;
        }
        StackPanel label = new()
        {
            Orientation = Orientation.Horizontal
        };
        label.Children.Add(Symbol("", 14));
        label.Children.Add(new TextBlock { Text = "Add " + noun, Margin = new(8, 0, 0, 0) });
        Button add = new()
        {
            Content = label,
            Margin = new(0, 4, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        add.Click += (_, _) =>
        {
            JsonObject next = waves ? Schema.NewWave() : Schema.Object(fields);
            if (fields == Schema.Trigger)
            {
                int suffix = array.Count + 1;
                while (array.OfType<JsonObject>().Any(o => Validator.String(o, "Id") == "trigger-" + suffix))
                    suffix++;
                next["Id"] = "trigger-" + suffix;
            }
            array.Add(next);
            Render();
        };
        target.Children.Add(add);
    }
}
